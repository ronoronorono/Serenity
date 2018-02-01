using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Linq;
using Discord.Audio;
using System.Threading.Tasks;
using System;
using System.IO;


namespace RonoBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service;

        private static int currentID = 0;

        public Music(AudioService service)
        {
            _service = service;
            currentID = _service.GetCurrentSongID();
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinCmd(IVoiceChannel channel = null)
        {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User, Context.Channel);
        }

        
        [Command("leave", RunMode = RunMode.Async)]
        public async Task LeaveCmd()
        {
            _service.ClearQueue();
            await _service.LeaveAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);  
        }

        [Command("queue")]
        [Alias("q")]
        public async Task Queue([Remainder] string song)
        {
            var embed =  _service.QueueAudio(Context.Guild, Context.User, Context.Channel, (Context.User as IVoiceState).VoiceChannel, song);
            if (embed != null)
                await Context.Channel.SendMessageAsync("", false, embed);

            //This means this is the first song, thus it must begin playing it
            if (_service.GetCurrentSongID() == 1)
            {
               Play();   
            } 
        }

        public async Task Play()
        {
            await _service.SendAudioAsyncYT(Context.Guild, Context.Channel, (Context.User as IVoiceState).VoiceChannel, Context.User);
        }

        [Command("lq", RunMode = RunMode.Async)]
        [Alias("listqueue")]
        public async Task ListQueue()
        {
            await _service.ListQueue(Context.Channel, Context.User);
        }

        /* [Command("skip", RunMode = RunMode.Async)]
         public async Task Skip()
         {
             await _service.SkipAudio(Context.Guild, Context.User, Context.Channel);
         }*/

    }
}
