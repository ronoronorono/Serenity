using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Discord;
using Discord.Audio;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using Discord.Rest;

namespace RonoBot.Modules.Audio
{
    public class MusicPlayer
    {       
        private List<YTSong> SongList = new List<YTSong>();
        public int CurrentSongID { get; set; } = 0;
        bool player = false;
        public IMessageChannel MessageChannel {get;set;}
        private object locker = new object();
        private int bytesSent = 0;
        public IAudioClient AudioClient { get; set; }
        private CancellationTokenSource SongCancelSource { get; set; }


        public void Begin()
        {
            if (SongList.Count != 0)
            {
                this.SongCancelSource = new CancellationTokenSource();
                //CurrentSongID++;
                Thread musicPlayer = new Thread(new ThreadStart(Play));
                player = true;
                musicPlayer.Start();
            }
            else
                Console.WriteLine("No song in queue");
        }

        public YTSong CurrentSong()
        {
            if (SongList.Count > 0)
                return SongList[CurrentSongID];
        
            return null;
        }

        public int ListSize()
        {
            return SongList.Count;
        }

        public async void ShowCurrentSongEmbed()
        {
            if (CurrentSong() == null)
                return;

            YTSong curSong = CurrentSong();

            var embedCur = new EmbedBuilder()
                       .WithColor(new Color(240, 230, 231))
                       .WithTitle("🎶  " + curSong.Order + "#  " + curSong.Ytresult.Snippet.Title)
                       .WithUrl(curSong.GetUrl())
                       .WithImageUrl(curSong.Ytresult.Snippet.Thumbnails.Default__.Url)
                       .WithFooter(new EmbedFooterBuilder().WithText(curSong.RequestAuthor + " | " + curSong.Duration));

            await MessageChannel.SendMessageAsync("", false, embedCur);
        }

        public async void ShowSongEndedEmbed()
        {
            if (CurrentSong() == null)
                return;

            YTSong curSong = CurrentSong();

            var embedEnd = new EmbedBuilder()
                       .WithColor(new Color(240, 230, 231))
                       .WithTitle(curSong.Order + "# " + curSong.Ytresult.Snippet.Title)
                       .WithDescription("Fim.")
                       .WithUrl(curSong.GetUrl())
                       .WithFooter(new EmbedFooterBuilder().WithText(curSong.RequestAuthor.Username + " | " + curSong.Duration)); ;

            await MessageChannel.SendMessageAsync("", false, embedEnd);
        }

        public async void ListSongs(IMessageChannel channel)
        {
            if (SongList.Count < 1)
            {
                var embedL = new EmbedBuilder()
                 .WithColor(new Color(240, 230, 231))
                 .WithDescription("Nenhuma musica na lista");
                await channel.SendMessageAsync("", false, embedL);
                return;
            }

            var i = 0;

            var embedList = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231));

            while (i < SongList.Count)
            {
                EmbedFieldBuilder embedField = new EmbedFieldBuilder();

                YTSong cur = SongList[i];

                if (SongList[i] == SongList[CurrentSongID])
                {
                    embedField.WithName("#" + (i+1) + " - Atual ")
                               .WithIsInline(false)
                               .WithValue("[" + cur.Ytresult.Snippet.Title + "](" + cur.GetUrl() + ")\n" + cur.RequestAuthor.Username + " | " + cur.Duration);
                }
                else
                {
                    embedField.WithName("#" + (i+1))
                               .WithIsInline(false)
                               .WithValue("[" + cur.Ytresult.Snippet.Title + "](" + cur.GetUrl() + ")\n" + cur.RequestAuthor.Username + " | " + cur.Duration);
                }
                embedList.AddField(embedField);
                i++;
            }
            await MessageChannel.SendMessageAsync("", false, embedList);
        }

        public bool NextSong()
        {          
            return (CurrentSongID < SongList.Count - 1);
        }

        public async void Play()
        {
            
            while (player)
            {
                bytesSent = 0;
                AudioOutStream pcm = null;
                Stream songStream = null;

                CancellationToken cancelToken;
                lock (locker)
                {
                    cancelToken = SongCancelSource.Token;
                }
                try
                {
                    // The size of bytes to read per frame; 1920 for mono
                    int blockSize = 3840;
                    
                    byte[] buffer = new byte[blockSize];
                    
                    pcm = AudioClient.CreatePCMStream(AudioApplication.Music, bufferMillis: 500);
                    int bytesRead = 0;

                    songStream = FFmpegProcess(YTVideoOperation.GetVideoURI(CurrentSong().GetUrl())).StandardOutput.BaseStream;
                    ShowCurrentSongEmbed();

                    while ((bytesRead = songStream.Read(buffer, 0, 3840)) > 0)
                    {                      
                        await pcm.WriteAsync(buffer, 0, bytesRead,cancelToken).ConfigureAwait(false);
                        unchecked { bytesSent += bytesRead; }
                    }
                   
                   
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("SONG CANCELED");
                    player = false;
                }
                finally
                {
                    ShowSongEndedEmbed();
                    if (pcm != null)
                    {
                        var flushCancel = new CancellationTokenSource();
                        var flushToken = flushCancel.Token;
                        var flushDelay = Task.Delay(1000, flushToken);
                        await Task.WhenAny(flushDelay, pcm.FlushAsync(flushToken));
                        flushCancel.Cancel();
                        pcm.Dispose();
                        songStream.Dispose();
                        
                        if (NextSong())
                        {
                            CurrentSongID++;
                        }
                        else
                        {
                            Clear();
                            player = false;
                        }
                    }
                }
                

            }
        }

        public void Clear()
        {
            CurrentSongID = 0;
            SongList.Clear();
            AudioClient = null;
            MessageChannel = null;
        }

        public void Enqueue(YTSong song)
        {
            SongList.Add(song);
        }

     
        public void StopSong()
        {
            var cs = SongCancelSource;
            SongCancelSource = new CancellationTokenSource();
            cs.Cancel();
        }

        public void Next()
        {
            StopSong();

            //After a song is cancelled, the player boolean which keeps the while loop running,
            //is set to false. In order to skip the current song we have to check if there's another one
            //available and conveniently, the operation that does this also returns a boolean indicating
            //whether there is one or not, thus we assign this boolean to the player.
            player = NextSong();
        }

        public Process FFmpegProcess(string URI)
        {
            Process currentsong = new Process();          
            currentsong.StartInfo = new ProcessStartInfo
            {               
                FileName = "ffmpeg",
                Arguments = $"-err_detect ignore_err -i \""+URI+"\" -vn -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            };

            currentsong.Start();
            return currentsong;
        }
    }
}
