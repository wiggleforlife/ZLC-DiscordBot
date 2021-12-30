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

namespace ZLCBotCore.Modules.Testing
{
    public class TestingCommands : ModuleBase
    {
        private DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private string _prefix;
        private readonly ILogger _logger;
        private readonly OnlineControllerLogic _controllerLogic;
        private readonly IServiceProvider _services;

        public TestingCommands(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordShardedClient>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _prefix = _config["prefix"];
            _logger = _services.GetRequiredService<ILogger<TestingCommands>>();

            _controllerLogic = _services.GetRequiredService<OnlineControllerLogic>();
        }

        [Command("test", RunMode = RunMode.Async)]
        public async Task TestingCommand(string command)
        {
            switch (command.ToLower())
            {
                case "start":
                    {
                        _controllerLogic.KeepRunning = true;

                        // Put into thread with while loop.
                        Thread t = new Thread(() => _controllerLogic.Run(Context));
                        t.Start();
                        await Context.Channel.SendMessageAsync("Online Controller Check started");

                        break;
                    }
                case "stop":
                    {
                        _controllerLogic.KeepRunning = false;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        [Command("del", RunMode = RunMode.Async)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task DeleteCommand(int amount = 10)
        {
            if (amount > 100) amount = 100;

            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, amount).FlattenAsync();

            foreach (var msg in messages)
            {
                if (!msg.IsPinned)
                {
                    await msg.DeleteAsync();
                }
            }

            //await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
            await Context.Message.DeleteAsync();
        }
    }
}
