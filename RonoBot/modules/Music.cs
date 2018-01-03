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
        /*
        private Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            return Process.Start(ffmpeg);
        }

        [Command("play"),RequireOwner]
        public async Task PlayAsync(string url)
        {
            IVoiceChannel channel = (Context.User as IVoiceState).VoiceChannel;
            IAudioClient client = await channel.ConnectAsync();

            var output = CreateStream(url).StandardOutput.BaseStream;
            var stream = client.CreatePCMStream(AudioApplication.Music, 128 * 1024);
            await output.CopyToAsync(stream);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        
        [Command("join")]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            // Get the audio channel
            channel = channel ?? (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Message.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
            var audioClient = await channel.ConnectAsync();
        }
        */

        private readonly AudioService _service;

        public Music(AudioService service)
        {
            _service = service;
        }

        [Command("join", RunMode = RunMode.Async),RequireOwner]
        public async Task JoinCmd(IVoiceChannel channel = null)
        {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        
        [Command("leave", RunMode = RunMode.Async), RequireOwner]
        public async Task LeaveCmd()
        {
            await _service.LeaveAudio(Context.Guild);

           // IVoiceChannel channel = (Context.User as IVoiceState).VoiceChannel;
            
        }

        [Command("play", RunMode = RunMode.Async), RequireOwner]
        public async Task PlayCmd()//[Remainder] string song)
        {
            await _service.SendAudioAsync(Context.Guild, Context.Channel, null);

        }

    }
}
