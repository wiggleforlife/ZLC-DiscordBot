using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ZLCBotCore.ControllerLogic;
using ZLCBotCore.Services;

namespace ZLCBotCore
{
    public class ZLCBot
    {
        private CommandHandler _handler;
        private static IConfigurationRoot _config;

        public async Task StartAsync()
        {
            // create Build Configuration
            var _builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                                                     .AddJsonFile(path: "config.json");
            _config = _builder.Build();

            // configure the services 
            var services = new ServiceCollection().AddSingleton(new DiscordShardedClient())
                                                  .AddSingleton(_config)
                                                  .AddSingleton(new CommandService(new CommandServiceConfig { DefaultRunMode = RunMode.Async, LogLevel = LogSeverity.Debug, CaseSensitiveCommands = false, ThrowOnError = false }))
                                                  .AddSingleton<StartupService>()
                                                  .AddSingleton<LoggingService>()
                                                  .AddSingleton<CommandHandler>()
                                                  .AddSingleton<OnlineControllerLogic>()
                                                  .AddSingleton<VatsimApiService>();




            // Need to add logging
            ConfigureServices(services);

            // build the services
            var serviceProvider = services.BuildServiceProvider();

            // Instantiate the Logger
            serviceProvider.GetRequiredService<LoggingService>();

            // Instantiate Controller Logic.
            serviceProvider.GetRequiredService<OnlineControllerLogic>();

            // Start the bot
            await serviceProvider.GetRequiredService<StartupService>().StartAsync();

            // Load Services
            serviceProvider.GetRequiredService<CommandHandler>();

            // Block program till its closed.
            await Task.Delay(-1);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            //Add SeriLog
            services.AddLogging(configure => configure.AddSerilog());
            //Remove default HttpClient logging as it is extremely verbose
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            
            var logLevel = _config["logLevel"]; 
            var level = Serilog.Events.LogEventLevel.Error;
            if (!string.IsNullOrEmpty(logLevel))
            {
                switch (logLevel.ToLower())
                {
                    case "error": { level = Serilog.Events.LogEventLevel.Error; break; }
                    case "info": { level = Serilog.Events.LogEventLevel.Information; break; }
                    case "debug": { level = Serilog.Events.LogEventLevel.Debug; break; }
                    case "crit": { level = Serilog.Events.LogEventLevel.Fatal; break; }
                    case "warn": { level = Serilog.Events.LogEventLevel.Warning; break; }
                    case "trace": { level = Serilog.Events.LogEventLevel.Debug; break; }
                }
            }
            Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("logs/zlc-bot.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .MinimumLevel.Is(level)
                    .CreateLogger();
        }
    }
}
