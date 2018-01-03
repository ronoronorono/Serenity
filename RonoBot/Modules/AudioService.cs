using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;


namespace RonoBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            IAudioClient client;

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
                Console.WriteLine(e.Message.ToString());
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            IAudioClient client;

          if (ConnectedChannels.TryRemove(guild.Id, out client))
          {
                //Whenever a music playback starts, a ffmpeg process is created, thus, when the bot leaves
                //he must close an instance of this process if it exists.
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


                await client.StopAsync();
          }
            
        }


        public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string path)
        {
            //Currently this is just a test, the real intention is to turn this method
            //into searching youtube for a given video and playing it
            path = "C:/Users/NetWork/Desktop/DiscordBot/juno.mp3";
            if (!File.Exists(path))
            {
                await channel.SendMessageAsync("File does not exist.");
                return;
            }
            IAudioClient client;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                using (var output = CreateStream(path).StandardOutput.BaseStream)
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try { await output.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); }      
                }
            }
        }

        //Creates the ffmpeg process...
        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            
        }
    }
}

