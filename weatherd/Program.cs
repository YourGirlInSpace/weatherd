using System;
using System.Threading;
using System.Threading.Tasks;
using AWS.Logger.SeriLog;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using weatherd.datasources;
using weatherd.datasources.pakbus;
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

            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddSingleton<IWeatherTimestreamService, WeatherTimestreamService>();
#if DEBUG
            services.AddTransient<ITestDataSource, TestDataSource>();
            services.AddTransient<IPakbusDataSource, PakbusDataSource>();
#endif

            Parser.Default.ParseArguments<CommandLineOptions>(args)
                  .WithParsed(o =>
                  {
                      services.AddSingleton(o);
                      InitializeLogger(o.Verbose, o.CloudwatchEnabled);

#if DEBUG
                      Log.Warning("weatherd is currently running in DEBUG mode!");
#endif
                  });

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            var timestreamService = serviceProvider.GetRequiredService<IWeatherTimestreamService>();

            if (!Enum.TryParse(Configuration.GetValue("DataSource", "Test"), out DataSourceType dataSourceType))
            {
                Log.Fatal("Could not determine data source type to load.");
                return;
            }

            IAsyncWeatherDataSource asyncWxDataSource;
            try
            {
                asyncWxDataSource = dataSourceType switch
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
                return;
            }

            Log.Information("Loading the {dataSourceType} data source!", dataSourceType);

            if (!await timestreamService.Initialize(asyncWxDataSource))
            {
                Log.Fatal("Failed to initialize Timestream service.");
                return;
            }

            if (!await timestreamService.Start())
            {
                Log.Fatal("Failed to start Timestream service.");
                return;
            }

            while (true)
                Thread.Sleep(1000);
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
