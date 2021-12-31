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

namespace ZLCBotCore.Modules.Staff
{
    class StaffCommands : ModuleBase
    {
        private DiscordShardedClient _client;
        private readonly IConfigurationRoot _config;
        private string _prefix;
        private readonly ILogger _logger;
        private readonly OnlineControllerLogic _controllerLogic;
        private readonly IServiceProvider _services;
        private readonly VatsimApiService _vatsimApi;


        public StaffCommands(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordShardedClient>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _prefix = _config["prefix"];
            _logger = _services.GetRequiredService<ILogger<StaffCommands>>();

            _controllerLogic = _services.GetRequiredService<OnlineControllerLogic>();
            _vatsimApi = _services.GetRequiredService<VatsimApiService>();
            
            _logger.LogInformation("Loaded: StaffCommands");
        }

        // Discord Staff only commands go here.

        [Name("Online Controller Monitor")]
        [Summary("Starts the ATC Online Controller Monitor")]
        [Command("start", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task StartCommand()
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


        [Name("Online Controller Monitor")]
        [Summary("Stops the ATC Online Controller Monitor")]
        [Command("stop", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task StopCommand()
        {
            await Context.Message.DeleteAsync();

            _controllerLogic.Stop();
        }


        [Name("Delete Messages")]
        [Summary("Deletes a number of messages (default: 10) in a channel. Note: Will not delete Pinned Messages.")]
        [Command("delete", RunMode = RunMode.Async)]
        [Alias("del", "purge")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task DeleteCommand(int amount = 10)
        {
            //await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
            await Context.Message.DeleteAsync();

            if (amount > 100) amount = 100;

            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, amount).FlattenAsync();

            foreach (var msg in messages)
            {
                if (!msg.IsPinned)
                {
                    await msg.DeleteAsync();
                    // Might need to adjust this to a more specific number. Discord only allows a certain number of calls per second. 
                    Thread.Sleep(100);
                }
            }
        }
    }
}
