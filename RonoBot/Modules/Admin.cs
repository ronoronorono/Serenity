using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace RonoBot.Modules
{
    public class Admin : ModuleBase<SocketCommandContext>
    {

        //Bans given user after a certain ammount of time, maximum time is 60s

        [Command("ban", RunMode = RunMode.Async) ,RequireUserPermission(GuildPermission.Administrator)]
        public async Task BanAsync(SocketGuildUser usr, int t)
        {
 
            //The bot wont be able to ban itself nor its owner
            if (usr.Mention.ToString() == Context.Client.CurrentUser.Mention.ToString() || usr.Id == 223895935539740672)
            {
                await Context.Channel.SendMessageAsync($"Haha boa tentativa.");
                return;
            }
            else if (usr == Context.User) //User shouldn't be able to ban himself
            {
                await Context.Channel.SendMessageAsync($"Não seja bobo, " + Context.User.Mention);
                return;
            }
            else
            {
                if (t > 60)
                {
                    await Context.Channel.SendMessageAsync($"Não vou perder mais de 1 minuto pra banir alguém.");
                    return;
                }
                //if the given time is less than 4 seconds, the user will be banned after a brief delay instead of the given time
                else if (t < 4 && t >= 0)
                {
                    await Context.Channel.SendMessageAsync($"Bom, sem mais delongas.");
                    Thread.Sleep(500);
                    await Context.Guild.AddBanAsync(usr);
                    await Context.Channel.SendMessageAsync($"Auf Wiedersehen, {usr.Mention}");
                    return;
                }
                //Of course, time cant be negative
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
                //Bot warns the user that he/she will be banned after the given time
                var msg = await Context.Channel.SendMessageAsync($"Ultimas palavras {usr.Mention} ? Você tem " + t + "s").ConfigureAwait(false);
                Thread.Sleep((t * 1000) - 3000);

                //Another warning, when there are only 3 seconds remaining
                msg = await Context.Channel.SendMessageAsync($"Tem mais 3 segundos para se esclarecer e refletir diante do inevitável {usr.Mention}").ConfigureAwait(false);
                Thread.Sleep(2850);

                //The farewell
                msg = await Context.Channel.SendMessageAsync($"Sayonara, {usr.Mention}").ConfigureAwait(false);
                Thread.Sleep(150);

                //Finally, the user is banned
                await Context.Guild.AddBanAsync(usr);
                await Context.Channel.SendMessageAsync($"Banido.");
            }
        }

        //Overload of the ban method. This bans the user instantly instead of waiting a given time
        [Command("ban"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task BanAsync(SocketGuildUser usr)
        {
            //User shouldn't be able to ban himself
            if (usr == Context.User)
            {
                await Context.Channel.SendMessageAsync($"Não seja bobo, " + Context.User.Mention);
                return;
            }

            await Context.Guild.AddBanAsync(usr);
            await Context.Channel.SendMessageAsync($"Au revoir, {usr.Mention}" +
                $"\n\nBanido.");
        }

        //Revokes the ban of a given user, however the parameter itself is a string since
        //a banned user cant be mentioned, thus the given string will be used to compare the
        //usernames that were banned until it finds the right one
        [Command("unban"),RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnbanAsync(params String[] usr)
        {
            //Gets all the bans in the server
            var bans = Context.Guild.GetBansAsync().Result.ToArray();

            String usrname = "";

            for (int j = 0; j<usr.Length; j++)
            {
                usrname += usr[j] + " ";
            }

            usrname = usrname.Trim();

            //Begins searching through all the bans until it finds the one containing the 
            //specified username given as a string
            for (int i = 0; i<bans.Length; i++)
            {
                //Begins comparing the given string with all the banned usernames,
                //if the given string contains the mention prefix "@" it will still work.
                if (bans[i].User.Username.ToString().ToLower() == usrname.ToLower() 
                    || "@"+bans[i].User.Username.ToString().ToLower() == usrname.ToLower())
                {
                    //first, the banned user must be put into a placeholder since
                    //after the ban it will no longer exist within the server, so there
                    //wont be any way to mention it properly.
                    RestUser placeholder = bans[i].User;

                    //Bans the user and returns.
                    await Context.Guild.RemoveBanAsync(bans[i].User);
                    await Context.Channel.SendMessageAsync($"{placeholder.Mention} desbanido(a) ");
                    return;
                }
            }
            
            //In case the specified user hasn't been found the bot will send a message
            //saying given user isn't banned.
            await Context.Channel.SendMessageAsync($"'{usr}' não está banido." +
                $"\n\nMas não hesite, banir ele é tão simples.");


        }

       
        //Removes the role of one more users
        [Command("rmcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRoleAsync(IRole role, params SocketGuildUser[] user)
        {
            String msg = "Cargo " + role.Mention;

            //Gets the user array and converts into a list to remove
            //specific cases
            List<SocketGuildUser> usrlist = user.ToList<SocketGuildUser>();

            //If the user is a bot he'll be removed from the list
            usrlist.RemoveAll(usr => usr.IsBot);

            //Users that doesn't have the given role will also be removed
            usrlist.RemoveAll(usr => !usr.Roles.Contains<IRole>(role));

            
            if (usrlist.Count > 1)
                msg += " removido dos usuarios: ";
            else if (usrlist.Count == 1)
                msg += " removido do usuario: ";
            //If the list size isn't >= 1 then there are no users to have their roles removed 
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
                //if the user count is greater than one and the current index points to the
                //list count minus 2, the penultimate member, a "and" will be placed in the return message
                if (usrlist.Count > 1 && i == usrlist.Count - 2)
                {
                    await usrlist[i].RemoveRoleAsync(role);
                    msg += usrlist[i].Mention + " e ";
                }
                //if the index points to the last member, no commas will be placed
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
                //in this case, the list has only one member so no commas have to be placed either
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

        //Overload of previous method, if the second parameter is "@everyone", all the users in the server
        //will have their roles removed
        [Command("rmcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRoleAsync(IRole role, IRole role2)
        {
         
                if (role2.ToString() == "@everyone")
                {
                String msg = "Cargo " + role.Mention;

                List<SocketGuildUser> usrlist = Context.Guild.Users.ToList<SocketGuildUser>();


                //If the user is a bot he'll be removed from the list
                usrlist.RemoveAll(usr => usr.IsBot);

                //Users that doesn't have the given role will also be removed
                usrlist.RemoveAll(usr => !usr.Roles.Contains<IRole>(role));


                if (usrlist.Count > 1)
                    msg += " removido dos usuarios: ";
                else if (usrlist.Count == 1)
                    msg += " removido do usuario: ";
                //If the list size isn't >= 1 then there are no users to have their roles removed 
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
                    //if the user count is greater than one and the current index points to the
                    //list count minus 2, the penultimate member, a "and" will be placed in the return message
                    if (usrlist.Count > 1 && i == usrlist.Count - 2)
                    {
                        await usrlist[i].RemoveRoleAsync(role);
                        msg += usrlist[i].Mention + " e ";
                    }
                    //if the index points to the last member, no commas will be placed
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
                    //in this case, the list has only one member so no commas have to be placed either
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

        }

        //Sets the given role to a user
        [Command("setcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetRoleAsync(IRole role, SocketGuildUser user)
        {
            await user.AddRoleAsync(role);
            await Context.Channel.SendMessageAsync($"Novo cargo de {user.Mention} --> {role.Mention}");
        }

        //Overload of previous method, works the same way "RemoveRoleAsync" does, however
        //setting a role instead of removing it
        [Command("setcargo"), RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetRoleAsync(IRole role, params SocketGuildUser[] user)
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
        public async Task SetRoleAsync(IRole role, IRole role2)
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

            if (role2.ToString() == "@everyone")
            {
                String msg = "Cargo " + role.Mention;

                List<SocketGuildUser> usrlist = Context.Guild.Users.ToList<SocketGuildUser>();

                usrlist.RemoveAll(usr => usr.IsBot);

                usrlist.RemoveAll(usr => usr.Roles.Contains<IRole>(role));


                if (usrlist.Count > 1)
                    msg += " adicionados para: ";
                else if (usrlist.Count == 1)
                    msg += " adicionado para: ";
                //If the list size isn't >= 1 then there are no users to have their roles removed 
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
                            msg += usrlist[i].Mention + ", ";
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
        }
        

        
        
        //Help command, shows the available commands as well as their descriptions and usages
        [Command("help")]
        public async Task HelpAsync()
        {
            await Context.Channel.SendMessageAsync("Meus comandos: " +
                "\n\n" +
                "http://htmlpreview.github.io/?https://github.com/ronoronorono/Serenity/blob/master/Commands.html");
        }


        
        //Deletes the last N messages
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

        //Deletes the last N messages from a specific user
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

       
        
        

    }
}
