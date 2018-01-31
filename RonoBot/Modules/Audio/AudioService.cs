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

        private Queue<YTSong> YTSearchResultSong = new Queue<YTSong>();

        private static int currentSongID = 0;

        private CancellationTokenSource source = new CancellationTokenSource();

        private ExceptionHandler ehandler;

        private YTVideoOperation ytVideoOp = new YTVideoOperation();

        //private static bool first = false;

        public int GetCurrentSongID()
        {
            return currentSongID;
        }

        public void IncrementID()
        {
            currentSongID++;
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target, SocketUser msgAuthor, IMessageChannel channel)
        {
            IAudioClient client;


            //If target's null, the user is not connected to a voice channel
            if (target == null)
            {
                await channel.SendMessageAsync(msgAuthor.Mention + ", você não está conectado(a) em nenhum canal de voz.");
                return;
            }

            var users = await guild.GetUsersAsync();
            foreach (var usr in users)
            {
                if (usr.Id == 390402848443203595)
                {
                    if (usr.VoiceChannel != null)
                    {
                        client = await usr.VoiceChannel.ConnectAsync();
                        ConnectedChannels.TryAdd(guild.Id, client);
                    }
                }
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
                ClearQueue();
                await LeaveAudio(guild, target);
            }
        }

        public async Task LeaveAudio(IGuild guild, IVoiceChannel target)
        {
            IAudioClient client;



            var users = await guild.GetUsersAsync();

            Killffmpeg();

            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await client.StopAsync();
                return;
            }

            //Disconnects the bot from any voice channel it might be connected
            foreach (var usr in users)
            {
                if (usr.Id == 390402848443203595)
                {
                    if (usr.VoiceChannel != null)
                    {
                        client = await usr.VoiceChannel.ConnectAsync();
                        await client.StopAsync();
                    }
                }
            }

        }

        public void ClearQueue()
        {
            YTSearchResultSong.Clear();
            currentSongID = 0;
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
                

            Queue<YTSong> aux = new Queue<YTSong>(YTSearchResultSong);

            var embedList = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231));

            int innercount = 1;

            while (aux.Count != 0)
            {
                YTSong song = aux.Dequeue();
                EmbedFieldBuilder embedField = new EmbedFieldBuilder();

                string url = "https://www.youtube.com/watch?v=" + song.Ytresult.Id.VideoId;

                string duration = song.Duration;

                string usrRequest = song.RequestAuthor.Username.ToString();

                string title = song.Ytresult.Snippet.Title;
                //First song has a special marker indicating that is the one currently playing,
                //thats why the if/else clause
                if (innercount == 1)
                {
                    embedField.WithName("#" + innercount + " - Atual")
                              .WithIsInline(false)
                              .WithValue("[" + title + "](" + url + ")\n"+ usrRequest +" | "+ duration);
                              
                }
                else
                {
                    embedField.WithName("#" + innercount)
                              .WithIsInline(false)
                              .WithValue("[" + title + "](" + url + ")\n"+usrRequest + " | " + duration);
                }

                embedList.AddField(embedField);
                innercount++;
            }



            await channel.SendMessageAsync("", false, embedList);
        }

        public EmbedBuilder QueueAudio(IGuild guild, SocketUser usr, IMessageChannel channel, IVoiceChannel target, string query)
        {
            try
            {               
                currentSongID++;

                //Searches youtube with the given query and if results are found, they are inserted
                //in the queue.
                SearchResult Song = ytVideoOp.YoutubeSearch(query);

                //If the result is null, that means no results were found
                if (Song == null)
                {
                    currentSongID--;
                    var embedNoSong = new EmbedBuilder()
                       .WithColor(new Color(240, 230, 231))
                       .WithDescription("Nenhum video encontrado");      

                    return embedNoSong;
                }

                YTSong newSong = new YTSong(Song, query, currentSongID, usr);
                YTSearchResultSong.Enqueue(newSong);

                string url = "https://www.youtube.com/watch?v=" + newSong.Ytresult.Id.VideoId;

                //string duration = newSong.Duration;

                //Embed that will be shown in the channel, containing details about the song that will be played
                var embedQueue = new EmbedBuilder()
                    .WithColor(new Color(240, 230, 231))
                    .WithTitle("#" + (newSong.Order) + "  " + newSong.Ytresult.Snippet.Title)
                    .WithDescription("Busca: " + query)
                    .WithUrl(url)
                    .WithImageUrl(newSong.Ytresult.Snippet.Thumbnails.Default__.Url)
                    .WithFooter(new EmbedFooterBuilder().WithText(newSong.RequestAuthor.Username.ToString() + " | "));//+ duration));       

                return embedQueue;
            }
            catch(Exception e)
            {
                ehandler = new ExceptionHandler(e);
                ehandler.WriteToFile();
                ClearQueue();
                return null;
            }            
        }
        
        public async Task SendAudioAsyncYT(IGuild guild, IMessageChannel channel, IVoiceChannel target, SocketUser usr)
        {
            try
            {

                while (YTSearchResultSong.Count >= 1)
                {
                    IAudioClient client;
                    Killffmpeg();
                    //If the bot isn't connected to any channel, it will attempt to join one before starting
                    //the music playback
                    if (!ConnectedChannels.TryGetValue(guild.Id, out client))
                    {
                        //Sometimes even after the bot disconnects, it will still remain in a voice
                        //channel, however without any audio outputs, so this is done in order to prevent
                        //a muted song being played
                        await LeaveAudio(guild, target);

                        await JoinAudio(guild, target, usr, channel);
                    }

                    YTSong currentSong = YTSearchResultSong.Peek();
                    
                    int songOrder = currentSong.Order;
                    string songID = currentSong.Ytresult.Id.VideoId;
                    string songTitle = currentSong.Ytresult.Snippet.Title;
                    string duration = currentSong.Duration;

                    //Not to be confounded with the video's author, this is actually the user
                    //who requested the song
                    string author = currentSong.RequestAuthor.Username.ToString();

                    //Embed that will be shown in the channel, containing details about the song that will be played
                    var embedImg = new EmbedBuilder()
                        .WithColor(new Color(240, 230, 231))
                        .WithTitle("🎶  " + songOrder + "#  " + songTitle)
                        .WithUrl("https://www.youtube.com/watch?v=" + songID)
                        .WithImageUrl(currentSong.Ytresult.Snippet.Thumbnails.Default__.Url)
                        .WithFooter(new EmbedFooterBuilder().WithText(author + " | " + duration));

                    //Embed that is shown when the music playback ends
                    var embedEnd = new EmbedBuilder()
                        .WithColor(new Color(240, 230, 231))
                        .WithTitle(songOrder + "# " + songTitle)
                        .WithDescription("Fim.")
                        .WithUrl("https://www.youtube.com/watch?v=" + songID)
                        .WithFooter(new EmbedFooterBuilder().WithText(author+ " | " + duration));


                        if (ConnectedChannels.TryGetValue(guild.Id, out client))
                        {
                            try
                            {
                                using (var output = ytVideoOp.PlayYt("https://www.youtube.com/watch?v=" + currentSong.Ytresult.Id.VideoId).StandardOutput.BaseStream)
                                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                                {
                                    try
                                    {
                                        await channel.SendMessageAsync("", false, embedImg);
                                        await output.CopyToAsync(stream);  
                                    }
                                    finally
                                    {
                                        await stream.FlushAsync();//cts);   
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                ehandler = new ExceptionHandler(e);
                                ehandler.WriteToFile();
                                await LeaveAudio(guild, target);                           
                            }
                        }

                        YTSearchResultSong.Dequeue();
                        var msgEnd = await channel.SendMessageAsync("", false, embedEnd);

                        if (YTSearchResultSong.Count >= 1)
                        {
                            await Task.Delay(4000);
                            await msgEnd.DeleteAsync();
                            await SendAudioAsyncYT(guild, channel,target ,usr );
                        }
                        else
                        {
                            currentSongID = 0;
                            await channel.SendMessageAsync("Fim da fila");                          
                            await msgEnd.DeleteAsync();                         
                        }
                    }

            }
            catch(OperationCanceledException)
            {
               //todo
            }
        }
    }
}

