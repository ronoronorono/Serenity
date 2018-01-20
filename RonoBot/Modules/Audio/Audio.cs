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

        public Music(AudioService service)
        {
            _service = service;
        }

        [Command("join", RunMode = RunMode.Async),RequireOwner]
        public async Task JoinCmd(IVoiceChannel channel = null)
        {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User, Context.Channel);
        }

        
        [Command("leave", RunMode = RunMode.Async), RequireOwner]
        public async Task LeaveCmd()
        {
            await _service.LeaveAudio(Context.Guild);

           // IVoiceChannel channel = (Context.User as IVoiceState).VoiceChannel;
            
        }

        [Command("play", RunMode = RunMode.Async), RequireOwner]
        public async Task PlayCmd([Remainder] string song)
        {
            await _service.SendAudioAsyncYT(Context.Guild, Context.User, Context.Channel, song);

        }

    }
}
