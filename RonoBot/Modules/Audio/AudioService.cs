using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Discord.WebSocket;

namespace RonoBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        public async Task JoinAudio(IGuild guild, IVoiceChannel target, SocketUser msgAuthor, IMessageChannel channel)
        {
            IAudioClient client;

            //If target's null, the user is not connected to a voice channel
            if(target==null)
            {
                await channel.SendMessageAsync(msgAuthor.Mention + ", você não está conectado(a) em nenhum canal de voz.");
                return;
            }
            //If the bot is already in a channel
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                return;
            }           
            if (target.Guild.Id != guild.Id)
            {
                return;
            }
            //Finally, if the flow stops here, the bot will join the voice channel the user is in
            try
            {
                var audioClient = await target.ConnectAsync();
                //The channel is added to the concurrentdictionary for futher operations
                ConnectedChannels.TryAdd(guild.Id, audioClient);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            IAudioClient client;

          if (ConnectedChannels.TryRemove(guild.Id, out client))
          {
                //Whenever a music playback starts, a ffmpeg process is created, thus, when the bot leaves
                //he must close an instance of this process if it exists.
                try
                {
                    foreach (Process proc in Process.GetProcessesByName("ffmpeg"))
                    {
                        proc.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }


                await client.StopAsync();
          }
            
        }

        //Searches youtube with the given query, playing the audio of the first video in the search result.
        public async Task SendAudioAsyncYT(IGuild guild, SocketUser usr, IMessageChannel channel, string query)
        {
            IAudioClient client;

            //If the bot isn't connected to any channel
            if (!ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                await channel.SendMessageAsync("Não estou conectada em nenhum canal de voz. " +
                    "\n\nUse o >join para me invocar no canal que você está conectado");
                return;
            }
            //If the bot is already connected to a voice channel, there is the possibility that it already
            //begun the music playback and currently a queue system hasn't been implemented, so if the user
            //requests to play another song while one is already being played, the current ffmpeg process must end
            //otherwise both songs will be played at same time
            else
            {
                try
                {
                    foreach (Process proc in Process.GetProcessesByName("ffmpeg"))
                    {
                        proc.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }

            }

            SerenityCredentials api = new SerenityCredentials();
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = api.GoogleAPIKey });

            //Creates the search request
            var searchListRequest = yt.Search.List("snippet");

            //With the given query
            searchListRequest.Q = query;
            searchListRequest.MaxResults = 10;

            //Placeholder variable which will store the first result of the search
            SearchResult result = null;
            try
            {
                //Starts the request and stores the first result
                SearchListResponse searchRequest = searchListRequest.Execute();
                result = searchRequest.Items[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Embed that will be shown in the channel, containing details about the song that will be played
            var embedImg = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))
                .WithTitle(result.Snippet.Title)
                .WithDescription("Busca: " + query)
                .WithUrl("https://www.youtube.com/watch?v=" + result.Id.VideoId)
                .WithImageUrl(result.Snippet.Thumbnails.Default__.Url)
                .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString()));

            //Embed that is shown when the music playback ends
            var embedEnd = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))
                .WithTitle(result.Snippet.Title)
                .WithDescription("Fim.")
                .WithUrl("https://www.youtube.com/watch?v=" + result.Id.VideoId)
                .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString()));


            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                try
                {
                    using (var output = CreateStream("https://www.youtube.com/watch?v=" + result.Id.VideoId).StandardOutput.BaseStream)
                    using (var stream = client.CreatePCMStream(AudioApplication.Music))
                    {
                        try { await channel.SendMessageAsync("", false, embedImg); await output.CopyToAsync(stream); }
                        finally
                        { await stream.FlushAsync(); }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            await channel.SendMessageAsync("", false, embedEnd);

        }

        /*
        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            
        }*/


        private Process CreateStream(string url)
        {
            Process currentsong = new Process();

            currentsong.StartInfo = new ProcessStartInfo
            {               
                FileName = "cmd.exe",
                Arguments = $"/C youtube-dl.exe -4 -f bestaudio -o - {url}| ffmpeg -i pipe:0 -vn -ac 2 -f s16le -ar 48000 pipe:1",
                //Arguments = $"/C youtube-dl.exe -4 -f bestaudio -o - ytsearch1:" +'"'+url+'"'+ " | ffmpeg -i pipe:0 -vn -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            };

            currentsong.Start();
            return currentsong;
        }


    }
}

