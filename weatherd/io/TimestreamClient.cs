using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using Microsoft.Extensions.Configuration;

namespace weatherd.io
{
    public class TimestreamClient : ITimestreamClient
    {
        private AmazonTimestreamWriteClient timestreamClient;
        
        public void Connect()
        {
            timestreamClient =
                new AmazonTimestreamWriteClient(new EnvironmentVariablesAWSCredentials(), RegionEndpoint.USEast1);
        }

        /// <inheritdoc />
        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request) => timestreamClient.ListTablesAsync(request);

        /// <inheritdoc />
        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request) => timestreamClient.CreateTableAsync(request);

        /// <inheritdoc />
        public Task<WriteRecordsResponse> WriteRecordsAsync(WriteRecordsRequest request) => timestreamClient.WriteRecordsAsync(request);
    }
}
