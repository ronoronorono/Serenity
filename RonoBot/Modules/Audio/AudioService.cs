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
using System.Threading;

namespace RonoBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        private Queue<YTSearchObject> YTSearchResultSong = new Queue<YTSearchObject>();

        private int currentSongID = 1;

        private CancellationTokenSource source = new CancellationTokenSource();

        private ExceptionHandler ehandler;

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
                ehandler = new ExceptionHandler(e);
                ehandler.WriteToFile();
                Console.WriteLine(e.Message);
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            IAudioClient client;

            YTSearchResultSong.Clear();

            currentSongID = 1;

            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                Killffmpeg();

                await client.StopAsync();
            }

        }

        //Checks if a url is a valid youtube one, following discord url parameters
        //which require a https:// body
        //Although this method is not the most correct way of finding videos ID via a youtube
        //url, it turns out to work in all cases so far, since all videos have an ID of length 11
        //While the youtube data api doesn't specify the specific size of a video ID, in cases where the length
        //were different, it always were larger by one digit and if you where to ommit said digit, you could still
        //find the video nevertheless
        public bool IsValidYTUrl(string url)
        {
            if (url.Length > 17)
                if (url.Substring(0, 17) == "https://youtu.be/" && url.Length >= 28)
                    return true;

            if (url.Length > 32)
                if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=" && url.Length >= 43)
                    return true;

            return false;                   
        }

        //Returns the ID of a youtube video from its url.
        //returns an empty string if the url is invalid
        public string GetYTVideoID(string url)
        {
            string id = "";


            if (IsValidYTUrl(url))
            {
                if (url.Substring(0, 17) == "https://youtu.be/")
                    return url.Substring(17, 11);
                //Not enterily necessary to check this case, however just to make sure...
                else if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=") 
                    return url.Substring(32, 11);

            }

            return id;
   
        }

       

        //Starts a youtube search with the given query, will return null if no results are found
        //Always returns the first result.
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
            catch (Exception e)
            {
                ehandler = new ExceptionHandler(e);
                ehandler.WriteToFile();
                Console.WriteLine(e.Message);
            }

            return result;
        }

        //kills any ffmpeg processes started by this program.
        public void Killffmpeg()
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

        /*public async Task SkipAudio (IGuild guild, SocketUser usr, IMessageChannel channel)
        {
            IAudioClient client;

            //Queue<YTSearchObject> aux = new Queue<YTSearchObject>();

            if (ConnectedChannels.TryGetValue(guild.Id, out client) && YTSearchResultSong.Count >= 1)
            {


                Killffmpeg();
                //Removes current song
                YTSearchObject skippedSong = YTSearchResultSong.Peek();

                source.Cancel();
                source = new CancellationTokenSource();
                //cancelSong = false;

                var embedSkip = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231))
                  .WithTitle("⏩ Skipped: " + skippedSong.Order+"# " + skippedSong.Ytresult.Snippet.Title)
                  .WithUrl("https://www.youtube.com/watch?v=" + skippedSong.Ytresult.Id.VideoId)
                  .WithImageUrl(skippedSong.Ytresult.Snippet.Thumbnails.Default__.Url)
                  .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString()));

                await channel.SendMessageAsync("", false, embedSkip);

               // YTSearchResultSong = new Queue<YTSearchObject>(aux);
                //Plays the next song if it exists
                if (YTSearchResultSong.Count >= 1)
                {
                    await SendAudioAsyncYT(guild, usr, channel, YTSearchResultSong.Peek(),source.Token);
                }

            }
        }*/

        public async Task ListQueue(IMessageChannel channel, SocketUser usr)
        {
            //If the queue is empty, warn users and return
            if (YTSearchResultSong.Count < 1)
            {
                var embedL = new EmbedBuilder()
                 .WithColor(new Color(240, 230, 231))
                 .WithDescription("Nenhuma musica na lista");
                await channel.SendMessageAsync("", false, embedL);
                return;
            }
                

            Queue<YTSearchObject> aux = new Queue<YTSearchObject>(YTSearchResultSong);

            var embedList = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231));

            int innercount = 1;

            while (aux.Count != 0)
            {
                YTSearchObject song = aux.Dequeue();
                EmbedFieldBuilder embedField = new EmbedFieldBuilder();

                string url = "https://www.youtube.com/watch?v=" + song.Ytresult.Id.VideoId;

                string duration =  await song.GetVideoDuration();
                //First song has a special marker indicating that is the one currently playing,
                //thats why the if/else clause
                if (innercount == 1)
                {
                    embedField.WithName("#" + innercount + " - Atual")
                              .WithIsInline(false)
                              .WithValue("[" + song.Ytresult.Snippet.Title + "](" + url + ")\n"+song.Author.Username.ToString() +" | "+ duration);
                              
                }
                else
                {
                    embedField.WithName("#" + innercount)
                              .WithIsInline(false)
                              .WithValue("[" + song.Ytresult.Snippet.Title + "](" + url + ")\n"+song.Author.Username.ToString() + " | " + duration);
                }

                embedList.AddField(embedField);
                innercount++;
            }



            await channel.SendMessageAsync("", false, embedList);
        }

        public async Task QueueAudio(IGuild guild, SocketUser usr, IMessageChannel channel, IVoiceChannel target, string query)
        {
            try
            {
                bool firstsong = false;

                //If the bot isn't in any voice channel it will automatically attempt to join the channel the user is connected in
                await JoinAudio(guild, target, usr, channel);


                //Searches youtube with the given query and if results are found, they are inserted
                //in the queue.
                SearchResult Song = YoutubeSearch(query);

                if (Song == null)
                {
                    await channel.SendMessageAsync("Não foram encontrados resultados para: "+
                        "\n\n"+query);
                    return;
                }

                YTSearchObject newSong = new YTSearchObject(Song, query, currentSongID, usr);
                YTSearchResultSong.Enqueue(newSong);

                if (currentSongID == 1)
                    firstsong = true;

                currentSongID++;

                string url = "https://www.youtube.com/watch?v=" + newSong.Ytresult.Id.VideoId;

                string duration = await newSong.GetVideoDuration();

                //Embed that will be shown in the channel, containing details about the song that will be played
                var embedQueue = new EmbedBuilder()
                    .WithColor(new Color(240, 230, 231))
                    .WithTitle("#"+(newSong.Order) +"  "+ newSong.Ytresult.Snippet.Title)
                    .WithDescription("Busca: " + query)
                    .WithUrl(url)
                    .WithImageUrl(newSong.Ytresult.Snippet.Thumbnails.Default__.Url)
                    .WithFooter(new EmbedFooterBuilder().WithText(newSong.Author.Username.ToString() + " | " + duration));       

                var msg = await channel.SendMessageAsync("", false, embedQueue).ConfigureAwait(false);
                await Task.Delay(5000);
                await msg.DeleteAsync().ConfigureAwait(false);

                //If the currentSongID is 1 this means thats the first song added to the queue, thus
                //it's playback should begin as soon as it's added.
                if (firstsong)
                {
                    firstsong = false;
                    await SendAudioAsyncYT(guild, usr, channel, YTSearchResultSong.Peek(), source.Token);
                }
            }
            catch(Exception e)
            {
                ehandler = new ExceptionHandler(e);
                ehandler.WriteToFile();
                await LeaveAudio(guild);
            }

        }
        
        public async Task SendAudioAsyncYT(IGuild guild, SocketUser usr, IMessageChannel channel, YTSearchObject ytSO, CancellationToken cts)
        {
            try
            {

                while (YTSearchResultSong.Count >= 1)
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

                        Killffmpeg();

                        string duration = await ytSO.GetVideoDuration();

                        //Embed that will be shown in the channel, containing details about the song that will be played
                        var embedImg = new EmbedBuilder()
                            .WithColor(new Color(240, 230, 231))
                            .WithTitle("🎶  " + ytSO.Order + "#  " + ytSO.Ytresult.Snippet.Title)
                            .WithUrl("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId)
                            .WithImageUrl(ytSO.Ytresult.Snippet.Thumbnails.Default__.Url)
                            .WithFooter(new EmbedFooterBuilder().WithText(usr.Username.ToString() + " | " + duration));

                        //Embed that is shown when the music playback ends
                        var embedEnd = new EmbedBuilder()
                            .WithColor(new Color(240, 230, 231))
                            .WithTitle(ytSO.Order + "# " + ytSO.Ytresult.Snippet.Title)
                            .WithDescription("Fim.")
                            .WithUrl("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId)
                            .WithFooter(new EmbedFooterBuilder().WithText(usr.Username.ToString() + " | " + duration));


                        if (ConnectedChannels.TryGetValue(guild.Id, out client))
                        {
                            try
                            {
                                using (var output = PlayYt("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId).StandardOutput.BaseStream)
                                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                                {
                                    try
                                    {
                                        var msgImg = await channel.SendMessageAsync("", false, embedImg);
                                        await output.CopyToAsync(stream);
                                        await Task.Delay(4000);
                                        await msgImg.DeleteAsync();
                                    }
                                    finally
                                    {  await stream.FlushAsync(cts); }
                                }
                            }
                            catch (Exception e)
                            {
                                ehandler = new ExceptionHandler(e);
                                ehandler.WriteToFile();
                                await LeaveAudio(guild);                           
                            }

                        }

                        //if (!cancelSong)
                        //{
                        YTSearchResultSong.Dequeue();
                        var msgEnd = await channel.SendMessageAsync("", false, embedEnd);
                        await Task.Delay(4000);
                        await msgEnd.DeleteAsync();

                        if (YTSearchResultSong.Count >= 1)
                        {
                            ytSO = YTSearchResultSong.Peek();
                        }
                        else
                        {
                            currentSongID = 1;
                        }
                    }

                }

            }
            catch(OperationCanceledException)
            {
                /*var embedEnd = new EmbedBuilder()
                            .WithColor(new Color(240, 230, 231))
                            .WithTitle(ytSO.Order + "# " + ytSO.Ytresult.Snippet.Title)
                            .WithDescription("Fim.")
                            .WithUrl("https://www.youtube.com/watch?v=" + ytSO.Ytresult.Id.VideoId)
                            .WithFooter(new EmbedFooterBuilder().WithText("-" + usr.Username.ToString()));
    
    */
                Console.WriteLine("***************OPERATION STOPPED*****************");
                YTSearchResultSong.Dequeue();
               // await channel.SendMessageAsync("", false, embedEnd);

                if (YTSearchResultSong.Count >= 1)
                {
                    ytSO = YTSearchResultSong.Peek();
                }
                else
                {
                    currentSongID = 1;
                }

                return;
            }
        }

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

