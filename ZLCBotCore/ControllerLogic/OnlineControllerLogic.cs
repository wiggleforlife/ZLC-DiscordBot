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

namespace ZLCBotCore.ControllerLogic
{
    public class OnlineControllerLogic
    {
        public bool KeepRunning { get; set; } = false;
        public EmbedBuilder MessageText { get; protected set; } = null;

        private IUserMessage Message;

        public List<VatsimController> CurrentControllerList { get; set; }

        public static readonly List<string> ZlcPrefixes = new List<string> { "LAX", "OAK", "BUR", "BIL", "BOI", "BZN", "SUN", "GPI", "GTF", "HLN", "IDA", "JAC", "TWF", "MSO", "OGD", "PIH", "PVU", "SLC", "ZLC" };
        public static readonly List<string> Suffixes = new List<string> { "DEL", "GND", "TWR", "APP", "DEP", "CTR", "TMU" };

        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        public OnlineControllerLogic(IServiceProvider services)
        {
            _services = services;
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _logger = _services.GetRequiredService<ILogger<OnlineControllerLogic>>();

            CurrentControllerList = new List<VatsimController>();
        }

        public async void Run(ICommandContext context)
        {
            while (KeepRunning)
            {
                CheckControllers();

                if (!(MessageText is null))
                {
                    if (Message is null)
                    {
                        Message = await context.Channel.SendMessageAsync("", false, MessageText.Build());
                    }
                    else
                    {
                        await Message.ModifyAsync(msg => msg.Embed = MessageText.Build());
                    }
                }

                Thread.Sleep(17000);
            }
        }

        private void CheckControllers()
        {
            _logger.LogDebug("CheckControllersCalled.");
            List<VatsimController> OnlineControllerList;

            try
            {
                OnlineControllerList = GetOnlineControllers();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            
            if (CurrentControllerListHasChanged(OnlineControllerList))
            {
                CurrentControllerList = OnlineControllerList;

                MessageText = FormatDiscordMessage();
            }
        }

        internal EmbedBuilder FormatDiscordMessage()
        {
            string msgOutput = "DISCORD MESSAGE - CURRENT ONLINE CONTROLLERS:\n"; // TODO - Should be a stringbuilder but for testing purposes we will just use a regular string. 

            if (CurrentControllerList.Count() == 0)
            {
                msgOutput += "\tNone";
            }
            else
            {
                foreach (VatsimController controller in CurrentControllerList)
                {
                    msgOutput += $"\t{controller.callsign} - {controller.name}\n";
                }
            }


            var embed = new EmbedBuilder();

            embed.Title = "ONLINE CONTROLLER LIST!";

            foreach (var onlineController in CurrentControllerList)
            {
                embed.AddField(new EmbedFieldBuilder { Name = onlineController.callsign, Value = onlineController.name });
            }
            
            
            return embed;
        }

        internal bool CurrentControllerListHasChanged(List<VatsimController> OnlineControllers)
        {
            if (CurrentControllerList.Count() != OnlineControllers.Count())
            {
                return true;
            }
            else if (CurrentControllerList.Count() == 0 && OnlineControllers.Count() == 0)
            {
                return false;
            }

            List<List<int>> CidLists = ExtractCidFromLists(OnlineControllers);

            IEnumerable<int> differenceQuery = CidLists[0].Except(CidLists[1]);
            IEnumerable<int> differenceQueryTwo = CidLists[1].Except(CidLists[0]);

            if (differenceQuery.Count() == 0 && differenceQueryTwo.Count() == 0) // TODO - Double Check this, it should be returning true when someone logs off, but its not.
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal List<List<int>> ExtractCidFromLists(List<VatsimController> OnlineControllers)
        {
            List<int> currentCids = new List<int>();
            List<int> onlineCids = new List<int>();

            foreach (VatsimController currentController in CurrentControllerList)
            {
                currentCids.Add(currentController.cid);
            }

            foreach (VatsimController onlineController in OnlineControllers)
            {
                onlineCids.Add(onlineController.cid);
            }

            return new List<List<int>> { currentCids, onlineCids };
        }

        internal List<VatsimController> GetOnlineControllers()
        {
            // Vatsim Json Link: https://data.vatsim.net/v3/vatsim-data.json

            string vatsimJsonString = ReadJsonFromWebsite("https://data.vatsim.net/v3/vatsim-data.json");

            VatsimJsonRootModel AllVatsimInfo = JsonConvert.DeserializeObject<VatsimJsonRootModel>(vatsimJsonString); // TODO - .Net Core has Json Functions in it. Switch to using that instead of Netonsoft.
            if (AllVatsimInfo is null)
            {
                throw new Exception("Could not Deserialize Vatsim Json. Is the website down?");
            }

            List<VatsimController> OnlineControllers = new List<VatsimController>();

            foreach (VatsimController controller in AllVatsimInfo.controllers)
            {
                if (controller.callsign.Contains('_'))
                {
                    string[] CallsignSplit = controller.callsign.Split('_');
                    string currentControllerPrefix = CallsignSplit[0];
                    string currentControllerSuffix = CallsignSplit[CallsignSplit.Length - 1];

                    if (ZlcPrefixes.Contains(currentControllerPrefix) && Suffixes.Contains(currentControllerSuffix))
                    {
                        try
                        {
                            controller.name = GetControllerName(controller.cid);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(e.Message);
                        }

                        OnlineControllers.Add(controller);
                    }
                }
            }

            return OnlineControllers;
        }

        internal string GetControllerName(int cid)
        {
            // Vatusa API link: https://api.vatusa.net/v2/user/{cid}

            string VatusaJsonString = ReadJsonFromWebsite($"https://api.vatusa.net/v2/user/{cid}");

            VatusaJsonRoot ControllerInformation = JsonConvert.DeserializeObject<VatusaJsonRoot>(VatusaJsonString); // TODO - .Net Core has Json Functions in it. Switch to using that instead of Netonsoft.
            if (ControllerInformation is null)
            {
                throw new Exception("Could not Deserialize Vatusa Json. Is the website down?");
            }

            string ControllerFullName = $"{ControllerInformation.data.fname} {ControllerInformation.data.lname}";

            return ControllerFullName;
        }

        internal string ReadJsonFromWebsite(string url)
        {
            using (WebClient webClient = new WebClient()) // TODO - Should this really be inside a using statement?
            {
                string json = webClient.DownloadString(url);

                return json;
            }
        }
    }
}
