using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;
using weatherd.datasources;
using weatherd.io;

namespace weatherd.services
{
    public interface ITimestreamService
    {
        bool Initialized { get; }
        Task<bool> Initialize();
        Task WriteToTimestream(WeatherState state);
    }

    public class TimestreamService : ITimestreamService
    {
        private readonly ITimestreamClient timestreamClient;
        private readonly bool _enableDataWrite = true;
        private static long _RecordVersion;

        private readonly string _databaseName;
        private readonly string _tableName;
        private readonly RecordDefinition[] _recordDefinitions;
        private readonly Dimension[] _dimensions;

        public TimestreamService(IConfiguration config, ITimestreamClient client)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            timestreamClient = client;

            IConfigurationSection tsConfig = config.GetSection("TimestreamService");

            if (tsConfig is null)
                return;

            _enableDataWrite = tsConfig.GetValue("EnableDataWrite", true);
            _databaseName = tsConfig.GetValue("Database", "weather");
            _tableName = tsConfig.GetValue("Table", "local");
            _recordDefinitions = tsConfig.GetValue("Records", Array.Empty<RecordDefinition>());

            // We cannot actually sync data without record definitions, so ...
            if (_enableDataWrite && _recordDefinitions.Length == 0)
                throw new InvalidOperationException(
                    "Cannot sync to Timestream without record definitions in configuration.");

            _recordDefinitions = Utilities.GetConfigurationArray<RecordDefinition>(tsConfig.GetSection("Records")).ToArray();
            _dimensions = Utilities.GetConfigurationArray<Dimension>(tsConfig.GetSection("Dimensions"), new Dictionary<string, Func<string, object>>
            {
                { nameof(Dimension.DimensionValueType), x => DimensionValueType.FindValue(x.ToUpperInvariant()) }
            }).ToArray();
        }
        
        /// <inheritdoc />
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        public async Task<bool> Initialize()
        {
            if (Initialized)
                return Initialized;

            timestreamClient.Connect();

            ListTablesResponse listResp = await timestreamClient.ListTablesAsync(new ListTablesRequest()
            {
                DatabaseName = _databaseName
            });

            if (listResp.Tables.Any(
                table => table.TableName.Equals(_tableName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Initialized = true;
                return Initialized;
            }

            Initialized = await InitializeDatabase();
            return Initialized;
        }

        private async Task<bool> InitializeDatabase()
        {
            CreateTableResponse createResp = await timestreamClient.CreateTableAsync(new CreateTableRequest
            {
                DatabaseName = _databaseName,
                TableName = _tableName
            });

            if (createResp.HttpStatusCode == HttpStatusCode.OK)
                return true;

            Log.Fatal("Could not create Timestream table '{TableName}'", _tableName);
            return false;
        }
        
        public async Task WriteToTimestream(WeatherState wxState)
        {
            if (!_enableDataWrite)
                return;

            List<Record> records = new List<Record>();

            foreach (RecordDefinition defn in _recordDefinitions)
            {
                try
                {
                    object value = GetProperty(wxState, defn.Property, defn.Unit);
                    MeasureValueType mvt = MeasureValueType.FindValue(defn.Type.ToUpperInvariant());
                    records.Add(new Record
                    {
                        MeasureName = defn.Name,
                        MeasureValueType = mvt,
                        MeasureValue = value.ToString()
                    });
                } catch
                {
                    // Ignore
                }
            }

            if (records.Count == 0)
                return;

            long recordVersion = Interlocked.Increment(ref _RecordVersion);
            WriteRecordsRequest recordsRequest = new WriteRecordsRequest
            {
                DatabaseName = _databaseName,
                TableName = _tableName,
                CommonAttributes = new Record
                {
                    Time = "now",
                    Dimensions = _dimensions.ToList(),
                    Version = recordVersion
                },
                Records = records
            };

            try
            {
                var response = await timestreamClient.WriteRecordsAsync(recordsRequest);

                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (response.HttpStatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        throw new RecordIngestForbiddenException(
                            $"Failed to ingest Timestream records:  {response.HttpStatusCode} in request ID {response.ResponseMetadata.RequestId}!");
                    case HttpStatusCode.OK:
                        Log.Information("Records ingested: {RecordsIngest} with version {VersionNumber} in request ID {RequestId}",
                                        response.RecordsIngested.Total, recordVersion, response.ResponseMetadata.RequestId);
                        break;
                    default:
                        Log.Warning(
                            "Failed to ingest Timestream records.  Status code: {StatusCode} in request ID {RequestId}",
                            response.HttpStatusCode, response.ResponseMetadata.RequestId);
                        break;
                }
            }
            catch (RejectedRecordsException)
            {
                // Ignore it
            }
        }

        public static object GetProperty(WeatherState wxState, string propertyName, string unitName)
        {
            Type weatherStateType = wxState.GetType();

            PropertyInfo propInfo = weatherStateType.GetProperty(propertyName);
            if (propInfo is null)
                throw new InvalidOperationException($"Could not find meteorological property '{propertyName}'");

            object value = propInfo.GetValue(wxState);
            if (value is null)
                throw new InvalidOperationException($"Could not resolve '{propertyName}' into a value.");

            if (string.IsNullOrEmpty(unitName))
                return value;

            Type valueType = value.GetType();

            PropertyInfo unitPropInfo = valueType.GetProperty(unitName);
            if (unitPropInfo is null)
                throw new InvalidOperationException($"Could not find unit '{unitName}'");

            return unitPropInfo.GetValue(value);
        }
    }
}
