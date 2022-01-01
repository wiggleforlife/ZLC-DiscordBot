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

namespace ZLCBotCore.Modules.Owner
{
    [Name("Owner Only commands")]
    [Summary("These commands only effect the hardware of the computer that the bot is running on.")]
    [RequireOwner]
    public class OwnerOnlyCommands : ModuleBase
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
        public async Task AdminApi(string command)
        {
            await Context.Message.DeleteAsync();

            switch (command.ToLower())
            {
                case "start":
                    {
                        _vatsimApi.Start();
                        await Context.Message.Author.SendMessageAsync($"Started Vatsim API bot Service");
                        break;
                    }
                case "stop":
                    {
                        _vatsimApi.Stop();
                        await Context.Message.Author.SendMessageAsync($"**WARNING:** Stopped Vatsim API bot Service");
                        break;
                    }
                case "restart":
                    {
                        await Context.Message.Author.SendMessageAsync($"**WARNING:** Restarting Vatsim API bot Service");

                        _vatsimApi.Stop();
                        Thread.Sleep(10000);
                        _vatsimApi.Start();
                        break;
                    }
                default: { break; }
            }
        }

        [Name("Reload Config")]
        [Summary("Reload the configuration file the bot uses")]
        [Command("reload-cfg", RunMode = RunMode.Async)]
        public async Task ReloadConfig()
        {
            await Context.Message.DeleteAsync();

            _config.Reload();

            if (_config["debuging:fastChecksDebuging"] == "true")
            {
                _config["serviceCheckLimit"] = "17000";
                _config["newPostLimit"] = "1";
                _config["corectName"] = "false";
            }

            await Context.Message.Author.SendMessageAsync("Reloaded the configuration file!");
        }
    }
}
