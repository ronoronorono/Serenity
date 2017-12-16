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

            var msg = await Context.Channel.SendMessageAsync($"Ultimas palavras {usr.Mention} ? Você tem "+t+"s").ConfigureAwait(false);
            Thread.Sleep((t * 1000) - 3000);
            //await msg.DeleteAsync().ConfigureAwait(false);
            msg = await Context.Channel.SendMessageAsync($"Tem mais 3 segundos para se esclarecer e refletir diante do inevitável {usr.Mention}").ConfigureAwait(false);
            Thread.Sleep(2850);
            msg = await Context.Channel.SendMessageAsync($"Sayonara, {usr.Mention}").ConfigureAwait(false);
            Thread.Sleep(150);
            await Context.Guild.AddBanAsync(usr);

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
            int i = 0;
            while (i < num)
            {
                embed.Description = $"Oi, eu sou a Serenity e isso é um spam.";
                embed.WithFooter(new EmbedFooterBuilder().WithText($"Serenity -- {Context.Guild.Name}"));
                await message.SendMessageAsync("", false, embed);
                embed.Description = $"Você spammou {user.Mention} ";

                await Context.Channel.SendMessageAsync("", false, embed);
                i++;
            }
        }
        
        /*[Command("help")]
        public async Task HelpAsync(SocketCommandContext cmd)
        {
            
        }*/


        private static IUser ThisIsMe;


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
