using System;
using System.Collections.Generic;
using TacoBellFinder.Web.Models; 

namespace TacoBellFinder.Web.Models
{
    public class SearchResults
    {
        public SearchResults()
        {
            this.Restaurants = new List<Restaurant>(); 
        }

        /// <summary>
        /// All restaurants that match the user's query. 
        /// </summary>
        public List<Restaurant> Restaurants { get; set; }
    }
}
