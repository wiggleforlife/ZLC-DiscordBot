using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZLCBotCore.ControllerLogic;
using ZLCBotCore.Services;

namespace ZLCBotCore.Modules.Testing
{
    [Name("Commands Under Development")]
    [Summary("Commands that are under development or just used for testing things.")]
    public class TestingCommands : ModuleBase
    {
        private DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private string _prefix;
        private readonly ILogger _logger;
        private readonly OnlineControllerLogic _controllerLogic;
        private readonly IServiceProvider _services;
        private readonly VatsimApiService _vatsimApi;


        public TestingCommands(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordShardedClient>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _prefix = _config["prefix"];
            _logger = _services.GetRequiredService<ILogger<TestingCommands>>();

            _controllerLogic = _services.GetRequiredService<OnlineControllerLogic>();
            _vatsimApi = _services.GetRequiredService<VatsimApiService>();

            _logger.LogInformation("Module: Loaded TestingCommands");
        }

        // Commands in Development go Here
    }
}
