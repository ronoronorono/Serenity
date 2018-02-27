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
            _service.StopMusicPlayer();
            await _service.LeaveAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        [Command("queue")]
        [Alias("q")]
        public async Task Queue([Remainder] string song)
        {
             _service.QueueSong(song, Context.User,Context.Channel,Context.Guild);
            if (_service.GetMPListSize() == 1)
            {
                _service.StartMusicPlayer(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User, Context.Channel);
            }
            
        }

        [Command("nowplaying")]
        [Alias("np")]
        public async Task NowPlayingAsync()
        {        
            YTSong Song = _service.MPCurSong();

            if (Song == null)
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

            //If its true, it was toggled from false to true so we must warn that the loop setting is now on
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

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("next","n","s")]
        public async Task Skip()
        {
             _service.MpNext();
        }

    }
}
