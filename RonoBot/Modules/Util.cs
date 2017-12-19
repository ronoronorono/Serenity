using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace RonoBot.Modules
{
    public class Util : ModuleBase<SocketCommandContext>
    {
        //Returns the user latency
        //Latency returned isn't the channel/server latency but the latency where the bot is actually being hosted
        //so values might not be accurate.
        [Command("ping")]
        public async Task PingAsync()
        {
            var sw = Stopwatch.StartNew();
            var msg = await Context.Channel.SendMessageAsync("Nani?!").ConfigureAwait(false);
            sw.Stop();
            await msg.DeleteAsync().ConfigureAwait(false);
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthor(Context.User.ToString())
                .WithDescription($"{(int)sw.Elapsed.TotalMilliseconds}ms");

            if ((int)sw.Elapsed.TotalMilliseconds > 250)
                builder.WithColor(Color.DarkRed);
            else if ((int)sw.Elapsed.TotalMilliseconds > 75)
                builder.WithColor(new Color(255, 255, 102));
            else
                builder.WithColor(new Color(102, 255, 102));

            await ReplyAsync("", false, builder.Build());
        }

        private static IUser ThisIsMe;
        //Sends a private message to ALL the users in the server
        [Command("msgall"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task MsgAllAsync()
        {
            var myId = Context.User.Mention;
            if (ThisIsMe == null)
            {
                foreach (var user in Context.Guild.Users)
                {
                    if (user.Id == 223895935539740672)
                    {
                        ThisIsMe = user;
                        myId = user.Mention;
                        break;
                    }
                }
            }
            SocketGuildUser[] usrs = Context.Guild.Users.ToArray<SocketGuildUser>();
            for (int i = 0; i < usrs.Length; i++)
            {
                if (!usrs[i].IsBot)
                {
                    var message = await usrs[i].GetOrCreateDMChannelAsync();
                    var embed = new EmbedBuilder()
                    {
                        Color = new Color(240, 230, 231)
                    };

                    embed.Description = $"Oi, eu sou a Serenity e isso é um teste." +
                        $"\n\nNão entre em panico, lembre que sua vida é miserável e eu nem existo!";
                    embed.WithFooter(new EmbedFooterBuilder().WithText($"Serenity -- {Context.Guild.Name}"));
                    await message.SendMessageAsync("", false, embed);
                    embed.Description = $"Você mandou uma mensagem pra {usrs[i].Mention} :monkas: ";

                    await Context.Channel.SendMessageAsync("", false, embed);
                }

            }
        }
    }
}
