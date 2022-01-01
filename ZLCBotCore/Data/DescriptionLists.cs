using System;
using System.Collections.Generic;
using System.Text;

namespace ZLCBotCore.Data
{
    public static class DescriptionLists
    {
        private static int ChanceToGetDescription = 25;

        private static List<string> offlineDescriptionOptions = new List<string>
        {
            "Hey... Hey you... Yeah you... \nMaybe you should get online and control for a little bit?",
            "Hey... You are already in the discord, why not control for a bit?",
            "'Break for Control.'\n'Salt Lake Local, Stockton. Information.'\n...\nOh, wait no one is online. Sad Day.",
            "Funny story: No one is online. \nOh wait thats not really that funny, is it?\nHmm, well you could always get online and control.",
            "The 'Colonel' commands you to get online and control.",
            "ZLC is a 24/7 ARTCC, well not in the virtual world, but you should still get online and control some."
        };

        public static string ChooseDescription(bool alwaysChooseDescription = true)
        {
            var random = new Random();
            if (alwaysChooseDescription || random.Next(0, 100) < ChanceToGetDescription) 
            {
                int index = random.Next(offlineDescriptionOptions.Count);
                return offlineDescriptionOptions[index];
            }
            return "\u200B";
        }
    }
}
