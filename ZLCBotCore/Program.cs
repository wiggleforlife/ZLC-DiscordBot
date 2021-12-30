using System;

namespace ZLCBotCore
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                new ZLCBot().StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
