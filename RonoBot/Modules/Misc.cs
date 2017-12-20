using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RonoBot.Modules
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        //Spams the user with a given number of private messages
        //Due to how discord handles requests, only 5 messages will be sent at a time
        //Note that this command is for entertainment purposes
        [Command("spam"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SpamAsync(SocketGuildUser user, int num)
        {

            SocketGuildUser usr = user;
            var message = await usr.GetOrCreateDMChannelAsync();

            var embed = new EmbedBuilder()
            {
                Color = new Color(240, 230, 231)
            };
            var embed2 = new EmbedBuilder()
            {
                Color = new Color(240, 230, 231)
            };
            int i = 0;

            embed.Description = $"Spammando {user.Mention} " + num + " vezes...";
            embed2.Description = $"{user.Mention} spammado com sucesso.";

            var msg = await Context.Channel.SendMessageAsync("", false, embed);



            while (i < num)
            {


                embed.Description = $"Oi, eu sou a Serenity e isso é um spam.";
                embed.WithFooter(new EmbedFooterBuilder().WithText($"Serenity -- {Context.Guild.Name}"));
                try
                {
                    await message.SendMessageAsync("", false, embed);
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync($"Não foi possível mandar mensagens para {user.Mention} " +
                        $"\n\nEle provavelmente cansou do spam e me blockou ¯\\_(ツ)_/¯");

                    return;
                }
                i++;
            }
            await msg.DeleteAsync().ConfigureAwait(false);
            await Context.Channel.SendMessageAsync("", false, embed2);
        }

        //Deletes the last message wrote by the whoever used this command.
        [Command("ops")]
        public async Task OpsAsync()
        {
            String[] quotes =
            {
                "Ficamos cientes do nada quando o preenchemos. " +
                "\n\n- Antonio Porchia",

                "A juventude sempre tenta preencher o vazio, a velhice aprende a conviver com ele." +
                "\n\n- Mark Z. Danielewski",

                "Nós podemos apenas saber que nada sabemos. E este é o maior grau da sabedoria humana." +
                "\n\n- Leo Tolstoy, Guerra e Paz",

                "Eu sou o homem mais sábio vivo, pois sei de uma coisa, e isto é que não sei de nada." +
                "\n\n- Platão",

                "IMessage = new IMessage();",

                "null",

                "¯\\_(ツ)_/¯",

                "Tudo bem, nínguem ia ver isso."
            };

            var messages = await this.Context.Channel.GetMessagesAsync().Flatten();
            int i = 0;
            int j = 0;

            List<IMessage> msgsdel = new List<IMessage>();

            while (i < 2 && j < messages.Count() && j < messages.Count<IMessage>())
            {
                if (messages.ElementAt(j).Author == Context.User)
                {
                    msgsdel.Add(messages.ElementAt(j));
                    i++;
                    j++;
                }
                else
                {
                    j++;
                }
            }

            Random rand = new Random();
            int idx = rand.Next(8);

            await this.Context.Channel.DeleteMessagesAsync(msgsdel);
            await Context.Channel.SendMessageAsync(quotes[idx]);
        }
    }
}
