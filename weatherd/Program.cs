using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Logger.SeriLog;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using weatherd.datasources;
using weatherd.datasources.pakbus;
using weatherd.datasources.Vaisala;
using weatherd.io;
using weatherd.services;
#if DEBUG
using weatherd.datasources.testdatasource;
#endif

namespace weatherd
{
    internal class Program
    {
        internal static IConfiguration Configuration { get; private set; }

        private static async Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
#if WINDOWS
                            .AddJsonFile("conf/weatherd.windows.json")
#elif LINUX
                            .AddJsonFile("conf/weatherd.linux.json")
#endif
                            .AddEnvironmentVariables()
                            .Build();

            IServiceProvider serviceProvider = ConfigureServices(args);

            var cliOptions = serviceProvider.GetRequiredService<CommandLineOptions>();
            if (cliOptions.ShowPakbus)
            {
                await ShowPakbusContents(serviceProvider);
                return;
            }

            var weatherService = serviceProvider.GetRequiredService<IWeatherService>();

            IAsyncWeatherDataSource[] dataSources = GetConfiguredDataSource(serviceProvider).ToArray();
            if (!await weatherService.Initialize(dataSources))
            {
                Log.Fatal("Failed to initialize Timestream service");
                return;
            }

            AutoResetEvent endSignaller = new AutoResetEvent(false);
            if (!await weatherService.Start(endSignaller))
            {
                Log.Fatal("Failed to start Timestream service");
                return;
            }
            
            // Wait for the timestream service to report that it is finished.
            endSignaller.WaitOne();

            Log.Information("weatherd is now exiting");
            Log.Verbose("Thank you for participating in this Aperture Science computer-aided enrichment activity");
        }

        private static async Task ShowPakbusContents(IServiceProvider serviceProvider)
        {
            IPakbusDataSource pakbusDataSource = serviceProvider.GetRequiredService<IPakbusDataSource>();
            
            if (!await pakbusDataSource.Initialize())
            {
                Log.Fatal("Could not initialize Pakbus.");
                return;
            }
            
            pakbusDataSource.EmitPakbusInformation();
        }

        private static IServiceProvider ConfigureServices(IEnumerable<string> args)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddTransient<ITimestreamClient, TimestreamClient>();
            services.AddSingleton<IWeatherService, WeatherService>();
            services.AddSingleton<ITimestreamService, TimestreamService>();
#if DEBUG
            services.AddTransient<ITestDataSource, TestDataSource>();
#endif
            services.AddTransient<IPakbusDataSource, PakbusDataSource>();
            services.AddTransient<IVaisalaDataSource, PWD12DataSource>();

            Parser.Default.ParseArguments<CommandLineOptions>(args)
                  .WithParsed(o =>
                  {
                      services.AddSingleton(o);
                      InitializeLogger(o.Verbose, o.CloudwatchEnabled);

#if DEBUG
                      Log.Warning("weatherd is currently running in DEBUG mode!");
#endif
                  });

            return services.BuildServiceProvider();
        }

        private static IEnumerable<IAsyncWeatherDataSource> GetConfiguredDataSource(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var dataSourceNames = Configuration.GetSection("DataSources").Get<string[]>();

            foreach (string name in dataSourceNames)
            {
                if (!Enum.TryParse(name, out DataSourceType dataSourceType))
                {
                    Log.Fatal("Could not determine data source type to load");
                    yield break;
                }
            
                Log.Information("Loading the {DataSourceType} data source!", dataSourceType);

                yield return dataSourceType switch
                {
                    DataSourceType.Pakbus => serviceProvider.GetRequiredService<IPakbusDataSource>(),
                    DataSourceType.Vaisala => serviceProvider.GetRequiredService<IVaisalaDataSource>(),
#if DEBUG
                    DataSourceType.Test => serviceProvider.GetRequiredService<ITestDataSource>(),
#else
                    DataSourceType.Test => throw new InvalidOperationException(
                        "Test data source is unavailable in release versions.")
#endif
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        private static void InitializeLogger(bool verboseEnabled, bool cloudwatchEnabled)
        {
            LoggerConfiguration loggerConfig = new LoggerConfiguration()
                                               .ReadFrom.Configuration(Configuration.GetSection("Serilog"))
                                               .WriteTo.Console()
#if WINDOWS
                                               .WriteTo.File("weatherd.log");
#elif LINUX
                                               .WriteTo.File("/var/log/weatherd.log");
#endif

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (verboseEnabled)
                loggerConfig = loggerConfig.MinimumLevel.Verbose();
            else
#if TRACE
                loggerConfig = loggerConfig.MinimumLevel.Verbose();
#elif DEBUG
                loggerConfig = loggerConfig.MinimumLevel.Debug();
#else
                loggerConfig = loggerConfig.MinimumLevel.Information();
#endif

            if (cloudwatchEnabled)
                loggerConfig = loggerConfig.WriteTo.AWSSeriLog(Configuration);

            Log.Logger = loggerConfig.CreateLogger();
            Log.Verbose("Logging system initialized");
        }
    }
}
