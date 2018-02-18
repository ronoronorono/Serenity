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

        private MusicPlayer mp = new MusicPlayer();

        private static int songOrder = 0;
        private ExceptionHandler ehandler;

        public int SongOrder()
        {
            return songOrder = mp.ListSize() + 1;
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
                MpStop();
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

        public void StopMusicPlayer()
        {
            mp.StopSong();
            mp.Clear();
        }

        public int GetMPCurSongID()
        {
            return mp.CurrentSongID;
        }

        public int GetMPListSize()
        {
            return mp.ListSize();
        }

        public async void ListQueue(IMessageChannel channel)
        {
            mp.ListSongs(channel);
        }

        public async void QueueSong(string query, SocketUser usr, IMessageChannel channel,IGuild guild)
        {

            if ((usr as IVoiceState).VoiceChannel == null)
            {
                await channel.SendMessageAsync(usr.Mention + " você não está conectado em nenhum canal de voz");
                return;
            }


            SearchResult Song = YTVideoOperation.YoutubeSearch(query);
            int i = SongOrder();

            var embedQueue = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))
                .WithTitle("#" + i + "  " + Song.Snippet.Title)
                .WithDescription("Busca: " + query)
                .WithUrl("https://www.youtube.com/watch?v=" + Song.Id.VideoId)
                .WithImageUrl(Song.Snippet.Thumbnails.Default__.Url)
                .WithFooter(new EmbedFooterBuilder().WithText(usr.Username + " | " + YTVideoOperation.GetVideoDuration(Song.Id.VideoId)));

            channel.SendMessageAsync("", false, embedQueue);


            YTSong newSong = new YTSong(Song, query, i, usr);
            mp.Enqueue(newSong);
            
           
        }

        public async Task StartMusicPlayer(IGuild guild, IVoiceChannel target, SocketUser usr, IMessageChannel channel)
        {
            IAudioClient client;
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
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                    mp.AudioClient = client;
                    mp.MessageChannel = channel;
                    mp.Begin();
            }
        }

        public void MpNext()
        {
            mp.Next();
        }

        public void MpStop()
        {
            mp.StopSong();
        }

        #region Old audio sending method
        
        /*
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
        */
        #endregion
    }
    
}

