using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ZLCBotCore.Models.AtcZeroNotesJson;

namespace ZLCBotCore.Data
{
    public class DescriptionLists
    {
        private int CurrentIndex = -1;
        private DateTime LastUpdated = DateTime.UtcNow;
        private atcZeroNotesJson atcZeroNotes = JsonConvert.DeserializeObject<atcZeroNotesJson>(ReadFromGithub("https://raw.githubusercontent.com/Nikolai558/ZLC-DiscordBot/main/ZLCBotCore/atcZeroNotes.json"));

        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        public DescriptionLists(IServiceProvider services)
        {
            _services = services;
            _config = _services.GetRequiredService<IConfigurationRoot>();
            _logger = _services.GetRequiredService<ILogger<DescriptionLists>>();
        }

        public string ChooseDescription(bool alwaysChooseDescription = true)
        {
            if (DateTime.UtcNow.Subtract(LastUpdated).TotalMinutes >= double.Parse(_config["getDescriptionCheck"]))
            {
                atcZeroNotes = JsonConvert.DeserializeObject<atcZeroNotesJson>(ReadFromGithub("https://raw.githubusercontent.com/Nikolai558/ZLC-DiscordBot/main/ZLCBotCore/atcZeroNotes.json"));
                LastUpdated = DateTime.UtcNow;
                _logger.LogDebug("Description: Grabed description messages from github.");
            }

            if (CurrentIndex + 1 > atcZeroNotes.atcZeroNotes.Count() -1)
            {
                CurrentIndex = -1;
            }
            
            CurrentIndex += 1;
            string output = atcZeroNotes.atcZeroNotes[CurrentIndex];

            //output.Replace("\\\\", "\\");
            return output;
        }

        private static string ReadFromGithub(string url)
        {
            using (WebClient webClient = new WebClient()) // TODO - Should this really be inside a using statement?
            {
                var json = webClient.DownloadString(url);

                return json;
            }
        }
    }
}
