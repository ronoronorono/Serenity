using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace RonoBot.Modules.Audio
{
    class YTVideoOperation
    {
        private ExceptionHandler ehandler;

        //Starts a youtube search with the given query, will return null if no results are found
        //Always returns the first result.
        public SearchResult YoutubeSearch(string query)
        {
            SerenityCredentials api = new SerenityCredentials();
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = api.GoogleAPIKey });

            //Creates the search request
            var searchListRequest = yt.Search.List("snippet");
            SearchResult result = null;

            if (IsValidYTUrl(query))
                query = GetYTVideoID(query);

            //With the given query
            searchListRequest.Type = "video";
            searchListRequest.Q = query;
            searchListRequest.MaxResults = 10;

            try
            {
                //Starts the request and stores the first result that is neither a playlist
                //nor a live broadcast
                SearchListResponse searchRequest = searchListRequest.Execute();
                if (searchRequest.Items.Count < 1)
                {
                    return null;
                }
                else
                {
                    var items = searchRequest.Items;
                    foreach (var item in items)
                    {
                        if (item.Snippet.LiveBroadcastContent == "none")
                        {
                            return item;                        
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ehandler = new ExceptionHandler(e);
                ehandler.WriteToFile();
                Console.WriteLine(e.Message);
            }
            return result;
        }

        public string[] YTDLYoutubeSearch (string query)
        {
            return GetVideoData(query);
        }

        private string[] GetVideoData(string query)
        {
            //string url = "https://www.youtube.com/watch?v=" + ytresult.Id.VideoId;

            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"--skip-download --get-title --get-id --get-duration \"ytsearch:"+query+"\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            })
            {
                
                process.Start();
                string[] data = process.StandardOutput.ReadToEnd().Split('\n');
                
                return data;
            }
        }

        public Process PlayYt(string url)
        {
            Process currentsong = new Process();

            currentsong.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C youtube-dl.exe -4 --geo-bypass --no-check-certificate -f bestaudio -o - {url}| ffmpeg -i pipe:0 -vn -ac 2 -f s16le -ar 48000 pipe:1",
                //Arguments = $"/C youtube-dl.exe -4 -f bestaudio -o - ytsearch1:" +'"'+url+'"'+ " | ffmpeg -i pipe:0 -vn -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            };

            currentsong.Start();
            return currentsong;
        }

        //Checks if a url is a valid youtube one, following discord url parameters
        //which require a https:// body
        //Although this method is not the most correct way of finding videos ID via a youtube
        //url, it turns out to work in all cases so far, since all videos have an ID of length 11
        //While the youtube data api doesn't specify the specific size of a video ID, in cases where the length
        //were different, it always were larger by one digit and if you where to ommit said digit, you could still
        //find the video nevertheless
        public bool IsValidYTUrl(string url)
        {
            if (url == "https://youtu.be/")
                return false;

            if (url == "https://www.youtube.com/watch?v=")
                return false;

            if (url.Length >= 28)
                if (url.Substring(0, 17) == "https://youtu.be/")
                    return true;

            if (url.Length >= 43)
                if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=")
                    return true;

            return false;
        }

        //Returns the ID of a youtube video from its url.
        //returns an empty string if the url is invalid
        public string GetYTVideoID(string url)
        {
            string id = "";


            if (IsValidYTUrl(url))
            {
                if (url.Substring(0, 17) == "https://youtu.be/")
                    return url.Substring(17, 11);
                //Not enterily necessary to check this case, however just to make sure...
                else if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=")
                    return url.Substring(32, 11);

            }

            return id;

        }

    }
}
