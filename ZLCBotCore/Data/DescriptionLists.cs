using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ZLCBotCore.Data
{
    public static class DescriptionLists
    {
        private static int CurrentIndex = -1;
        private static DateTime LastUpdated = DateTime.UtcNow;

        private static List<string> atcZeroNotes = ReadFromGithub("https://raw.githubusercontent.com/Nikolai558/ZLC-DiscordBot/main/ZLCBotCore/atcZeroNotes.txt");

        public static string ChooseDescription(bool alwaysChooseDescription = true)
        {
            if (DateTime.UtcNow.Subtract(LastUpdated).TotalHours >= 1)
            {
                atcZeroNotes = ReadFromGithub("https://raw.githubusercontent.com/Nikolai558/ZLC-DiscordBot/main/ZLCBotCore/atcZeroNotes.txt");
            }

            if (CurrentIndex + 1 > atcZeroNotes.Count)
            {
                CurrentIndex = 0;
            }
            
            CurrentIndex += 1;
            string output = atcZeroNotes[CurrentIndex];
            return output;
        }

        private static List<string> ReadFromGithub(string url)
        {
            using (WebClient webClient = new WebClient()) // TODO - Should this really be inside a using statement?
            {
                string text = webClient.DownloadString(url);

                return text.Split('|').ToList();
            }
        }
    }
}
