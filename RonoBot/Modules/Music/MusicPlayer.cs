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
        private Thread musicPlayer;
        private List<YTSong> SongList = new List<YTSong>();
        public int CurrentSongID { get; set; } = 0;
        public bool Playing { get; private set; } = false;
        public bool LoopList { get; set; } = false;
        public IMessageChannel MessageChannel {get;set;}
        private object locker = new object();
        private int bytesSent = 0;
        private bool error = false;
        const int _frameBytes = 3840;
        const float _miliseconds = 20.0f;
        public TimeSpan CurTime => TimeSpan.FromSeconds(bytesSent / (float)_frameBytes / (1000 / _miliseconds));

        public IAudioClient AudioClient { get; set; }
        private CancellationTokenSource SongCancelSource { get; set; }


        public void Begin()
        {
            if (SongList.Count != 0)
            {
                this.SongCancelSource = new CancellationTokenSource();
                musicPlayer = new Thread(new ThreadStart(Play));
                Playing = true;
                musicPlayer.Start();
            }
            else
                Console.WriteLine("No song in queue");
        }

        public async void Play()
        {

            while (Playing)
            {
                bytesSent = 0;
                AudioOutStream pcm = null;
                Stream songStream = null;
                Process ffmpeg = null;

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
                    ffmpeg = FFmpegProcess(YTVideoOperation.GetVideoAudioURI(CurrentSong().GetUrl()));

                    ffmpeg.Start();
                    songStream = ffmpeg.StandardOutput.BaseStream;
                    

                    if (songStream.Read(buffer, 0, 3840) == 0)
                    {
                        ShowSongNullEmbed();
                        error = true;
                        throw new OperationCanceledException();
                    }

                    ShowSongStartEmbed();
                  
                    while ((bytesRead = songStream.Read(buffer, 0, 3840)) > 0)
                    {
                        
                        await pcm.WriteAsync(buffer, 0, bytesRead, cancelToken).ConfigureAwait(false);
                        unchecked { bytesSent += bytesRead; }

                    }

                    
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("SONG CANCELED");
                    //Playing = false;
                }
                finally
                {
                    if (!error)
                        ShowSongEndedEmbed();
                    else
                        error = false;

                    if (pcm != null)
                    {
                        var flushCancel = new CancellationTokenSource();
                        var flushToken = flushCancel.Token;
                        var flushDelay = Task.Delay(1000, flushToken);

                        await Task.WhenAny(flushDelay, pcm.FlushAsync(flushToken));
                        flushCancel.Cancel();

                        if (ffmpeg != null)
                            ffmpeg.Dispose();

                        
                        pcm.Dispose();
                        songStream.Dispose();

                        if (NextSong())
                        {
                            CurrentSongID++;
                        }
                        else
                        {
                            if (LoopList)
                            {
                                CurrentSongID = 0;
                            }
                            else
                            {
                                Clear();
                            }
                        }
                    }
                }


            }
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

        public string FormattedCurTime()
        {
            string time = "";

            string hours = "";
            string minutes = "";
            string seconds = "";

            TimeSpan curTime = CurTime;

            if (curTime.Hours > 9)
                hours = curTime.Hours + ":";
            else if (curTime.Hours > 0)
                hours = "0" + curTime.Hours + ":";

            time += hours;

            if (curTime.Minutes > 9)
                minutes = curTime.Minutes + ":";
            else if (curTime.Minutes > 0)
                minutes = "0" + curTime.Minutes + ":";
            else
                minutes = "00:";

            time += minutes;

            if (curTime.Seconds > 9)
                seconds = ""+curTime.Seconds;
            else if (curTime.Seconds > 0)
                seconds = "0" + curTime.Seconds;
            else
                seconds = "00";

            time += seconds;

            return time;
        }

        public EmbedBuilder CurrentSongEmbed()
        {
            if (CurrentSong() == null)
                return null;

            YTSong curSong = CurrentSong();

            // EmbedFieldBuilder embedField = new EmbedFieldBuilder();

            //  embedField.WithValue().

            EmbedFieldBuilder songTitle = new EmbedFieldBuilder();

            songTitle.WithName("Musica atual")
                            .WithIsInline(false)
                            .WithValue("[" + curSong.Ytresult.Snippet.Title + "](" + curSong.GetUrl() + ")" +
                            "\n\n" +"`"+ FormattedCurTime() + " / " + curSong.Duration + "`" +
                            "\n\n`Solicitada por:` " + curSong.RequestAuthor.Username);



            var embed = new EmbedBuilder()
                .WithColor(new Color(240, 230, 231))               
                .WithThumbnailUrl(curSong.Ytresult.Snippet.Thumbnails.Default__.Url);

            embed.AddField(songTitle);
                //.AddField(songTitle);
                //.WithFooter(new EmbedFooterBuilder().WithText(curSong.RequestAuthor.Username));

            return embed;
        }

        public async void ShowSongStartEmbed()
        {
            if (CurrentSong() == null)
                return;

            YTSong curSong = CurrentSong();

            EmbedFieldBuilder songTitle = new EmbedFieldBuilder();

            songTitle.WithName("** "+curSong.Order+" # **")
                            .WithIsInline(false)
                            .WithValue("[" + curSong.Ytresult.Snippet.Title + "](" + curSong.GetUrl() + ")" +                            
                            "\n\n`" + curSong.RequestAuthor.Username + " | " + curSong.Duration + "`");

            var embedCur = new EmbedBuilder()
                       .WithColor(new Color(240, 230, 231))
                       .WithAuthor(author => { author.WithName(" ♪ ♪ ").WithIconUrl("https://cdn.discordapp.com/avatars/390402848443203595/d2831182eb4d3177febd28f44b4ec936.png?size=256"); })
                       .WithThumbnailUrl(curSong.Ytresult.Snippet.Thumbnails.Default__.Url);

            embedCur.AddField(songTitle);
            await MessageChannel.SendMessageAsync("", false, embedCur);
        }

        public async void ShowSongNullEmbed()
        {

            if (CurrentSong() == null)
                return;

            YTSong curSong = CurrentSong();

            EmbedFieldBuilder songTitle = new EmbedFieldBuilder();

            songTitle.WithName("** " + curSong.Order + " # **")
                            .WithIsInline(false)
                            .WithValue("[" + curSong.Ytresult.Snippet.Title + "](" + curSong.GetUrl() + ")" +
                            "\n\n`" + curSong.RequestAuthor.Username + " | " + curSong.Duration + "`");

            var embedCur = new EmbedBuilder()
                       .WithColor(new Color(240, 230, 231))
                       .WithAuthor(author => { author.WithName(" ❌ Erro na reprodução ").WithIconUrl("https://cdn.discordapp.com/avatars/390402848443203595/d2831182eb4d3177febd28f44b4ec936.png?size=256"); })
                       .WithThumbnailUrl(curSong.Ytresult.Snippet.Thumbnails.Default__.Url);

            embedCur.AddField(songTitle);
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

        public async void ListSongs(IMessageChannel channel, int page)
        {
            //Page enumeration works like an array, so pages actually start from 0,
            int arrayPage = page - 1;

            int pageRepresentation = page;
            int pageCount = (ListSize() + 10 - 1) / 10;

            if (SongList.Count < 1)
            {
                var embedL = new EmbedBuilder()
                 .WithColor(new Color(240, 230, 231))
                 .WithDescription("Nenhuma musica na lista");
                await channel.SendMessageAsync("", false, embedL);
                return;
            }
            else if (ListSize() <= arrayPage * 10)
            {
                var embedL = new EmbedBuilder()
               .WithColor(new Color(240, 230, 231))
               .WithDescription("Não é possível exibir a " + page + "ª pagina pois não há musicas suficientes");
                await channel.SendMessageAsync("", false, embedL);
                return;
            }
            

           
            var embedList = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231))
                  .WithFooter(new EmbedFooterBuilder().WithText("Pagina " + pageRepresentation + "/" + pageCount));

            //10 Songs per page
            var i = arrayPage * 10;
            

            while (i < SongList.Count && i < ((arrayPage*10) + 10))
            {
                EmbedFieldBuilder embedField = new EmbedFieldBuilder();

                YTSong cur = SongList[i];

                if (SongList[i] == SongList[CurrentSongID])
                {
                    embedField.WithName("`#" + (i + 1) + " - Atual `")
                               .WithIsInline(false)
                               .WithValue("[" + cur.Ytresult.Snippet.Title + "](" + cur.GetUrl() + ")\n\n`" + cur.RequestAuthor.Username + " | " + cur.Duration+"`");
                }
                else
                {
                    embedField.WithName("`#" + (i + 1)+"`")
                               .WithIsInline(false)
                               .WithValue("[" + cur.Ytresult.Snippet.Title + "](" + cur.GetUrl() + ")\n\n`" + cur.RequestAuthor.Username + " | " + cur.Duration+"`");
                }
                embedList.AddField(embedField);
                i++;
            }
            await MessageChannel.SendMessageAsync("", false, embedList);
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

            int pageCount = (ListSize() + 10 - 1) / 10;
            

            var embedList = new EmbedBuilder()
                  .WithColor(new Color(240, 230, 231))
                  .WithFooter(new EmbedFooterBuilder().WithText("Pagina 1/" + pageCount ));

            //Will only show the first page, each page contains 10 songs
            while (i < SongList.Count && i < 10)
            {
                EmbedFieldBuilder embedField = new EmbedFieldBuilder();

                YTSong cur = SongList[i];

                if (SongList[i] == SongList[CurrentSongID])
                {
                    embedField.WithName("`#" + (i+1) + " - Atual `")
                               .WithIsInline(false)
                               .WithValue("[" + cur.Ytresult.Snippet.Title + "](" + cur.GetUrl() + ")\n\n`" + cur.RequestAuthor.Username + " | " + cur.Duration+"`");
                }
                else
                {
                    embedField.WithName("`#" + (i+1)+"`")
                               .WithIsInline(false)
                               .WithValue("[" + cur.Ytresult.Snippet.Title + "](" + cur.GetUrl() + ")\n\n`" + cur.RequestAuthor.Username + " | " + cur.Duration+"`");
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
    
        public void Clear()
        {
            Playing = false;
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
      
        public double BytesSent()
        {
            return bytesSent;
        }

        public void Next()
        {
            StopSong();


            //If the music player is set to loop we dont have to worry whether or not we should 
            //check if there is a next song available.
            if (LoopList)
                Playing = true;
            else
                Playing = NextSong();
        }

        public Process FFmpegProcess(string URI)
        {
            Process currentsong = new Process();
            currentsong.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"" + URI + "\" -vn -ac 2 -f s16le -ar 48000 pipe:1 ",
                UseShellExecute = false,
                RedirectStandardOutput = true,      
                CreateNoWindow = false
            };
     
            //currentsong.Start();
            return currentsong;
        }
    }
}
