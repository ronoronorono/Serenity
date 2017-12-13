using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RonoBot.Modules
{
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task PingAsync()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

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
        
        [Command("OI")]
        public async Task GreetingAsync()
        {
            if (Context.User.Id == 223895935539740672)
                await ReplyAsync("Saudações, mestre.");
            else
                await ReplyAsync($"Oi {Context.User.Mention}");
        }

        


    }
}
