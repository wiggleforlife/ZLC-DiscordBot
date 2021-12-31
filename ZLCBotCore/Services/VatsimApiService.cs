using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Threading;
using ZLCBotCore.Models.VatsimJsonData;
using ZLCBotCore.Models.VatusaJsonData;

namespace ZLCBotCore.Services
{
    public class VatsimApiService
    {
        // TODO - remove non ZLC Prefixes
        public static List<string> ZlcPrefixes { get; protected set; } = new List<string> { "BIL", "BOI", "BZN", "SUN", "GPI", "GTF", "HLN", "IDA", "JAC", "TWF", "MSO", "OGD", "PIH", "PVU", "SLC", "ZLC" };
        public static List<string> Suffixes { get; protected set; } = new List<string> { "DEL", "GND", "TWR", "APP", "DEP", "CTR", "TMU" };

        public bool VatsimServiceRun { get; protected set; } = false;

        private readonly DiscordShardedClient _discord;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;



        //public List<VatsimController> ZLCOnlineControllers { get; protected set; }
        public List<VatsimController> ZLCOnlineControllers { get; protected set; }

        public VatsimApiService(IServiceProvider services)
        {
            _services = services;
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _discord = _services.GetRequiredService<DiscordShardedClient>();
            _logger = _services.GetRequiredService<ILogger<CommandHandler>>();

            ZLCOnlineControllers = new List<VatsimController>();
        }

        private void Run()
        {
            while (VatsimServiceRun)
            {
                var online = GetOnlineControllers();

                if (!(online is null))
                {
                    ZLCOnlineControllers = online;
                }

                // TODO - Change this interval to 5Minues converted for Miliseconds.
                Thread.Sleep(int.Parse(_config["serviceCheckLimit"]));
            }
        }

        public void Start()
        {
            // Just incharge of reaching out to Vatisim and Vatusa API and keeping a list of who is online! 
            VatsimServiceRun = true;

            Thread t = new Thread(Run);
            t.Start();
        }

        public void Stop()
        {
            VatsimServiceRun = false;
        }

        private List<VatsimController> GetOnlineControllers()
        {
            // Vatsim Json Link: https://data.vatsim.net/v3/vatsim-data.json

            string vatsimJsonString = ReadJsonFromWebsite("https://data.vatsim.net/v3/vatsim-data.json");

            VatsimJsonRootModel AllVatsimInfo = JsonConvert.DeserializeObject<VatsimJsonRootModel>(vatsimJsonString); // TODO - .Net Core has Json Functions in it. Switch to using that instead of Netonsoft.
            if (AllVatsimInfo is null)
            {
                _logger.LogError("Could not Deserialize Vatsim Json. Is the website down?");
                return null;
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
                            
                            string new_name = GetControllerName(controller.cid);
                            string old_name = controller.name;

                            controller.name = new_name;
                            _logger.LogDebug($"Controller name Changed [{old_name}] -> [{new_name}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Could not change Controller Name [{controller.name}]: {e.Message}");
                            if (string.IsNullOrWhiteSpace(controller.name))
                            {
                                controller.name = "UNKNOWN NAME";
                            }
                        }

                        OnlineControllers.Add(controller);
                    }
                }
            }

            return OnlineControllers;
        }

        private string GetControllerName(int cid)
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

        private string ReadJsonFromWebsite(string url)
        {
            using (WebClient webClient = new WebClient()) // TODO - Should this really be inside a using statement?
            {
                string json = webClient.DownloadString(url);

                _logger.LogDebug($"Read JSON from: {url}");
                return json;
            }
        }
    }
}
