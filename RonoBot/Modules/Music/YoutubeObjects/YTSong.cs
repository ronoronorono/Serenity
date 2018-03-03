using Discord.WebSocket;
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
        private string title;
        private string defaultThumbnailUrl;
        private string videoID;
        private string audioURI;
        private string url;
        private string query;
        private int order;
        private SocketUser requestAuthor;
        private string duration;

        public string Title { get => title; set => title = value; }
        public string DefaultThumbnailUrl { get => defaultThumbnailUrl; set => defaultThumbnailUrl = value; }
        public string VideoID { get => videoID; set => videoID = value; }
        public string Query { get => query; set => query = value; }
        public int Order { get => order; set => order = value; }
        public SocketUser RequestAuthor { get => requestAuthor; set => requestAuthor = value; }
        public string Duration { get => duration; set => duration = value; }
        public string Url { get => url; set => url = value; }
        public string AudioURI { get => audioURI; set => audioURI = value; }

        public YTSong(string title, string defaultThumbnailUrl, string videoID, string audioURI, string query, int order, SocketUser requestAuthor, string duration)
        {
            this.title = title;
            this.defaultThumbnailUrl = defaultThumbnailUrl;
            this.videoID = videoID;
            this.url = "https://www.youtube.com/watch?v=" + videoID;
            this.audioURI = audioURI;
            this.query = query;
            this.order = order;
            this.requestAuthor = requestAuthor;
            this.duration = duration;
        }

        public YTSong(Video video, string audioURI, string query, int order, SocketUser requestAuthor)
        {
            this.title = video.Snippet.Title;
            this.defaultThumbnailUrl = video.Snippet.Thumbnails.Default__.Url;
            this.videoID = video.Id;
            this.url = "https://www.youtube.com/watch?v=" + video.Id;
            this.audioURI = audioURI;
            this.query = query;
            this.order = order;
            this.requestAuthor = requestAuthor;
            this.duration = YTVideoOperation.FormatVideoDuration(video.ContentDetails.Duration);
        }

        public YTSong(SearchResult ytresult, string audioURI, string query, int order, SocketUser requestAuthor, string duration)
        {
            this.title = ytresult.Snippet.Title;
            this.defaultThumbnailUrl = ytresult.Snippet.Thumbnails.Default__.Url;
            this.videoID = ytresult.Id.VideoId;
            this.url = "https://www.youtube.com/watch?v=" + videoID;
            this.audioURI = audioURI;
            this.query = query;
            this.order = order;
            this.requestAuthor = requestAuthor;
            this.duration = duration;
        }

        public YTSong(YTSong song)
        {
            this.title = song.Title;
            this.defaultThumbnailUrl = song.DefaultThumbnailUrl;
            this.videoID = song.VideoID;
            this.url = song.Url;
            this.audioURI = song.AudioURI;
            this.query = song.Query;
            this.order = song.Order;
            this.requestAuthor = song.RequestAuthor;
            this.duration = song.Duration;
        }
        
    }
}
