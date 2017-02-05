using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TacoBellFinder.Web.Models; 

namespace TacoBellFinder.Web.Services
{
    public interface IRestaurantService
    {
        Task<bool> InitializeData();
        Task<Restaurant> SearchByCityStateRestaurantId(string cityState, string restaurantId);
        Task<List<Restaurant>> SearchByCityStateAndZip(string cityState, string zip);
        Task<List<Restaurant>> SearchByStateAndMinimumHealthRating(string state, int healthRating);
        Task<List<Restaurant>> HasGorditas();
        Task UpdateRestaurant(Restaurant r);
        Task DeleteRestaurant(Restaurant r);
    }
}
