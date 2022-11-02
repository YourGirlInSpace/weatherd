using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AWS.Logger.SeriLog;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using weatherd.datasources;
using weatherd.datasources.pakbus;
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

            var timestreamService = serviceProvider.GetRequiredService<IWeatherTimestreamService>();
            IAsyncWeatherDataSource dataSource = GetConfiguredDataSource(serviceProvider);

            if (!await timestreamService.Initialize(dataSource))
            {
                Log.Fatal("Failed to initialize Timestream service.");
                return;
            }

            AutoResetEvent endSignaller = new AutoResetEvent(false);
            if (!await timestreamService.Start(endSignaller))
            {
                Log.Fatal("Failed to start Timestream service.");
                return;
            }
            
            // Wait for the timestream service to report that it is finished.
            endSignaller.WaitOne();

            Log.Information("weatherd is now exiting.");
            Log.Verbose("Thank you for participating in this Aperture Science computer-aided enrichment activity.");
        }

        private static IServiceProvider ConfigureServices(IEnumerable<string> args)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddTransient<ITimestreamClient, TimestreamClient>();
            services.AddSingleton<IWeatherTimestreamService, WeatherTimestreamService>();
#if DEBUG
            services.AddTransient<ITestDataSource, TestDataSource>();
#endif
            services.AddTransient<IPakbusDataSource, PakbusDataSource>();

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

        private static IAsyncWeatherDataSource GetConfiguredDataSource(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (!Enum.TryParse(Configuration.GetValue("DataSource", "Test"), out DataSourceType dataSourceType))
            {
                Log.Fatal("Could not determine data source type to load.");
                return null;
            }
            
            Log.Information("Loading the {dataSourceType} data source!", dataSourceType);

            try
            {
                return dataSourceType switch
                {
                    DataSourceType.Pakbus => serviceProvider.GetRequiredService<IPakbusDataSource>(),
#if DEBUG
                    DataSourceType.Test => serviceProvider.GetRequiredService<ITestDataSource>(),
#else
                    DataSourceType.Test => throw new InvalidOperationException(
                        "Test data source is unavailable in release versions.")
#endif
                    _ => throw new ArgumentOutOfRangeException()
                };
            } catch (Exception ex)
            {
                Log.Fatal(ex, "Could not configure data source.");
                return null;
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
            Log.Verbose("Logging system initialized.");
        }
    }
}
