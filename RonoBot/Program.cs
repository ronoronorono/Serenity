using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Audio;
using System.IO;
using RonoBot.Modules;

namespace RonoBot
{
    class Program
    {
        //Serenity is a discord bot written in C# with the purpose of testing and entertainment
        //I dont expect her to be used anywhere so the commands are very specific to the current server i own
        //
        //However depending on how far i end up developing this bot, i might make her able to be used in any server.


        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private AudioService _audio = new AudioService();
        private IServiceProvider _services;
        private string[] CustomReactions = { "OI" };
        public async Task RunBotAsync()
        {
            int i = 0;
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _audio = new AudioService();
            Music music = new Music(_audio);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(_audio)
                .BuildServiceProvider();
            //Gets the bot token
            SerenityCredentials token = new SerenityCredentials();

            String botToken = token.BotToken;

            //Log and UserJoined event handlers
            _client.Log += Log;
            _client.UserJoined += AnnounceUserJoined;
         

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botToken);

            await _client.StartAsync();

            await Task.Delay(-1);


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
            var aboninga = _client.GetChannel(391239397498159105) as SocketTextChannel; 
            await aboninga.SendMessageAsync("Olá " + user.Mention + ", bem vindo(a)."); 
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

            //This prevents the bot from answering commands when sent a private message.
            if (message.Channel.ToString() == "@" + message.Author.ToString())
            {
                await message.Channel.SendMessageAsync($"Para de falar comigo, isso é estranho.");
            }
            else
            {
                //This is where the commands are handled, the default prefix being ">" 

                //Note that, as stated before, this bot is currently only being hosted on my personal server for test/entertainment
                //purposes, so the response messages and how some commands are handled intends to be treated as a joke. Of course,
                //the core purpose of the command will act as intended, for instance, the ban command will indeed ban a given user.

                if (message.HasStringPrefix(">", ref argPos)) //|| message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    var context = new SocketCommandContext(_client, message);

                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess && result.ErrorReason.ToString() == "User not found.")
                    {
                        await message.Channel.SendMessageAsync($"Usuario não encontrado, talvez você esteja com algum " +
                        $"dos sintomas de esquizofrenia?" +
                        $"\n\nTalvez esse site possa te ajudar: http://schizophrenia.com/diag.html");
                    }
                    else if (!result.IsSuccess)
                        Console.WriteLine(result.ErrorReason);
                }
                //This is currently a placeholder to handle bot reactions
                else if (CustomReactions.Contains(message.Content.ToUpper()))
                {
                    string[] possiblereacs = { "Oi", "Olá", "Saudações", "Oie"};
                    Random rnd = new Random();


                    if (message.Author.Id == 223895935539740672)
                        await message.Channel.SendMessageAsync("Saudações mestre.");
                    else
                    {
                        int idx = rnd.Next(4);
                        await message.Channel.SendMessageAsync(possiblereacs[idx] + " "+message.Author.Mention);
                    }
                }
            }

        }
    }
}
