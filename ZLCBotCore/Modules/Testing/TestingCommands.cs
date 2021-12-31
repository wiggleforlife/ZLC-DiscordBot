﻿using Discord;
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

        }

        [Command("admin-api", RunMode = RunMode.Async)]
        public async Task AdminApi(string command)
        {
            switch (command.ToLower())
            {
                case "start":
                    {
                        _vatsimApi.Start();
                        break;
                    }
                case "stop":
                    {
                        _vatsimApi.Stop();
                        break;
                    }
                case "restart":
                    {
                        _vatsimApi.Stop();
                        Thread.Sleep(10000);
                        _vatsimApi.Start();
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