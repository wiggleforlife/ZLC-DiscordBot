using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZLCBotCore.Services
{
    public class ChannelCheck
    {

        public async Task Reply(ICommandContext context, EmbedBuilder embed)
        {
            //await Task.Run(async () =>
            //{
            //    ChannelOutput replyChannel;
            //    var guildInfo = context.Guild;
            //    if (guildInfo == null)
            //    {
            //        replyChannel = new ChannelOutput();
            //    }
            //    else
            //    {
            //        replyChannel = GetGuildBotChannel(context.Guild.Id);
            //    }
            //    if (!string.IsNullOrEmpty(replyChannel.ChannelName))
            //    {
            //        var messageChannel = await context.Client.GetChannelAsync((ulong)replyChannel.ChannelId) as ISocketMessageChannel;
            //        await messageChannel.SendMessageAsync("", false, embed.Build());
            //    }
            //    else
            //    {
            //        await context.Channel.SendMessageAsync("", false, embed.Build());
            //    }
            //});
        }
    }
}
