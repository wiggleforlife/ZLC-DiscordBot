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

namespace ZLCBotCore.Modules.OnlineControllerModule
{
    public class OnlineControllerCommands : ModuleBase
    {
        private DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private string _prefix;
        private readonly ILogger _logger;
        private readonly OnlineControllerLogic _controllerLogic;
        private readonly IServiceProvider _services;
        private readonly VatsimApiService _vatsimApi;


        public OnlineControllerCommands(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordShardedClient>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _prefix = _config["prefix"];
            _logger = _services.GetRequiredService<ILogger<OnlineControllerCommands>>();

            _controllerLogic = _services.GetRequiredService<OnlineControllerLogic>();
            _vatsimApi = _services.GetRequiredService<VatsimApiService>();

        }

        [Command("start", RunMode = RunMode.Async)]
        public async Task Start()
        {
            await Context.Message.DeleteAsync();

            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, 10).FlattenAsync();

            foreach (var msg in messages)
            {
                if (!msg.IsPinned)
                {
                    await msg.DeleteAsync();
                    // Might need to adjust this to a more specific number. Discord only allows a certain number of calls per second. 
                    Thread.Sleep(100);
                }
            }

            if (!_vatsimApi.VatsimServiceRun)
            {
                _vatsimApi.Start();
                Thread.Sleep(5000);
            }
            _controllerLogic.Start(Context);
        }

        [Command("stop", RunMode = RunMode.Async)]
        public async Task Stop()
        {
            await Context.Message.DeleteAsync();

            _controllerLogic.Stop();
        }
    }
}
