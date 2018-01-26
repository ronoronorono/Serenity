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
    public class YTSearchObject
    {
        SearchResult ytresult;
        string query;
        int order;
        SocketUser author;

        public YTSearchObject(SearchResult ytresult, string query, int order, SocketUser author)
        {
            this.ytresult = ytresult;
            this.query = query;
            this.order = order;
            this.author = author;
        }

        public async Task<string> GetVideoDuration()
        {
            string url = "https://www.youtube.com/watch?v="+ ytresult.Id.VideoId;

            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"--skip-download --get-duration  {url}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            })
            {
                process.Start();
                var str = await process.StandardOutput.ReadToEndAsync();
                return str;
            }
        }

        public SearchResult Ytresult { get => ytresult; set => ytresult = value; }
        public string Query { get => query; set => query = value; }
        public int Order { get => order; set => order = value; }
        public SocketUser Author { get => author; set => author = value; }
    }
}
