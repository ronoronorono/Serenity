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
using RonoBot.Modules.Audio;

namespace RonoBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        private Queue<YTSearchObject> YTSearchResultSong = new Queue<YTSearchObject>();

        private int currentSongID = 1;

        public async Task JoinAudio(IGuild guild, IVoiceChannel target, SocketUser msgAuthor, IMessageChannel channel)
        {
            IAudioClient client;

            //If target's null, the user is not connected to a voice channel
            if (target == null)
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

            YTSearchResultSong.Clear();

            currentSongID = 1;

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

        //Returns the ID of a youtube video from its url.
        //returns an empty string if the url is invalid
        public string GetYTVideoID(string url)
        {
            string id = "";

            if (url.Length < 27)
                return id;

            if (url.Substring(0, 17) == "https://youtu.be/")
            {
                id = url.Substring(17, 11);
                return id;
            }

            else if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=")
            {
                id = url.Substring(32, 11);
                return id;
            }

            return id;
        }

        public SearchResult YoutubeSearch(string query)
        {
            SerenityCredentials api = new SerenityCredentials();
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = api.GoogleAPIKey });


            //Creates the search request
            var searchListRequest = yt.Search.List("snippet");


            SearchResult result = null;

            if (GetYTVideoID(query) != "")
                query = GetYTVideoID(query);

            //With the given query
            
            searchListRequest.Q = query;
            searchListRequest.MaxResults = 10;

            //Placeholder variable which will store the first result of the search

            try
            {
                //Starts the request and stores the first result
                SearchListResponse searchRequest = searchListRequest.Execute();
                if (searchRequest.Items.Count < 1)
                {
                    return null;
                }
                else
                    result = searchRequest.Items[0];
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public async Task QueueAudio(IGuild guild, SocketUser usr, IMessageChannel channel, IVoiceChannel target, string query)
        {
            try
            {
                IAudioClient client;

                //If the bot isn't in any channel it will automatically join the channel the user is connected in
                if (!ConnectedChannels.TryGetValue(guild.Id, out client))
                {
                    if (target == null)
                    {
                        await channel.SendMessageAsync(usr.Mention + ", você não está conectado(a) em nenhum canal de voz.");
                        return;
                    }
                    try
                    {
                        var audioClient = await target.ConnectAsync();
                        ConnectedChannels.TryAdd(guild.Id, audioClient);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message.ToString());
                    }
                }

                //Searches youtube with the given query and if results are found, they are inserted
                //in the queue.
                SearchResult Song = YoutubeSearch(query);
                YTSearchResultSong.Enqueue(new YTSearchObject(Song,query,currentSongID));

                //Embed that will be shown in the channel, containing details about the song that will be played
                var embedQueue = new EmbedBuilder()
                    .WithColor(new Color(240, 230, 231))
                    .WithTitle((currentSongID) + "# " + Song.Snippet.Title)
                    .WithDescription("Busca: " + query)
                    .WithUrl("https://www.youtube.com/watch?v=" + Song.Id.VideoId)
                    .WithImageUrl(Song.Snippet.Thumbnails.Default__.Url)
                    .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString()));

                await channel.SendMessageAsync("", false, embedQueue);

                //If the currentSongID is 1 this means thats the first song added to the queue, thus
                //it's playback should begin as soon as it's added.
                if (currentSongID == 1)
                {
                    currentSongID++;
                    await SendAudioAsyncYT(guild, usr, channel, YTSearchResultSong.Dequeue());
                }
                else
                    currentSongID++;
            }
            catch(Exception err)
            {

                Console.WriteLine(err.Message.ToString());
            }

        }


        
        public async Task SendAudioAsyncYT(IGuild guild, SocketUser usr, IMessageChannel channel, YTSearchObject ytSO)
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

                //Embed that will be shown in the channel, containing details about the song that will be played
                var embedImg = new EmbedBuilder()
                    .WithColor(new Color(240, 230, 231))
                    .WithTitle("🎶  "+ytSO.Order+"#  "+ ytSO.Ytresult.Snippet.Title)
                    .WithUrl("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId)
                    .WithImageUrl(ytSO.Ytresult.Snippet.Thumbnails.Default__.Url)
                    .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString() + "|"));

                //Embed that is shown when the music playback ends
                var embedEnd = new EmbedBuilder()
                    .WithColor(new Color(240, 230, 231))
                    .WithTitle(ytSO.Order+"# "+ytSO.Ytresult.Snippet.Title)
                    .WithDescription("Fim.")
                    .WithUrl("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId)
                    .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString()));


                if (ConnectedChannels.TryGetValue(guild.Id, out client))
                {
                    try
                    {
                        using (var output = PlayYt("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId).StandardOutput.BaseStream)
                        using (var stream = client.CreatePCMStream(AudioApplication.Music))
                        {
                            try { await channel.SendMessageAsync("", false, embedImg); await output.CopyToAsync(stream); }
                            finally
                            { await stream.FlushAsync(); }

                        }
                    }
                    catch (Exception exe)
                    {
                        Console.WriteLine(exe.Message);
                    }

                }
                await channel.SendMessageAsync("", false, embedEnd);

                if (YTSearchResultSong.Count >= 1)
                {
                    await SendAudioAsyncYT(guild, usr, channel, YTSearchResultSong.Dequeue());
                }
                else
                {
                    currentSongID = 1;
                }
            }
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


                private Process PlayYt(string url)
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

