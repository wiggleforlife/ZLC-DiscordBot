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
                        _controllerLists.CurrentPostedControllers = _controllerLists.ZLCOnlineControllers;
                        MessageText = FormatDiscordMessage();

                        if (!(Message is null))
                        {
                            // try to delete the previous message
                            try { await Message.DeleteAsync(); } catch (Exception ex) { _logger.LogError($"Message: {ex.Message}");}
                        }

                        Message = await context.Channel.SendMessageAsync("", false, MessageText.Build());
                        lastNewPostTime = DateTime.UtcNow;
                    }
                    else
                    {
                        _controllerLists.CurrentPostedControllers = _controllerLists.ZLCOnlineControllers;
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
                            lastNewPostTime = DateTime.UtcNow;
                        }
                    }
                }
                else
                {
                    _controllerLists.CurrentPostedControllers = _controllerLists.ZLCOnlineControllers;
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

        private EmbedBuilder FormatDiscordMessage()
        {
            _logger.LogDebug("Function: OnlineControllerLogic.FormatDiscordMessage() Called");
            var time = DateTime.UtcNow.ToString("HH:mm");

            var embed = new EmbedBuilder();

            embed.Title = $"{_controllerLists.CurrentPostedControllers.Count}  -  ATC ONLINE";
            embed.Color = new Discord.Color(0, 38, 0);
            embed.Footer = new EmbedFooterBuilder { Text = $"Updated: {time}z" };
            
            if (_controllerLists.CurrentPostedControllers.Count() <= 0)
            {
                embed.Title = "NO ATC ONLINE";
                embed.Color = new Discord.Color(38,0,0);
                embed.Description = DescriptionLists.ChooseDescription();
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
                { "OBS", new List<VatsimController>() },
                { "OTHER", new List<VatsimController>() }
            };

            foreach (var onlineController in _controllerLists.CurrentPostedControllers)
            {
                var splitCallsign = onlineController.callsign.Split('_');
                var suffix = splitCallsign[^1];

                if (atcBySuffix.Keys.Contains(suffix))
                {
                    atcBySuffix[suffix].Add(onlineController);
                }
                else
                {
                    atcBySuffix["OTHER"].Add(onlineController);

                }
            }

            foreach (var suffix in atcBySuffix.Keys)
            {
                bool test = false;
                string valueForField = $"";
                foreach (VatsimController controller in atcBySuffix[suffix])
                {
                    string addToField = $"***{controller.callsign}*** - {controller.name}\n";

                    if (valueForField.Length + addToField.Length + $"{'\u200B'}\n{'\u200B'}\n".Length > 1024)
                    {
                        //valueForField += $"{'\u200B'}\n{'\u200B'}\n";
                        AddField(embed, valueForField, suffix, test);
                        test = true;
                        valueForField = "";
                    }

                    valueForField += addToField;
                }

                valueForField += $"{'\u200B'}\n{'\u200B'}\n";

                AddField(embed, valueForField, suffix, test);
            }

            return embed;
        }

        private void AddField(EmbedBuilder embed, string fieldValue, string suffix, bool continuationOfCategory)
        {
            switch (continuationOfCategory)
            {
                case false:
                    {
                        if ((!string.IsNullOrEmpty(fieldValue) && !string.IsNullOrWhiteSpace(fieldValue) && fieldValue != $"{'\u200B'}\n{'\u200B'}\n")
                             && (!string.IsNullOrWhiteSpace(suffix) && !string.IsNullOrEmpty(suffix)))
                        {
                            switch (suffix)
                            {
                                case "TMU": { embed.AddField(new EmbedFieldBuilder { Name = $"**__TFC Management__**", Value = $"{fieldValue}" }); break; }
                                case "CTR": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Center__**", Value = $"{fieldValue}" }); break; }
                                case "APP": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Approach__**", Value = $"{fieldValue}" }); break; }
                                case "DEP": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Departure__**", Value = $"{fieldValue}" }); break; }
                                case "GND": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Ground__**", Value = $"{fieldValue}" }); break; }
                                case "DEL": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Clearance__**", Value = $"{fieldValue}" }); break; }
                                case "TWR": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Tower__**", Value = $"{fieldValue}" }); break; }
                                case "OBS": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Observer__**", Value = $"{fieldValue}" }); break; }
                                default: { embed.AddField(new EmbedFieldBuilder { Name = $"**__Other__**", Value = $"{fieldValue}" }); break; }
                            }
                        }

                        break;
                    } 
                case true:
                    {
                        if ((!string.IsNullOrEmpty(fieldValue) && !string.IsNullOrWhiteSpace(fieldValue) && fieldValue != $"{'\u200B'}\n{'\u200B'}\n")
                             && (!string.IsNullOrWhiteSpace(suffix) && !string.IsNullOrEmpty(suffix)))
                        {
                            switch (suffix)
                            {
                                case "TMU": { embed.AddField(new EmbedFieldBuilder { Name = $"**__TFC Management-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "CTR": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Center-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "APP": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Approach-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "DEP": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Departure-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "GND": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Ground-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "DEL": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Clearance-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "TWR": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Tower-Cont.__**", Value = $"{fieldValue}" }); break; }
                                case "OBS": { embed.AddField(new EmbedFieldBuilder { Name = $"**__Observer-Cont.__**", Value = $"{fieldValue}" }); break; }
                                default: { embed.AddField(new EmbedFieldBuilder { Name = $"**__Other-Cont.__**", Value = $"{fieldValue}" }); break; }
                            }
                        }
                        break;
                    }
            }

            
        }
    }
}
