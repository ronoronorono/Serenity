﻿using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;

namespace RonoBot.Modules
{
    public class Searches : ModuleBase<SocketCommandContext>
    {
        //Searches for an image through the google search engine
        //selecting a random image from the 10 first found
        [Command("rimg")]
        public async Task SearchRImgAsync([Remainder] string search)
        {
 
            var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = SerenityCredentials.GoogleAPIKey() });
            
            var listRequest = customSearchService.Cse.List(search);
            Random rnd = new Random();
            listRequest.Cx = SerenityCredentials.CustomSearchEngineKey();

            //Restricting the search to only images
            listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
            
            //The results will be put inside this list
            IList<Result> paging = new List<Result>();

            //Begins the search
            paging = listRequest.Execute().Items;

            //The default number of results per search is 10, so the random integer must
            //be a value ranging from 0 to 9
            int index = rnd.Next(10);

            var embedImg = new EmbedBuilder()
                   .WithColor(new Color(240, 230, 231))
                   .WithAuthor(eab => eab.WithName(search))
                   .WithDescription(paging.ElementAt(index).Link)
                   .WithImageUrl(paging.ElementAt(index).Link);

            await Context.Channel.SendMessageAsync("",false,embedImg);
        }


        //Same as above but gets the first image instead.
        [Command("img")]
        public async Task SearchImgAsync([Remainder] string search)
        {

            var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = SerenityCredentials.GoogleAPIKey() });

            var listRequest = customSearchService.Cse.List(search);
            Random rnd = new Random();
            listRequest.Cx = SerenityCredentials.CustomSearchEngineKey();

            //Restricting the search to only images
            listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;

            //The results will be put inside this list
            IList<Result> paging = new List<Result>();

            //Begins the search
            paging = listRequest.Execute().Items;

            int index = 0;

            var embedImg = new EmbedBuilder()
                   .WithColor(new Color(240, 230, 231))
                   .WithAuthor(eab => eab.WithName(search))
                   .WithDescription(paging.ElementAt(index).Link)
                   .WithImageUrl(paging.ElementAt(index).Link);

            await Context.Channel.SendMessageAsync("", false, embedImg);
        }
    }
}
