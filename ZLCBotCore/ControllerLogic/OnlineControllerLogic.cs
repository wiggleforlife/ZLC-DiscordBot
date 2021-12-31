using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ZLCBotCore.Models.VatsimJsonData;
using ZLCBotCore.Models.VatusaJsonData;
using ZLCBotCore.Services;

namespace ZLCBotCore.ControllerLogic
{

    // TODO - Make this a service and take some parts out that dont make sense in a discord bot.
    public class OnlineControllerLogic
    {
        private readonly IConfigurationRoot _config;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly VatsimApiService _vatsimApi;

        private DateTime lastNewPostTime;

        public bool OnlineControllerRun { get; private set; } = false;
        public List<VatsimController> CurrentPostedControllers { get; private set; }

        private IUserMessage Message;
        public EmbedBuilder MessageText = null;



        public OnlineControllerLogic(IServiceProvider services)
        {
            _services = services;
            _logger = _services.GetRequiredService<ILogger<CommandHandler>>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _vatsimApi = _services.GetRequiredService<VatsimApiService>();

            CurrentPostedControllers = new List<VatsimController>();
        }

        private async void Run(ICommandContext context)
        {
            while (OnlineControllerRun)
            {
                // TODO - Change this interval to 5Minues converted for Miliseconds.

                if (NewControllerLoggedOn())
                {
                    if (DateTime.UtcNow.Subtract(lastNewPostTime).TotalMinutes >= double.Parse(_config["newPostLimit"]))
                    {
                        // Update our current Posted List to be that same as the online list.
                        CurrentPostedControllers = _vatsimApi.ZLCOnlineControllers;

                        // Build out our Message! this will be used to either post new or edit previous.
                        MessageText = FormatDiscordMessage();

                        // Check to see if we even have a previous message to delete.
                        if (!(Message is null))
                        {
                            // We do so delete it.
                            try
                            {
                                await Message.DeleteAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message);
                            }
                        }

                        // Post a new message and save it into our variable Message
                        Message = await context.Channel.SendMessageAsync("", false, MessageText.Build());

                        // Update our LastNewPostTime
                        lastNewPostTime = DateTime.UtcNow;
                    }
                    else
                    {
                        // Update our current Posted List to be that same as the online list.
                        CurrentPostedControllers = _vatsimApi.ZLCOnlineControllers;

                        // Build out our Message! this will be used to either post new or edit previous.
                        MessageText = FormatDiscordMessage();

                        // Check to see if we even have a previous message to edit.
                        
                        try
                        {
                            await Message.ModifyAsync(msg => msg.Embed = MessageText.Build());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                               
                            Message = await context.Channel.SendMessageAsync("", false, MessageText.Build());

                            // Update our LastNewPostTime
                            lastNewPostTime = DateTime.UtcNow;
                        }
                        
                    }
                }
                else
                {
                    // Update our current Posted List to be that same as the online list.
                    CurrentPostedControllers = _vatsimApi.ZLCOnlineControllers;

                    // Build out our Message! this will be used to either post new or edit previous.
                    MessageText = FormatDiscordMessage();

                    // Check to see if we even have a previous message to edit.
                    try
                    {
                        await Message.ModifyAsync(msg => msg.Embed = MessageText.Build());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);

                        Message = await context.Channel.SendMessageAsync("", false, MessageText.Build());

                        // Update our LastNewPostTime
                        lastNewPostTime = DateTime.UtcNow;
                    }
                }

                Thread.Sleep(int.Parse(_config["serviceCheckLimit"]));
            }
        }

        public void Start(ICommandContext context)
        {
            OnlineControllerRun = true;
            Thread t = new Thread(() => Run(context));
            t.Start();
        }

        public void Stop()
        {
            OnlineControllerRun = false;
        }

        private bool NewControllerLoggedOn()
        {
            // ExtractCidFromLists Looks at the online controller list (current) and the Posted Controller List (previous)
            Dictionary<string, List<int>> CidLists = ExtractCidFromLists();

            // The ".Except" will return a list with ONLY the difference in the two lists (from above)
            // Note: the way this is set up, it will only grab the differences from the online Controller List. See example below
            // Example 1: 
            //      PostedCids = [123, 456, 789]
            //      OnlineCids = [123, 456, 789]
            //         This would return Nothing (i.e. No change)
            // Example 2: 
            //      PostedCids = [123, 456, 789, 555]
            //      OnlineCids = [123, 456, 789]
            //         This would return Nothing (i.e. No change)
            //         Since we only care about New OnlineCids
            // Example 3: 
            //      PostedCids = [123, 456, 789]
            //      OnlineCids = [123, 456, 789, 777]
            //         This would return One CID (i.e. 777)
            // Example 4: 
            //      PostedCids = [123, 456, 789]
            //      OnlineCids = [123, 456, 789, 777, 888, 999]
            //         This would return Three CIDs (i.e. 777, 888, 999)

            IEnumerable<int> differenceQuery = CidLists["OnlineCids"].Except(CidLists["PostedCids"]);

            if (differenceQuery.Count() == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private Dictionary<string, List<int>> ExtractCidFromLists()
        {
            var controllerCids = new Dictionary<string, List<int>>(){
                {"PostedCids", new List<int>()},
                {"OnlineCids", new List<int>()}
            };

            List<int> currentCids = new List<int>();
            List<int> onlineCids = new List<int>();

            foreach (VatsimController currentController in CurrentPostedControllers)
            {
                controllerCids["PostedCids"].Add(currentController.cid);
            }

            foreach (VatsimController onlineController in _vatsimApi.ZLCOnlineControllers)
            {
                controllerCids["OnlineCids"].Add(onlineController.cid);
            }

            return controllerCids;
        }

        internal EmbedBuilder FormatDiscordMessage()
        {

            var embed = new EmbedBuilder();

            embed.Title = "ONLINE ZLC ATC:";

            if (CurrentPostedControllers.Count() <= 0)
            {
                embed.AddField(new EmbedFieldBuilder { Value = "None at this time." });
            }
            else
            {
                foreach (var onlineController in CurrentPostedControllers)
                {
                    embed.AddField(new EmbedFieldBuilder { Name = onlineController.callsign, Value = onlineController.name});
                }
            }

            return embed;
        }
    }
}
