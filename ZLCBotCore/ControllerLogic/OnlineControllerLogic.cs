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
using ZLCBotCore.Data;
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
        private readonly ControllerLists _controllerLists;
        //private readonly VatsimApiService _vatsimApi;

        private DateTime lastNewPostTime;
        private IUserMessage Message;
        public EmbedBuilder MessageText = null;
        
        public bool OnlineControllerRun { get; private set; } = false;



        public OnlineControllerLogic(IServiceProvider services)
        {
            _services = services;
            _logger = _services.GetRequiredService<ILogger<CommandHandler>>();
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _controllerLists = _services.GetRequiredService<ControllerLists>();
            //_vatsimApi = _services.GetRequiredService<VatsimApiService>();

            
            _logger.LogInformation("Loaded: OnlineControllerLogic");
        }

        private async void Run(ICommandContext context)
        {
            _logger.LogInformation("Service: OnlineControllerLogic.Run() Service Started");

            while (OnlineControllerRun)
            {
                if (NewControllerLoggedOn())
                {
                    if (DateTime.UtcNow.Subtract(lastNewPostTime).TotalMinutes >= double.Parse(_config["newPostLimit"]))
                    {
                        // Update our current Posted List to be that same as the online list.
                        _controllerLists.CurrentPostedControllers = _controllerLists.ZLCOnlineControllers;

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
                                _logger.LogError($"Message: {ex.Message}");
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
                        _controllerLists.CurrentPostedControllers = _controllerLists.ZLCOnlineControllers;

                        // Build out our Message! this will be used to either post new or edit previous.
                        MessageText = FormatDiscordMessage();

                        // Check to see if we even have a previous message to edit.
                        try
                        {
                            await Message.ModifyAsync(msg => msg.Embed = MessageText.Build());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Message: {ex.Message}");

                            Message = await context.Channel.SendMessageAsync("", false, MessageText.Build());

                            // Update our LastNewPostTime
                            lastNewPostTime = DateTime.UtcNow;
                        }
                    }
                }
                else
                {
                    // Update our current Posted List to be that same as the online list.
                    _controllerLists.CurrentPostedControllers = _controllerLists.ZLCOnlineControllers;

                    // Build out our Message! this will be used to either post new or edit previous.
                    MessageText = FormatDiscordMessage();

                    // Check to see if we even have a previous message to edit.
                    try
                    {
                        await Message.ModifyAsync(msg => msg.Embed = MessageText.Build());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message: {ex.Message}");

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
            _logger.LogDebug("Function: OnlineControllerLogic.Start() Called");

            OnlineControllerRun = true;
            Thread t = new Thread(() => Run(context));
            t.Start();
        }

        public void Stop()
        {
            _logger.LogWarning("Function: OnlineControllerLogic.Stop() Called");

            OnlineControllerRun = false;
        }

        private bool NewControllerLoggedOn()
        {
            _logger.LogDebug("Function: OnlineControllerLogic.NewControllerLoggedOn() Called");

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
            _logger.LogDebug("Function: OnlineControllerLogic.ExtractCidFromLists() Called");

            var controllerCids = new Dictionary<string, List<int>>(){
                {"PostedCids", new List<int>()},
                {"OnlineCids", new List<int>()}
            };

            List<int> currentCids = new List<int>();
            List<int> onlineCids = new List<int>();

            foreach (VatsimController currentController in _controllerLists.CurrentPostedControllers)
            {
                controllerCids["PostedCids"].Add(currentController.cid);
            }

            foreach (VatsimController onlineController in _controllerLists.ZLCOnlineControllers)
            {
                controllerCids["OnlineCids"].Add(onlineController.cid);
            }

            return controllerCids;
        }

        internal EmbedBuilder FormatDiscordMessage()
        {
            _logger.LogDebug("Function: OnlineControllerLogic.FormatDiscordMessage() Called");
            var time = DateTime.UtcNow.ToString("HH:mm");

            var embed = new EmbedBuilder();

            embed.Title = "ONLINE ZLC ATC:";
            embed.Color = new Discord.Color(0, 38, 0);
            embed.Footer = new EmbedFooterBuilder { Text = $"Updated: {time}z" };

            if (_controllerLists.CurrentPostedControllers.Count() <= 0)
            {
                embed.AddField(new EmbedFieldBuilder { Name = "-", Value = $"None at this time.\n{'\u200B'}\n{'\u200B'}\n" });
                embed.Color = new Discord.Color(38,0,0);
                return embed;
            }

            Dictionary<string, List<VatsimController>> atcBySuffix = new Dictionary<string, List<VatsimController>>
            {
                { "TMU", new List<VatsimController>() },
                { "CTR", new List<VatsimController>() },
                { "APP", new List<VatsimController>() },
                { "DEP", new List<VatsimController>() },
                { "TWR", new List<VatsimController>() },
                { "GND", new List<VatsimController>() },
                { "DEL", new List<VatsimController>() },
                { "OBS", new List<VatsimController>() }
            };

            foreach (var onlineController in _controllerLists.CurrentPostedControllers)
            {
                var splitCallsign = onlineController.callsign.Split('_');
                var suffix = splitCallsign[^1];

                atcBySuffix[suffix].Add(onlineController);
            }

            foreach (var suffix in atcBySuffix.Keys)
            {
                string valueForField = $"";
                foreach (VatsimController controller in atcBySuffix[suffix])
                {
                    valueForField += $"***{controller.callsign}*** - {controller.name}\n";
                }

                valueForField += $"{'\u200B'}\n{'\u200B'}\n";

                if ((!string.IsNullOrEmpty(valueForField) && !string.IsNullOrWhiteSpace(valueForField) && valueForField != $"{'\u200B'}\n{'\u200B'}\n") 
                     && (!string.IsNullOrWhiteSpace(suffix) && !string.IsNullOrEmpty(suffix)))
                {
                    switch (suffix)
                    {
                        case "TMU": { embed.AddField(new EmbedFieldBuilder { Name = $"**__TFC Management__**", Value = $"{valueForField}" }); break; }
                        case "CTR": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Center__**", Value = $"{valueForField}" }); break; }
                        case "APP": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Approach__**", Value = $"{valueForField}" }); break; }
                        case "DEP": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Departure__**", Value = $"{valueForField}" }); break; }
                        case "GND": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Ground__**", Value = $"{valueForField}" }); break; }
                        case "DEL": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Clearance__**", Value = $"{valueForField}" }); break; }
                        case "TWR": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Tower__**", Value = $"{valueForField}" }); break; }
                        case "OBS": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Observer__**", Value = $"{valueForField}" }); break; }
                        default: break;
                    }
                }
            }

            return embed;
        }
    }
}
