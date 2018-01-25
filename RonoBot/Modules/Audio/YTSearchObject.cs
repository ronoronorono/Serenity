using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace RonoBot.Modules.Audio
{
    public class YTSearchObject
    {
        SearchResult ytresult;
        string query;
        int order;

        public YTSearchObject(SearchResult ytresult, string query, int order)
        {
            this.ytresult = ytresult;
            this.query = query;
            this.order = order;
        }

        public SearchResult Ytresult { get => ytresult; set => ytresult = value; }
        public string Query { get => query; set => query = value; }
        public int Order { get => order; set => order = value; }
    }
}
