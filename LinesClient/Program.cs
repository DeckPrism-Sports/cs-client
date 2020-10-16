#region Usings
using DPSports.Configuration;
using DPSports.Feed;
using DPSports.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Net; 
#endregion

namespace LinesClient
{
    class Program
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// Software entrypoint
        /// </summary>
        /// <param name="args">none so far</param>
        /// <returns>Int32 return type</returns>
        public static int Main(string[] args)
        {
            //increase the default connection limit
            ServicePointManager.DefaultConnectionLimit = 20;

            //get the environment
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Console.WriteLine($"Environment1 {env}");

            var basePath = Directory.GetCurrentDirectory();

            var path1 = Path.Combine(basePath, $"appsettings.{env}.json");

            if(!File.Exists(path1))
                Console.WriteLine($"Cannot find ASPNETCORE_ENVIRONMENT {path1}");

            //load the configuration to setup logging
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", false)//mandatory
                .AddJsonFile($"appsettings.{env}.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();//optional

            ConfigurationManager.Configuration = configuration;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            LogManager.EnableLogger(Log.Logger);

            try
            {
                LogManager.Info($"Starting Lines Client Version....", null);

                RegisterServices(configuration, Log.Logger);

                var service = _serviceProvider.GetService<IWorker>();

                var signal = service.MainLoop().Result;
#if DEBUG
                Console.ReadLine();
#endif
                DisposeServices();

                LogManager.Info("Lines Client Ended", null);

                return signal;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex, "Application terminated unexpectedly", null);
#if DEBUG
                Console.ReadLine();
#endif
                return 1;
            }
            finally
            {
                LogManager.CloseAndFlush();
            }
        }

        /// <summary>
        /// Creates a denpendency injection enviroment with all the services needed
        /// </summary>
        private static void RegisterServices(IConfigurationRoot config, ILogger logger)
        {
            var services = new ServiceCollection();

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger, false));

            services.AddSingleton(config);

            //Local services          
            services.AddSingleton<IWorker, LinesWorker>();
            services.AddSingleton<IRabbitService, RabbitService>();
            services.AddSingleton<IApiClient, ApiClient>();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Disposes all disposable services before closing
        /// </summary>
        private static void DisposeServices()
        {
            if(_serviceProvider == null && _serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
