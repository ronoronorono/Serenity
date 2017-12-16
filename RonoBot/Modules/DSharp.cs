using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Discord.Commands;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;

namespace RonoBot.Modules
{
    public class DSharp : ModuleBase<Discord.Commands.SocketCommandContext>
    {
        [DSharpPlus.CommandsNext.Attributes.Command("tst"), Description("Run a poll with reactions.")]
        public async Task Poll(DSharpPlus.CommandsNext.CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
            var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id && xm.Content.ToLower() == "b", TimeSpan.FromSeconds(10));
            if (msg != null)
                await ctx.RespondAsync($"?");
        }
    }
}
