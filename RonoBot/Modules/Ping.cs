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
    public class Ping : ModuleBase<SocketCommandContext>
    {

        [Command("ban", RunMode = RunMode.Async) ,RequireUserPermission(GuildPermission.Administrator)]
        public async Task RespAsync(SocketGuildUser usr, int t)
        {
            if (usr.Mention.ToString() == Context.Client.CurrentUser.Mention.ToString() || usr.Id == 223895935539740672)
            {
                await Context.Channel.SendMessageAsync($"Haha boa tentativa.");
            }
            else
            {
                if (t > 60)
                {
                    await Context.Channel.SendMessageAsync($"Não vou perder mais de 1 minuto pra banir alguém.");
                    return;
                }
                else if (t < 4 && t >= 0)
                {
                    await Context.Channel.SendMessageAsync($"Bom, sem mais delongas.");
                    Thread.Sleep(500);
                    await Context.Guild.AddBanAsync(usr);
                    await Context.Channel.SendMessageAsync($"Auf Wiedersehen, {usr.Mention}");
                    return;
                }
                else if (t < 0)
                {
                    await Context.Channel.SendMessageAsync($"Por mais interessante que seja o seu parâmetro, dotado de uma quantidade" +
                        $" de tempo negativa, sinto dizer que, pelo nosso atual entendimento da física o tempo não pode ser negativo." +
                        $"\n\nTalvez seja muito para sua mente primitiva compreender, mas o tempo é uma entidade funcional, " +
                        $"idealizada por seres racionais para representar relações temporais." +
                        $"Contudo, não sendo uma entidade real, ela não ocupa um espaço fisico e/ou possui uma direção." +
                        $"\n\n A presuposição de viajar no tempo em uma direção específica é fruto de nossa concepção de relacionar causa e efeito." +
                        $"A causa sempre irá preceder o efeito e o efeito sempre será seguido de uma causa. Não é possível inverter essa relação." +
                        $"Como já estabelecemos uma relação temporal dessa forma, não podemos, simultaneamente estabelecer outra, numa direção oposta." +
                        $"Inverter essa relação seria o mesmo que fazer o efeito preceder a causa.");
                    return;
                }

                var msg = await Context.Channel.SendMessageAsync($"Ultimas palavras {usr.Mention} ? Você tem " + t + "s").ConfigureAwait(false);
                Thread.Sleep((t * 1000) - 3000);
                //await msg.DeleteAsync().ConfigureAwait(false);
                msg = await Context.Channel.SendMessageAsync($"Tem mais 3 segundos para se esclarecer e refletir diante do inevitável {usr.Mention}").ConfigureAwait(false);
                Thread.Sleep(2850);
                msg = await Context.Channel.SendMessageAsync($"Sayonara, {usr.Mention}").ConfigureAwait(false);
                Thread.Sleep(150);
                await Context.Guild.AddBanAsync(usr);

            }
        }

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


        [Command("rmcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRoleAsync(IRole role, params SocketGuildUser[] user)
        {
            String msg = "Cargo " + role.Mention;

            List<SocketGuildUser> usrlist = user.ToList<SocketGuildUser>();
            usrlist.RemoveAll(usr => usr.IsBot);
            usrlist.RemoveAll(usr => !usr.Roles.Contains<IRole>(role));

            if (usrlist.Count > 1)
                msg += " removido dos usuarios: ";
            else if (usrlist.Count == 1)
                msg += " removido do usuario: ";
            else
            {
                var embedR = new EmbedBuilder()
                {
                    Color = new Color(240, 230, 231)
                };

                embedR.Description = "Todos os usuario(s) especificados não possuem o cargo fornecido)";
                await Context.Channel.SendMessageAsync("", false, embedR);
                return;
            }


            for (int i = 0; i < usrlist.Count; i++)
            {
                if (usrlist.Count > 1 && i == usrlist.Count - 2)
                {
                    await usrlist[i].RemoveRoleAsync(role);
                    msg += usrlist[i].Mention + " e ";
                }
                else if (usrlist.Count > 1 && i == usrlist.Count - 1)
                {
                    await usrlist[i].RemoveRoleAsync(role);
                    msg += usrlist[i].Mention;
                }
                else if (usrlist.Count > 1)
                {
                    await usrlist[i].RemoveRoleAsync(role);
                    msg += usrlist[i].Mention + ", ";
                }
                else
                {
                    await usrlist[i].RemoveRoleAsync(role);
                    msg += usrlist[i].Mention;
                }
            }



            var embed = new EmbedBuilder()
            {
                Color = new Color(240, 230, 231)
            };

            embed.Description = msg;
            await Context.Channel.SendMessageAsync("", false, embed);

        }

        [Command("rmcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRoleAsync(IRole role, IRole role2)
        {
         
                if (role2.ToString() == "@everyone")
                {
                    SocketGuildUser[] usrs = Context.Guild.Users.ToArray<SocketGuildUser>();
                    for (int i = 0; i < usrs.Length; i++)
                    {
                        if (!usrs[i].IsBot)
                        {
                            if (usrs[i].Roles.Contains<IRole>(role))
                            {
                                await usrs[i].RemoveRoleAsync(role);
                                await Context.Channel.SendMessageAsync($"Cargo {role.Mention} removido de {usrs[i].Mention}");
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync($"{usrs[i].Mention} não possui o cargo especificado. " +
                                    $"\nTalvez você seja um idiota.");
                            }
                        }

                    }
                }
            
          
        }


        [Command("setcargo"),RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RoleAsync(IRole role, SocketGuildUser user)
        {

            
                await user.AddRoleAsync(role);
                await Context.Channel.SendMessageAsync($"Novo cargo de {user.Mention} --> {role.Mention}");
            
        }

        [Command("setcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RoleAsync(IRole role, params SocketGuildUser[] user)
        {
            String msg = "Cargo " + role.Mention;

            List < SocketGuildUser > usrlist = user.ToList<SocketGuildUser>();
            usrlist.RemoveAll(usr => usr.IsBot);
            usrlist.RemoveAll(usr => usr.Roles.Contains<IRole>(role));

            if (usrlist.Count > 1)
                msg += " adicionados para: ";
            else if (usrlist.Count == 1)
                msg += " adicionado para: ";
            else
            {
                var embedR = new EmbedBuilder()
                {
                    Color = new Color(240, 230, 231)
                };

                embedR.Description = "Todos os usuario(s) especificados possuem o cargo dado e/ou são inválido(s)";
                await Context.Channel.SendMessageAsync("", false, embedR);
                return;
            }

            

            for (int i = 0; i < usrlist.Count; i++)
            {
                if (usrlist.Count > 1 && i == usrlist.Count - 2)
                {
                    await usrlist[i].AddRoleAsync(role);
                    msg += usrlist[i].Mention + " e ";
                }
                else if (usrlist.Count > 1 && i == usrlist.Count - 1)
                {
                    await usrlist[i].AddRoleAsync(role);
                    msg += usrlist[i].Mention;
                }
                else if (usrlist.Count > 1)
                {
                    await usrlist[i].AddRoleAsync(role);
                    msg += usrlist[i].Mention+", ";
                }
                else
                {
                    await usrlist[i].AddRoleAsync(role);
                    msg += usrlist[i].Mention;
                }
            }



                var embed = new EmbedBuilder()
                {
                    Color = new Color(240, 230, 231)
                };
  
                embed.Description = msg;
                await Context.Channel.SendMessageAsync("", false, embed);

                
        }


        [Command("setcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RoleAsync(IRole role, IRole role2)
        {

            if (role2.ToString() == "@everyone")
            {
                SocketGuildUser[] usrs = Context.Guild.Users.ToArray<SocketGuildUser>();
                for (int i = 0; i < usrs.Length; i++)
                {
                    if (!usrs[i].IsBot)
                    {
                        await usrs[i].AddRoleAsync(role);
                        await Context.Channel.SendMessageAsync($"Novo cargo de {usrs[i].Mention} --> {role.Mention}");
                    }
                }
            }
        }


        [Command("spam"),RequireUserPermission(GuildPermission.Administrator)]
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
           
            embed.Description = $"Spammando {user.Mention} "+num+" vezes...";
            embed2.Description = $"{user.Mention} spammado com sucesso.";

            var msg = await Context.Channel.SendMessageAsync("", false, embed);



            while (i < num)
            {

                
                embed.Description = $"Oi, eu sou a Serenity e isso é um spam.";
                embed.WithFooter(new EmbedFooterBuilder().WithText($"Serenity -- {Context.Guild.Name}"));

                
                try
                {
                    await  message.SendMessageAsync("", false, embed);
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
        
        [Command("help")]
        public async Task HelpAsync()
        {
            await Context.Channel.SendMessageAsync("Meus comandos: " +
                "\n\n" +
                "http://htmlpreview.github.io/?https://github.com/ronoronorono/Serenity/blob/master/Commands.html");
        }


        private static IUser ThisIsMe;

        [Command("delmsg"),RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteMsgsAsync(int n)
        {
            if (n == 0)
            {
                await Context.Channel.SendMessageAsync("0 Mensagens deletadas, uau.");
                return;
            }
            else if (n < 0)
            {
                await Context.Channel.SendMessageAsync(n + " Mensagens deletadas :thinking: " +
                    "\n\nSugiro ler: https://pt.wikipedia.org/wiki/Contagem_(matemática)");
                return;
            }

            var messages = await this.Context.Channel.GetMessagesAsync(n+1).Flatten();
            await this.Context.Channel.DeleteMessagesAsync(messages);
            await Context.Channel.SendMessageAsync(messages.Count()-1+" Mensagens deletadas.");
        }

        [Command("delmsgusr"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteMsgsUserAsync(SocketGuildUser u, int n)
        {

            if (n == 0)
            {
                await Context.Channel.SendMessageAsync("0 Mensagens deletadas ¯\\_(ツ)_/¯");
                return;
            }
            else if (n < 0)
            {
                await Context.Channel.SendMessageAsync(n + " Mensagens deletadas :thinking: " +
                "\n\nSugiro ler: https://pt.wikipedia.org/wiki/Contagem_(matemática)");
                return;
            }

            var messages = await this.Context.Channel.GetMessagesAsync().Flatten();
            int i = 0;
            int j = 0;

            List<IMessage> msgsdel = new List<IMessage>();

            while (i < n && j < messages.Count() && j < messages.Count<IMessage>())
            {
                if (messages.ElementAt(j).Author == u)
                {
                    if (j == 0 && messages.ElementAt(j).Author == Context.Message.Author)
                    {
                        //await messages.ElementAt(j).DeleteAsync();
                        j++;
                    }
                    else
                    {
                        msgsdel.Add(messages.ElementAt(j));
                        i++;
                        j++;
                    }
                }
                else
                {
                    j++;
                }
            }

            await this.Context.Channel.DeleteMessagesAsync(msgsdel);
            await Context.Channel.SendMessageAsync(msgsdel.Count+ " Mensagens deletadas do usuario "+u.Mention);
        }

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

            while (i < 1 && j < messages.Count() && j < messages.Count<IMessage>())
            {
                if (messages.ElementAt(j).Author == Context.User)
                {
                    /* if (j == 0 && messages.ElementAt(j).Author == Context.Message.Author)
                    {
                        //await messages.ElementAt(j).DeleteAsync();
                        j++;
                    }
                    else
                    {*/
                        msgsdel.Add(messages.ElementAt(j));
                        i++;
                        j++;
                    //}
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
        

        [Command("t"), RequireOwner]
        public async Task AddMsgsAsync(int n)
        {
            // IEnumerable<IMessage> msgs = Context.Channel.GetCachedMessages(n);
            //IAsyncEnumerable<IReadOnlyCollection<IMessage>> MSGG = Context.Channel.GetMessagesAsync(n, CacheMode.AllowDownload);
            //var msgs = Context.Channel.GetMessagesAsync(n, CacheMode.AllowDownload);
            for (int i = 0; i < n; i++)
                await Context.Channel.SendMessageAsync(""+(i+1));
        }

        [Command("msgall"),RequireUserPermission(GuildPermission.Administrator)]
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

            //var application = await Context.Client.GetApplicationInfoAsync();
            SocketGuildUser[] usrs = Context.Guild.Users.ToArray<SocketGuildUser>();
            for (int i = 0; i < usrs.Length; i++)
            {
                if (!usrs[i].IsBot)
                {
                    var message = await usrs[i].GetOrCreateDMChannelAsync();
                    //var invite = Context.Guild.DefaultChannel.CreateInviteAsync(Int32.MaxValue, Int32.MaxValue, false, false);
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
