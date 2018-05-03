using Discord;
using Discord.Commands;
using RonoBot.Modules.Audio;
using System;
using System.Threading.Tasks;


namespace RonoBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service = new AudioService();
        private static Object queueLock = new Object();

        public Music(AudioService service)
        {
            _service = service;
        }

        public async Task SendDescriptionOnlyEmbed(string desc)
        {
            var descEmbed = new EmbedBuilder()
              .WithColor(new Color(240, 230, 231))
              .WithDescription(desc);

            await Context.Channel.SendMessageAsync("", false, descEmbed);
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinCmd(IVoiceChannel channel = null)
        {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User, Context.Channel);
        }
        
        [Command("leave", RunMode = RunMode.Async)]
        public async Task LeaveCmd()
        {           
            await _service.LeaveAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
            _service.StopMusicPlayer();
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task Queue([Remainder] string song)
        {
            _service.QueueSong(song, Context.User, Context.Channel, Context.Guild, Context.Message);

            if (_service.GetMPEnded())
            {
                _service.firstSong = true;
            }

            lock (queueLock)
            {
                if (_service.firstSong)
                {
                    _service.firstSong = false;
                    Console.WriteLine("Joined voice channel, starting player.");
                    _service.StartMusicPlayer(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User, Context.Channel);
                }
           }
             
           
                       
        }

        [Command("queueplaylist", RunMode = RunMode.Async)]
        [Alias("qpl")]
        public async Task QueuePlaylist([Remainder] string song)
        {
                await _service.QueuePlaylist(song, Context.User, Context.Channel, Context.Guild, (Context.User as IVoiceState).VoiceChannel);                                
        }

        [Command("nowplaying")]
        [Alias("np")]
        public async Task NowPlayingAsync()
        {                 
            if (!_service.IsPlaying())
            {
                await Context.Channel.SendMessageAsync("Não há nenhuma música tocando");
                return;
            }
            await Context.Channel.SendMessageAsync("",false, _service.CurrentSongEmbed());
        }

        [Command("loopstatus")]
        [Alias("lps")]
        public async Task LoopStatusAsync()
        {
            if (!_service.IsPlaying())
            {
                await SendDescriptionOnlyEmbed("Não é possível checar o status do loop sem nenhuma música tocando");
                return;
            }

            if (_service.IsMPLooping())
            {
                await SendDescriptionOnlyEmbed("Loop: ✔");
            }
            else
            {
                await SendDescriptionOnlyEmbed("Loop: ❌");
            }

        }

        //Toggles the music player to either constantly loop through the song list or
        //dispose of it after its done
        [Command("loop")]
        [Alias("lp")]
        public async Task LoopAsync()
        {
            //Loop wont be toggled if the MP is not playing anything
            if (!_service.IsPlaying())
            {
                await SendDescriptionOnlyEmbed("Não é possível alterar o status do loop sem nenhuma música tocando");
                return;          
            }

            //By default the loop feature is set to false
            _service.ToggleMPListLoop();

            //If its true, it was toggled from false so we must warn that the loop setting is now on
            if (_service.IsMPLooping())
            {
                await SendDescriptionOnlyEmbed("Loop: ✔");
            }
            else
            {
                await SendDescriptionOnlyEmbed("Loop: ❌");
            }
        }
    
        [Command("listqueue", RunMode = RunMode.Async)]
        [Alias("lq")]
        public async Task ListQueue()
        {
             _service.ListQueue(Context.Channel);
        }

        [Command("listqueue", RunMode = RunMode.Async)]
        [Alias("lq")]
        public async Task ListQueue(int page)
        {
            if (page <= 0)
                return;

            _service.ListQueue(Context.Channel,page);
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("next","n","s")]
        public async Task Skip()
        {
             _service.MpNext(Context.User);
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("next", "n", "s")]
        public async Task Skip(int n)
        {
            int remainingSongs = _service.GetMPListSize() - (_service.GetMPCurSongID() + 1);

            if (n == 0)
            {
                await Context.Channel.SendMessageAsync("0 músicas puladas <:hmm:273160805363482625>");
                return;
            }
            else if (n < 0)
            {
                await Context.Channel.SendMessageAsync(n + " músicas puladas <:holy:273134467521052692>");
                return;
            }

            if (n <= remainingSongs)
                _service.MpNext(n, Context.User);
            else
            {
                switch(remainingSongs)
                {
                    case 0: await Context.Channel.SendMessageAsync("Impossível pular " + n + " música(s) já que não há " +
                            "mais músicas para serem tocadas");
                            break;

                    case 1:
                        await Context.Channel.SendMessageAsync("Impossível pular " + n + " música(s) já que existe apenas mais "
                        + remainingSongs + " música para ser tocada");
                        break;

                    default:
                        await Context.Channel.SendMessageAsync("Impossível pular " + n + " música(s) já que existem "
                        + remainingSongs + " músicas para serem tocadas");
                        break;
                }
               
            }
           
        }

    }
}
