﻿using Discord;
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

        //private static int currentID = 0;

        public Music(AudioService service)
        {
            _service = service;
           // currentID = _service.GetCurrentSongID();
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
        
        public async Task Play()
        {
            //await _service.testplay(Context.Guild, (Context.User as IVoiceState).VoiceChannel, Context.User, Context.Channel);
        }


        [Command("lq", RunMode = RunMode.Async)]
        [Alias("listqueue")]
        public async Task ListQueue()
        {

             _service.ListQueue(Context.Channel);
        }

         [Command("skip", RunMode = RunMode.Async)]
         public async Task Skip()
         {
            _service.MpNext();
         }

    }
}
