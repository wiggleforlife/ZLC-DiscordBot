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
using ZLCBotCore.ControllerLogic;
using ZLCBotCore.Data;
using ZLCBotCore.Models.VatsimJsonData;
using ZLCBotCore.Models.VatusaJsonData;

namespace ZLCBotCore.Services
{
    public class VatsimApiService
    {
        public static List<string> ZlcPrefixes { get; protected set; } = new List<string> { "BIL", "BOI", "BZN", "SUN", "GPI", "GTF", "HLN", "IDA", "JAC", "TWF", "MSO", "OGD", "PIH", "PVU", "SLC", "ZLC" };
        public static List<string> Suffixes { get; protected set; } = new List<string> { "DEL", "GND", "TWR", "APP", "DEP", "CTR", "TMU" };

        public bool VatsimServiceRun { get; protected set; } = false;

        private readonly DiscordShardedClient _discord;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly ControllerLists _controllerLists;

        //public List<VatsimController> ZLCOnlineControllers { get; protected set; }

        public VatsimApiService(IServiceProvider services)
        {
            _services = services;
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _discord = _services.GetRequiredService<DiscordShardedClient>();
            _logger = _services.GetRequiredService<ILogger<CommandHandler>>();
            _controllerLists = services.GetRequiredService<ControllerLists>();

            _logger.LogInformation("Loaded: VatsimApiService");
        }

        private void Run()
        {
            _logger.LogInformation("Service: VatsimApiService.Run Started");

            if (_config["includeOBS"] == "true")
            {
                _logger.LogDebug("Service: VatsimApiService is Including Observer positions.");
                Suffixes.Add("OBS");
            }

            while (VatsimServiceRun)
            {
                var online = GetOnlineControllers();

                if (!(online is null))
                {
                    _controllerLists.ZLCOnlineControllers = online;
                }

                Thread.Sleep(int.Parse(_config["serviceCheckLimit"]));
            }
        }

        public void Start()
        {
            _logger.LogDebug("Function: VatsimApiService.Start() Called");

            // Just incharge of reaching out to Vatisim and Vatusa API and keeping a list of who is online! 
            VatsimServiceRun = true;

            Thread t = new Thread(Run);
            t.Start();
        }

        public void Stop()
        {
            _logger.LogWarning("Function: VatsimApiService.Stop() Called");

            VatsimServiceRun = false;
        }

        private List<VatsimController> GetOnlineControllers()
        {
            _logger.LogDebug("Function: VatsimApiService.GetOnlineControllers() Called");


            // Vatsim Json Link: https://data.vatsim.net/v3/vatsim-data.json

            string vatsimJsonString = ReadJsonFromWebsite("https://data.vatsim.net/v3/vatsim-data.json");

            VatsimJsonRootModel AllVatsimInfo = JsonConvert.DeserializeObject<VatsimJsonRootModel>(vatsimJsonString); // TODO - .Net Core has Json Functions in it. Switch to using that instead of Netonsoft.
            if (AllVatsimInfo is null)
            {
                _logger.LogError("Json: Could not Deserialize Vatsim Json. Is the website down?");
                return null;
            }

            List<VatsimController> OnlineControllers = new List<VatsimController>();

            foreach (VatsimController controller in AllVatsimInfo.controllers)
            {
                if (controller.callsign.Contains('_'))
                {
                    string[] CallsignSplit = controller.callsign.Split('_');
                    string currentControllerPrefix = CallsignSplit[0];
                    string currentControllerSuffix = CallsignSplit[^1];

                    if ((_config["debuging:allowAnyPrefix"] == "true" || ZlcPrefixes.Contains(currentControllerPrefix)) 
                        && (_config["debuging:allowAnySuffix"] == "true" || Suffixes.Contains(currentControllerSuffix)))
                    {
                        string old_name = controller.name;

                        // check current list and fix online list UpdatedNameWithVatUsa bool.
                        if (_controllerLists.CurrentPostedControllers.Count >= 1)
                        {
                            foreach (VatsimController postedController in _controllerLists.CurrentPostedControllers)
                            {
                                if (postedController.cid == controller.cid)
                                {
                                    controller.UpdatedNameWithVatUsa = postedController.UpdatedNameWithVatUsa;
                                    controller.name = postedController.name;
                                    break;
                                }
                            }
                        }

                        try
                        {
                            if (_config["corectName"] == "true" && !controller.UpdatedNameWithVatUsa)
                            {
                                string new_name = GetControllerName(controller.cid);

                                if (new_name != null)
                                {
                                    controller.name = new_name;
                                    controller.UpdatedNameWithVatUsa = true;
                                    _logger.LogDebug($"Name: Controller name Changed [{old_name}] -> [{new_name}]");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Name: Could not change Controller Name [{controller.name}]: {e.Message}");
                            if (string.IsNullOrWhiteSpace(old_name))
                            {
                                controller.name = "UNKNOWN NAME";
                            }
                            else
                            {
                                controller.name = old_name;
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
            _logger.LogDebug($"Function: VatsimApiService.GetControllerName() Called **args[{cid}]");

            // Vatusa API link: https://api.vatusa.net/v2/user/{cid}

            string VatusaJsonString = ReadJsonFromWebsite($"https://api.vatusa.net/v2/user/{cid}");

            VatusaJsonRoot ControllerInformation = JsonConvert.DeserializeObject<VatusaJsonRoot>(VatusaJsonString); // TODO - .Net Core has Json Functions in it. Switch to using that instead of Netonsoft.
            if (ControllerInformation is null)
            {
                _logger.LogError("Json: Could not Deserialize Vatusa Json. Is the website down?");
                return null;
            }

            string ControllerFullName = $"{ControllerInformation.data.fname} {ControllerInformation.data.lname}";

            return ControllerFullName;
        }

        private string ReadJsonFromWebsite(string url)
        {
            _logger.LogDebug($"Function: VatsimApiService.ReadJsonFromWebsite() Called **args[{url}]");

            using (WebClient webClient = new WebClient()) // TODO - Should this really be inside a using statement?
            {
                string json = webClient.DownloadString(url);

                return json;
            }
        }
    }
}
