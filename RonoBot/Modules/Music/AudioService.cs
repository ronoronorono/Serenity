using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Google.Apis.YouTube.v3.Data;
using Discord.WebSocket;
using RonoBot.Modules.Audio;
using RonoBot;
using System.Collections;
using System.Threading;

namespace RonoBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        private MusicPlayer mp = new MusicPlayer();

        public bool firstSong = true;

        private bool cancelPlaylist = false;

        public Boolean IsOnVoice(IGuild guild)
        {
            return (ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client));
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target, SocketUser msgAuthor, IMessageChannel channel)
        {            
            //If target's null, the user is not connected to a voice channel
            if (target == null)
            {
                await channel.SendMessageAsync(msgAuthor.Mention + ", você não está conectado(a) em nenhum canal de voz.");
                return;
            }
           
            //If the bot is already in a channel
            if (IsOnVoice(guild))
            {
                return;
            }

            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            //If the execution reaches this part of the code, the bot will join the user's channel-
            try
            {
                    var audioClient = await target.ConnectAsync();
                    //The channel is added to the concurrentdictionary for futher operations
                    ConnectedChannels.TryAdd(guild.Id, audioClient);
            }
            catch (Exception e)
            {
                ExceptionHandler.WriteToFile(e);      
                //StopMusicPlayer();
               // await LeaveAudio(guild, target);
            }
        }

        public async Task LeaveAudio(IGuild guild, IVoiceChannel target)
        {

            // Connected = false;

            //If the bot is connected to a voice channel and its inside the dictionary with the guild's id,
            //we can get the AudioClient from the dictionary and stop it
            if (ConnectedChannels.TryRemove(guild.Id, out IAudioClient client))
            {
                await client.StopAsync();
                return;
            }

            //Sometimes the bot will remain in a voice channel
            //even after the application has closed and it disconnected from discord, so we need to remove
            //the connection directly.

            var serenity = guild.GetCurrentUserAsync().Result;
            if (serenity.VoiceChannel != null)
            {
                client = await serenity.VoiceChannel.ConnectAsync();
                await client.StopAsync();
                return;
            }        

        }
       
        public void StopMusicPlayer()
        {
            /* var cs = QueuePlaylistCancel;
             QueuePlaylistCancel = new CancellationTokenSource();
             cs.Cancel();*/
            cancelPlaylist = true;
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

        public bool IsPlaying()
        {
            return mp.Playing;
        }

        private int SongOrder()
        {
            return mp.ListSize() + 1;
        }

        public void MpNext(SocketUser usr)
        {
            mp.Next(usr);
        }

        public void MpNext(int n,SocketUser usr)
        {
            mp.Next(n,usr);
        }

        public bool IsMPLooping()
        {
            return mp.LoopList;
        }

        public void ToggleMPListLoop()
        {
            mp.LoopList = !mp.LoopList;
        }

        public void ListQueue(IMessageChannel channel)
        {
            var tempChannel = mp.MessageChannel;
            mp.MessageChannel = channel;
            mp.ListSongs(channel);
            mp.MessageChannel = tempChannel;
        }

        public void ListQueue(IMessageChannel channel, int page)
        {
            var tempChannel = mp.MessageChannel;
            mp.MessageChannel = channel;
            mp.ListSongs(channel,page);
            mp.MessageChannel = tempChannel;
        }

        public EmbedBuilder CurrentSongEmbed()
        {
            return mp.CurrentSongEmbed();
        }

        public YTSong MPCurSong()
        {
            return mp.CurrentSong();
        }
    
        public double NowPlayingBytes()
        {
            return mp.BytesSent();
        }

        public bool GetMPEnded()
        {
            return mp.Ended;
        }

        public async Task QueueSong(string query, SocketUser usr, IMessageChannel channel,IGuild guild, SocketUserMessage usrmsg)
        {
            if ((usr as IVoiceState).VoiceChannel == null)
            {
                await channel.SendMessageAsync(usr.Mention + " você não está conectado em nenhum canal de voz");
                return;
            }
            Video video = YTVideoOperation.YoutubeSearch(query);

            if (video == null)
            {
                var embedL = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))
                .WithDescription("Nenhum resultado encontrado para: "+query);
                await channel.SendMessageAsync("", false, embedL);
                return;
            }

            //Attempts to get the audio URI either by YoutubeExplode or youtube-dl
            //First attempt is with YoutubeExplode
            string uri = YTVideoOperation.GetVideoURIExplode(video.Id).Result;

            
            if (uri == null)
                uri = YTVideoOperation.GetVideoAudioURI("https://www.youtube.com/watch?v="+video.Id);

            YTSong song = new YTSong(video,uri, query, SongOrder(), usr);

            mp.Enqueue(song);
            var embedQueue = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))
                .WithTitle("#" + song.Order + "  " + song.Title)
                .WithDescription("Busca: " + query)
                .WithUrl(song.Url)
                .WithImageUrl(song.DefaultThumbnailUrl)
                .WithFooter(new EmbedFooterBuilder().WithText(song.RequestAuthor.Username + " | " + song.Duration));

            var msg = await channel.SendMessageAsync("", false, embedQueue);

            await Task.Delay(6000);

            await usrmsg.DeleteAsync().ConfigureAwait(false);
            await msg.DeleteAsync().ConfigureAwait(false);
        }

        public async Task QueuePlaylist(string playlistUrl, SocketUser usr, IMessageChannel channel, IGuild guild, IVoiceChannel target)
        {
            if ((usr as IVoiceState).VoiceChannel == null)
            {
                await channel.SendMessageAsync(usr.Mention + " você não está conectado em nenhum canal de voz");
                return;
            }

            PlaylistItem[] songs = YTVideoOperation.PlaylistSearch(YTVideoOperation.TryParsePlaylistID(playlistUrl).ID);
            string[] audioURIs = new string[songs.Length];


            var embedA = new EmbedBuilder()
               .WithColor(new Color(240, 230, 231))
               .WithDescription("Carregando playlist...");
            await channel.SendMessageAsync("", false, embedA);

            int unavailableVids = 0;
            int x = 0;

            for (int i = 0; i < songs.Length; i++)
            {
                try
                {
                    //Video video = YTVideoOperation.SearchVideoByID(songs[i].Snippet.ResourceId.VideoId);
                    if (cancelPlaylist)
                        break;

                    var song = songs[i];

                    string uri = YTVideoOperation.GetVideoURIExplode(songs[i].Snippet.ResourceId.VideoId).Result;

                    if (uri == null)
                        uri = YTVideoOperation.GetVideoAudioURI("https://www.youtube.com/watch?v=" + songs[i].Snippet.ResourceId.VideoId);

                    if (uri == null || uri == "")
                    {
                        unavailableVids++;
                    }
                    else
                    {
                        YTSong playlistSong = new YTSong(song.Snippet.Title,
                                                         song.Snippet.Thumbnails.Default__.Url,
                                                         song.Snippet.ResourceId.VideoId, uri, "playlist", SongOrder(), usr, YTVideoOperation.GetVideoDuration(song.Snippet.ResourceId.VideoId));
                        mp.Enqueue(playlistSong);
                        x++;
                    }

                    if (cancelPlaylist)
                        break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("QUEUE PLAYLIST CANCELED");
                    //cancelPlaylist = true;
                    //Playing = false;
                }               

                if (!mp.Playing)
                    StartMusicPlayer(guild,target,usr,channel);
            }

            if (cancelPlaylist)
            {
                mp.Clear();
                cancelPlaylist = false;
                return;
            }

            if (unavailableVids == 0)
            {
                var embedAll = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231))
                  .WithDescription("Playlist carregada \n\n`Todos os videos carregados ("+x+")`" );
                await channel.SendMessageAsync("", false, embedAll);
            }
            else
            {
                var embedB = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231))
                  .WithDescription("Playlist carregada \n\n`✔ Videos carregados: " + x + " ❌ Videos indisponiveis: " + unavailableVids);
                await channel.SendMessageAsync("", false, embedB);
            }
           
        }

        public async Task StartMusicPlayer(IGuild guild, IVoiceChannel target, SocketUser usr, IMessageChannel channel)
        {
            if (mp.Ended)
            {
                await LeaveAudio(guild, target);
                mp.Ended = false;
            }

            //If the bot isn't connected to any channel, it will attempt to join one before starting
            //the music playback
            if (!ConnectedChannels.TryGetValue(guild.Id, out IAudioClient client))
            {
               // await LeaveAudio(guild, target);
                await JoinAudio(guild, target, usr, channel);
            }

            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                //Console.WriteLine("Starting mp, setting client");
                //Sets the necessary properties to the MusicPlayer class
                mp.AudioClient = client;
                //Console.WriteLine("Client set, channel");
                mp.MessageChannel = channel;

                //Console.WriteLine("begin");
                //Starts the player loop thread
                mp.Begin();
            }
            
        }    
     
    }
    
}

