using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;


namespace RonoBot
{
    class Program
    {
        //Test change github
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private string[] CustomReactions = { "OI" };

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

<<<<<<< HEAD
            string token;
            var fs = new FileStream("token.txt", FileMode.Open, FileAccess.Read);

            using (var reader = new StreamReader(fs))
{
                token = reader.ReadLine();
            }
            String botToken = token;
=======
            String botToken = "";
>>>>>>> 742fd1c5f5742d8b8664770948a669be9089b749

            _client.Log += Log;
            _client.UserJoined += AnnounceUserJoined;
            _client.UserBanned += AnnounceUserBanned;
            

            

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botToken);

            await _client.StartAsync();


            //if (aboninga != null)
            //await Dm(aboninga);

            await Task.Delay(-1);


        }

        private async Task AnnounceUserBanned(SocketUser user, SocketGuild guild)
        {
            var aboninga = _client.GetChannel(391239397498159105) as SocketTextChannel; //gets channel to send message in
            await aboninga.SendMessageAsync("Banido."); //Welcomes the new user
        }

        private async Task AnnounceUserJoined(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.DefaultChannel;

            SocketRole[] roles = guild.Roles.ToArray<SocketRole>();
            SocketRole carbon = null;
            for (int i = 0; i < roles.Length; i++)
                if (roles[i].Id == 391245533747871756)
                {
                    Console.WriteLine(roles[i].ToString());
                    carbon = roles[i];
                }

            await user.AddRoleAsync(carbon);
            var aboninga = _client.GetChannel(391239397498159105) as SocketTextChannel; //gets channel to send message in
            await aboninga.SendMessageAsync("Olá, " + user.Mention + " bem vindo(a)."); //Welcomes the new user
        }
        
    


        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);

            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot) return;

            int argPos = 0;

            if (message.Channel.ToString() == "@" + message.Author.ToString())
            {
                await message.Channel.SendMessageAsync($"Para de falar comigo, isso é estranho.");
            }
            else
            {
                if (message.HasStringPrefix(">", ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    var context = new SocketCommandContext(_client, message);

                    var result = await _commands.ExecuteAsync(context, argPos, _services);

                    if (!result.IsSuccess)
                        Console.WriteLine(result.ErrorReason);
                }
                else if (CustomReactions.Contains(message.Content.ToUpper()))
                {
                    string[] possiblereacs = { "Oi", "Olá", "Saudações", "Oie"};
                    Random rnd = new Random();


                    if (message.Author.Id == 223895935539740672)
                        await message.Channel.SendMessageAsync("Saudações mestre.");
                    else if (message.Author.Id == 206208126171611137)
                        await message.Channel.SendMessageAsync("Você é patético.");
                    else
                    {
                        int idx = rnd.Next(4);
                        await message.Channel.SendMessageAsync(possiblereacs[idx] + " "+message.Author.Mention);
                    }


                    //if (!result.IsSuccess)
                    //  Console.WriteLine(result.ErrorReason);
                }
            }

        }
    }
}
