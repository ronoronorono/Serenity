using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;
using RonoBot.Modules.Audio;

namespace RonoBot.Modules.Audio
{
    class YTVideoOperation
    {
        //Starts a youtube search with the given query, will return null if no results are found
        //Returns the first result that matches certain criterias explained further on.
        public static SearchResult YoutubeSearch(string query)
        {
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = SerenityCredentials.GoogleAPIKey() });
       
            //Creates the search request
            var searchListRequest = yt.Search.List("snippet");
            SearchResult result = null;

            //checks if the query is a youtube url
            //if it is, we will directly get the video's id
            //that is inside the url
            var res = TryParseID(query);
            if (res.Valid)
            {                
                var r = SearchVideoByID(res.VideoID);

                //fetching a video directly by its ID returns a video object, thus we must
                //fill a search result object with the video's object content, since this method
                //must return a search result. Of course, most of the fields wont be filled, 
                //however we wont be using them.
                SearchResult a = new SearchResult();

                a.Snippet = new SearchResultSnippet();
                a.Snippet.Title = r.Snippet.Title;
                a.Snippet.Thumbnails = r.Snippet.Thumbnails;

                a.Id = new ResourceId();
                a.Id.VideoId = r.Id.ToString();
                
                return a;
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
                            GetVideoDuration(item.Id.VideoId);
                            return item;                        
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
            return result;
        }

        //Since the search result object doesn't have the video's duration, we need the actual video object
        public static string GetVideoDuration (string videoID)
        {
            //Gets the video object
            Video video = SearchVideoByID(videoID);

            //Since a video's ID is unique to each one, we'll only get one result.
            //The time is given in the PT#MS#S format in XML, so we convert it
            TimeSpan t = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
            string duration = t.ToString();

            //Minor formatting here, if a video has 03:00(3 minutes) of duration, it'll
            //be written as 00:03:00 and we dont need the first 3 characters
            if (duration.StartsWith("00:"))
                duration = duration.Remove(0, 3);
           
            return duration;
        }

        public static Video SearchVideoByID(string videoID)
        {
            //Starts the youtube service
            YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() { ApiKey = SerenityCredentials.GoogleAPIKey() });

            //Starts a video search request, to return video objects
            var searchVideoRequest = yt.Videos.List("snippet,contentDetails");

            //We'll search for the video directly with its ID
            searchVideoRequest.Id = videoID;
            var res = searchVideoRequest.Execute();

            //If the ID is invalid
            if (res.Items.Count == 0)
                return null;

            //Otherwise, since the ID is unique to each video, we'll always get exactly one result
            return res.Items[0];
        }

        //Gets only the audio of a youtube video in a URI
        //URI are pretty much like URL's however, they "identify" a specific resource,
        //in this case the audio file inside a youtube video.
        public static string GetVideoAudioURI (string videoURL)
        {
            //We use youtube-dl, a command-line program that downloads yt videos
            //additional information about it can be found here: https://github.com/rg3/youtube-dl
            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"-4 --geo-bypass --no-check-certificate --skip-download -f bestaudio --get-url {videoURL}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            })
            {

                process.Start();
                string URI = process.StandardOutput.ReadToEnd().Replace("\n","");

                return URI;
            }
        }

        //Checks if a url is a valid youtube one, following discord url parameters
        //in cases where the id was greater than 11, getting only the 11 characters still lead to the same video nevertheless
        public static YTUrlValidationResponse TryParseID(string url)
        {
            YTUrlValidationResponse result = new YTUrlValidationResponse();
            result.Valid = false;
            result.VideoID = null;

            if (url.Length >= 28)
                if (url.Substring(0, 17) == "https://youtu.be/")
                {
                    result.Valid = true;
                    result.VideoID = url.Substring(17, 11);
                    return result;
                }

            if (url.Length >= 39)
                if (url.Substring(0, 28) == "https://youtube.com/watch?v=")
                {
                    result.Valid = true;
                    result.VideoID = url.Substring(28, 11);
                    return result;
                }

            if (url.Length >= 41)
                if (url.Substring(0, 30) == "https://www.youtube.com/embed/")
                {
                    result.Valid = true;
                    result.VideoID = url.Substring(30, 11);
                    return result;
                }

            if (url.Length >= 43)
                if (url.Substring(0, 32) == "https://www.youtube.com/watch?v=")
                {
                    result.Valid = true;
                    result.VideoID = url.Substring(32, 11);
                    return result;
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

            if (url.Substring(0, 17) == "https://youtu.be/")
                return url.Substring(0, 28);

            if (url.Substring(0, 28) == "https://youtube.com/watch?v=")
                return url.Substring(0, 39);

            return url.Substring(0, 43);
        }

      

    }
}
