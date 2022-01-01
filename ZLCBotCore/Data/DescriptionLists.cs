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
    public static class DescriptionLists
    {
        private static int CurrentIndex = -1;
        private static DateTime LastUpdated = DateTime.UtcNow;

        private static atcZeroNotesJson atcZeroNotes = JsonConvert.DeserializeObject<atcZeroNotesJson>(ReadFromGithub("https://raw.githubusercontent.com/Nikolai558/ZLC-DiscordBot/main/ZLCBotCore/atcZeroNotes.txt"));

        public static string ChooseDescription(bool alwaysChooseDescription = true)
        {
            if (DateTime.UtcNow.Subtract(LastUpdated).TotalHours >= 1)
            {
                ReadFromGithub("https://raw.githubusercontent.com/Nikolai558/ZLC-DiscordBot/main/ZLCBotCore/atcZeroNotes.txt");
                LastUpdated = DateTime.UtcNow;
            }

            if (CurrentIndex + 1 > atcZeroNotes.atcZeroNotes.Count())
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
