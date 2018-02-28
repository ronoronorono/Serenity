﻿using Discord.WebSocket;
using Google.Apis.YouTube.v3.Data;
using RonoBot.Modules.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RonoBot.Modules.Audio
{
    public class YTSong
    {
        SearchResult ytresult;
        string query;
        int order;
        //Not to be confounded with the video's author, this actually references the user who
        //requested the song
        SocketUser requestAuthor;
        string duration;

        public YTSong(SearchResult ytresult, string query, int order, SocketUser requestAuthor)
        {
            this.ytresult = ytresult;
            this.query = query;
            this.order = order;
            this.requestAuthor = requestAuthor;

            this.duration = YTVideoOperation.GetVideoDuration(ytresult.Id.VideoId);
        }

        public YTSong(YTSong song)
        {
            this.ytresult = song.Ytresult;
            this.query = song.Query;
            this.order = song.Order;
            this.requestAuthor = song.RequestAuthor;
            this.duration = song.Duration;
        }

        public SearchResult Ytresult { get => ytresult; set => ytresult = value; }
        public string Query { get => query; set => query = value; }
        public int Order { get => order; set => order = value; }
        public SocketUser RequestAuthor { get => requestAuthor; set => requestAuthor = value; }
        public string Duration { get => duration; set => duration = value; }
        public string GetUrl ()
        {
            return "https://www.youtube.com/watch?v=" + Ytresult.Id.VideoId;
        }

    }
}
