using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using YoutubeExplode;
using System.Threading.Tasks;
using System.Linq;

namespace RonoBot.Modules.Audio
{
    
    class YTVideoOperation
    {
        //Starts a youtube search with the given query, will return null if no results are found
        //Returns the first result that matches certain criterias explained further on.
        public static Video YoutubeSearch(string query)
        {
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = SerenityCredentials.GoogleAPIKey() });
       
            //Creates the search request
            var searchListRequest = yt.Search.List("snippet");
            
            //checks if the query is a youtube url
            //if it is, we will directly get the video's id
            //that is inside the url
            var res = TryParseID(query);
            if (res.Valid)
            {                
                return SearchVideoByID(res.ID);
            }

            //sets the search type and the query
            //setting to video type excludes channels and playlists from the results,
            //however live broadcasts wont be excluded, but this is dealt afterwards.
            searchListRequest.Type = "video";
            searchListRequest.Q = query;

            //the video will be attempted to be found inside a maximum of 10 results
            searchListRequest.MaxResults = 10;

            try
            {
                //Starts the request
                SearchListResponse searchRequest = searchListRequest.Execute();
                //This means no results were encountered
                if (searchRequest.Items.Count < 1)
                {
                    return null;
                }
                else
                {
                    var items = searchRequest.Items;
                    foreach (var item in items)
                    {
                        //This is where we deal with the live broadcast results
                        //we'll get the first item that has "none" set to the LiveBroadcastContent property,                        
                        if (item.Snippet.LiveBroadcastContent == "none")
                        {                
                            return SearchVideoByID(item.Id.VideoId);                        
                        }
                    }
                }

                //There's a possibility that all 10 results were live broadcasts, so we have 
                //to return null here also
                return null;

            }
            catch (Exception e)
            {
                ExceptionHandler.WriteToFile(e);
            }
            return null;
        }

        /*public static int PlaylistSize(string playlistID)
        {

        }*/

        

        public static PlaylistItem[] PlaylistSearch (string playlistID)
        {
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = SerenityCredentials.GoogleAPIKey() });

            var playlistItems = yt.PlaylistItems.List("snippet,status,contentDetails");
            var nextPageToken = "";

            playlistItems.PlaylistId = playlistID;
            playlistItems.MaxResults = 50;
            playlistItems.PageToken = nextPageToken;
            

            int i = 0;

            var res = playlistItems.Execute();
            int totalSize = res.PageInfo.TotalResults.Value;

            PlaylistItem[] videos = new PlaylistItem[totalSize];

            IList<PlaylistItem> resItems = res.Items;

            resItems.CopyTo(videos, i);

            i += 50;

            nextPageToken = res.NextPageToken;

            while (nextPageToken != null)
            {
                playlistItems = yt.PlaylistItems.List("snippet,status,contentDetails");
                playlistItems.PlaylistId = playlistID;
                playlistItems.MaxResults = 50;
                playlistItems.PageToken = nextPageToken;

                var resp = playlistItems.Execute();

                resItems = resp.Items;

                resItems.CopyTo(videos, i);

                nextPageToken = resp.NextPageToken;

                i += 50;
            }

           

            return videos;            
        }

        
        public static string FormatVideoDuration(string duration)
        {
            //Since a video's ID is unique to each one, we'll only get one result.
            //The time is given in the PT#MS#S format in XML, so we convert it
            TimeSpan t = XmlConvert.ToTimeSpan(duration);
            string dur = t.ToString();

            //Minor formatting here, if a video has 03:00(3 minutes) of duration, it'll
            //be written as 00:03:00 and we dont need the first 3 characters
            if (dur.StartsWith("00:"))
                dur = dur.Remove(0, 3);

            return dur;
        }

        public static Video SearchVideoByID(string videoID)
        {
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = SerenityCredentials.GoogleAPIKey() });
        
            //Starts a video search request, to return video objects
            var searchVideoRequest = yt.Videos.List("snippet,contentDetails");

            
            //We'll search for the video directly with its ID
            //searchVideoRequest.
            searchVideoRequest.Id = videoID;
            var res = searchVideoRequest.Execute();

            //If the ID is invalid
            if (res.Items.Count == 0)
                return null;

            //Otherwise, since the ID is unique to each video, we'll always get exactly one result
            return res.Items[0];
        }

        public static string GetVideoDuration(string videoID)
        {
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = SerenityCredentials.GoogleAPIKey() });

            //Starts a video search request, to return video objects
            var searchVideoRequest = yt.Videos.List("contentDetails");


            //We'll search for the video directly with its ID
            //searchVideoRequest.
            searchVideoRequest.Id = videoID;
            var res = searchVideoRequest.Execute();

            //If the ID is invalid
            if (res.Items.Count == 0)
                return null;

            //Otherwise, since the ID is unique to each video, we'll always get exactly one result
            return FormatVideoDuration(res.Items[0].ContentDetails.Duration);
        }

        //Gets only the audio of a youtube video in a URI
        //using youtube-dl, a command-line program that downloads yt videos
        //additional information about it can be found here: https://github.com/rg3/youtube-dl
        public static string GetVideoAudioURI (string videoURL)
        {
            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"-4 --youtube-skip-dash-manifest --geo-bypass --no-check-certificate -f bestaudio --get-url {videoURL}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = false,
                },
            })
            {
                process.Start();
                string URI = process.StandardOutput.ReadToEnd().Replace("\n", "");
               
                return URI;
            }
        }

        //Same as above but with YoutubeExplode
        public static async Task<string> GetVideoURIExplode (string videoID)
        {
            try
            {
                var client = new YoutubeClient();
                var info = await client.GetVideoMediaStreamInfosAsync(videoID);
                var audio = info.Audio.OrderByDescending(x => x.Bitrate).FirstOrDefault();
                return audio.Url;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            return null;
        }

        //Checks if a url is a valid youtube one, following discord url parameters
        //in cases where the id was greater than 11, getting only the 11 characters still lead to the same video nevertheless
        public static YTUrlValidationResponse TryParseID(string url)
        {
            YTUrlValidationResponse result = new YTUrlValidationResponse();
            result.Valid = false;
            result.ID = null;

            if (url.Length >= 28)
                if (url.Substring(0, 17) == "https://youtu.be/")
                {
                    result.Valid = true;
                    result.ID = url.Substring(17, 11);
                    return result;
                }

            if (url.Length >= 37)
                if (url.Substring(0, 26) == "https://youtube.com/embed/")
                {
                    result.Valid = true;
                    result.ID = url.Substring(26, 11);
                    return result;
                }

            if (url.Length >= 39)
                if (url.Substring(0, 28) == "https://youtube.com/watch?v=")
                {
                    result.Valid = true;
                    result.ID = url.Substring(28, 11);
                    return result;
                }

            if (url.Length >= 41)
                if (url.Substring(0, 30) == "https://www.youtube.com/embed/")
                {
                    result.Valid = true;
                    result.ID = url.Substring(30, 11);
                    return result;
                }

            if (url.Length >= 43)
                if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=")
                {
                    result.Valid = true;
                    result.ID = url.Substring(32, 11);
                    return result;
                }

            return result;
        }

        public static YTUrlValidationResponse TryParsePlaylistID (string playlistUrl)
        {
            YTUrlValidationResponse result = new YTUrlValidationResponse();
            result.Valid = false;
            result.ID = null;

            //Minimum playlist id is 13 
            if (playlistUrl.Length >= 51)
                if (playlistUrl.Substring(0,38) == "https://www.youtube.com/playlist?list=")
                {
                    result.Valid = true;
                    result.ID = playlistUrl.Substring(38);
                }

            if (playlistUrl.Length >= 47)
                if (playlistUrl.Substring(0, 34) == "https://youtube.com/playlist?list=")
                {
                    result.Valid = true;
                    result.ID = playlistUrl.Substring(34);
                }

            if (playlistUrl.Length >= 62)
            {
                if (playlistUrl.Substring(0, 32) == "https://www.youtube.com/watch?v=")
                {
                    if (playlistUrl.Substring(43, 6) == "&list=")
                    {
                        result.Valid = true;
                        result.ID = playlistUrl.Substring(49);
                    }
                }
            }


            if (playlistUrl.Length >= 58)
            {
                if (playlistUrl.Substring(0, 28) == "https://youtube.com/watch?v=")
                {
                    if (playlistUrl.Substring(39, 6) == "&list=")
                    {
                        result.Valid = true;
                        result.ID = playlistUrl.Substring(45);
                    }
                }
            }

            if (playlistUrl.Length >= 48)
            {
                if (playlistUrl.Substring(0,17) == "https://youtu.be/")
                {
                    if (playlistUrl.Substring(28,1) == "/")
                    {
                        result.Valid = true;
                        result.ID = playlistUrl.Substring(29);
                    }
                }
            }
         
            return result;
        }


        //Removes everything from a youtube url except the video id,
        //for instance a url containing a specific time such as:
        //https://www.youtube.com/watch?v=abcdefghijk&t=31s
        //would be formatted to: https://www.youtube.com/watch?v=abcdefghijk
        public static string FormatUrl(string url)
        {
            if (!TryParseID(url).Valid)
                return null;

            //The https://www.youtube.com/embed/ url format cant contain
            //any additional info beyond its ID so there's no need to check it

            if (url.Substring(0, 17) == "https://youtu.be/")
                return url.Substring(0, 28);

            if (url.Substring(0, 28) == "https://youtube.com/watch?v=")
                return url.Substring(0, 39);

            return url.Substring(0, 43);
        }

      

    }
}
