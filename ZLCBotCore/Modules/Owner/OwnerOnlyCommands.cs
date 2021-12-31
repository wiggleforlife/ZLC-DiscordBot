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

namespace ZLCBotCore.Modules.Owner
{
    class OwnerOnlyCommands : ModuleBase
    {
        private DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private string _prefix;
        private readonly ILogger _logger;
        private readonly OnlineControllerLogic _controllerLogic;
        private readonly IServiceProvider _services;
        private readonly VatsimApiService _vatsimApi;


        public OwnerOnlyCommands(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordShardedClient>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _prefix = _config["prefix"];
            _logger = _services.GetRequiredService<ILogger<OwnerOnlyCommands>>();

            _controllerLogic = _services.GetRequiredService<OnlineControllerLogic>();
            _vatsimApi = _services.GetRequiredService<VatsimApiService>();

            _logger.LogInformation("Module: Loaded OwnerOnlyCommands");
        }


        [Name("Vatsim API Bot Service")]
        [Summary("Control the status of the Service the bot uses to connect with VATSIM Data.")]
        [Command("admin-api", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task AdminApi(string command)
        {
            switch (command.ToLower())
            {
                case "start":
                    {
                        _vatsimApi.Start();
                        await Context.Channel.SendMessageAsync($"Started Vatsim API bot Service");
                        break;
                    }
                case "stop":
                    {
                        _vatsimApi.Stop();
                        await Context.Channel.SendMessageAsync($"**WARNING:** Stopped Vatsim API bot Service");
                        break;
                    }
                case "restart":
                    {
                        await Context.Channel.SendMessageAsync($"**WARNING:** Restarting Vatsim API bot Service");

                        _vatsimApi.Stop();
                        Thread.Sleep(10000);
                        _vatsimApi.Start();
                        break;
                    }
                default: { break; }
            }
        }
    }
}
