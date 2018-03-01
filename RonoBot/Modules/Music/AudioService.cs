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

namespace RonoBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        private MusicPlayer mp = new MusicPlayer();

        private static int songOrder = 0; 
   
        public async Task JoinAudio(IGuild guild, IVoiceChannel target, SocketUser msgAuthor, IMessageChannel channel)
        {
            IAudioClient client;

            
            //If target's null, the user is not connected to a voice channel
            if (target == null)
            {
                await channel.SendMessageAsync(msgAuthor.Mention + ", você não está conectado(a) em nenhum canal de voz.");
                return;
            }

            //Sometimes when this application closes and the bot is still inside a voice channel,
            //it will remain there even though its not even online anymore.
            //In this case, we just have to reconnect to the voice channel the bot is currently on and
            //add it to the dictionary.        
            var serenity = guild.GetCurrentUserAsync().Result;
           
            if (serenity.VoiceChannel != null)
            {
                client = await serenity.VoiceChannel.ConnectAsync();
                ConnectedChannels.TryAdd(guild.Id, client);
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

            //If the execution reaches this part of the code, the bot will join the user's channel
            try
            {
                var audioClient = await target.ConnectAsync();
              
                //The channel is added to the concurrentdictionary for futher operations
                ConnectedChannels.TryAdd(guild.Id, audioClient);
            }
            catch (Exception e)
            {
                ExceptionHandler.WriteToFile(e);      
                StopMusicPlayer();
                await LeaveAudio(guild, target);
            }
        }

        public async Task LeaveAudio(IGuild guild, IVoiceChannel target)
        {
            IAudioClient client;

            var users = await guild.GetUsersAsync();

            //If the bot is connected to a voice channel and its inside the dictionary with the guild's id,
            //we can get the AudioClient from the dictionary and stop it
            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await client.StopAsync();
                return;
            }

            //As said above in the JoinAudio method, sometimes the bot will remain in a voice channel
            //even after the application has closed and it disconnected from discord
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

        private int SongOrder()
        {
            return songOrder = mp.ListSize() + 1;
        }

        public void MpNext()
        {
            mp.Next();
        }

        public void MpNext(int n)
        {
            mp.Next(n);
        }

        public bool IsMPLooping()
        {
            return mp.LoopList;
        }

        public bool IsPlaying()
        {
            return mp.Playing;
        }

        public void ToggleMPListLoop()
        {
            mp.LoopList = !mp.LoopList;
        }

        public async void ListQueue(IMessageChannel channel)
        {
            mp.ListSongs(channel);           
        }

        public async void ListQueue(IMessageChannel channel, int page)
        {
            mp.ListSongs(channel,page);
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

        public async void QueueSong(string query, SocketUser usr, IMessageChannel channel,IGuild guild)
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

            YTSong song = new YTSong(video, query, SongOrder(), usr);

            mp.Enqueue(song);
            var embedQueue = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))
                .WithTitle("#" + song.Order + "  " + song.Title)
                .WithDescription("Busca: " + query)
                .WithUrl(song.Url)
                .WithImageUrl(song.DefaultThumbnailUrl)
                .WithFooter(new EmbedFooterBuilder().WithText(song.RequestAuthor.Username + " | " + song.Duration));

            channel.SendMessageAsync("", false, embedQueue);

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
                //Video video = YTVideoOperation.SearchVideoByID(songs[i].Snippet.ResourceId.VideoId);

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

                if (!mp.Playing)
                    StartMusicPlayer(guild,target,usr,channel);
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
            IAudioClient client;
            //If the bot isn't connected to any channel, it will attempt to join one before starting
            //the music playback
            if (!ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                await LeaveAudio(guild, target);
                await JoinAudio(guild, target, usr, channel);
            }
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                //Sets the necessary properties to the MusicPlayer class
                mp.AudioClient = client;
                mp.MessageChannel = channel;

                //Starts the player loop thread
                mp.Begin();
            }
        }    
     
    }
    
}

