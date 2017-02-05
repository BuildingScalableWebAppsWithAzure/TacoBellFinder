using Microsoft.AspNetCore.Mvc;
using TacoBellFinder.Web.Services;

namespace TacoBellFinder.Web.Controllers
{
    [Route("api/[controller]")]
    public class RestaurantDataController : Controller
    {
        private IRestaurantService _restaurantService; 

        /// <summary>
        /// Dependency injection will inject the RestaurantService instance into our constructor. 
        /// </summary>
        public RestaurantDataController(IRestaurantService restaurantService)
        {
            _restaurantService = restaurantService; 
        }

        /// <summary>
        /// When called, this method will set up our test data for the project. It will do the following:
        /// 1. Ensure that our table Restaurants is created. 
        /// 2. Delete all entities in the Restaurants table.
        /// 3. Insert our handful of test records that we're using for illustration purposes. 
        /// </summary>
        [HttpPost("initialize")]
        public async void Initialize()
        {
            await _restaurantService.InitializeData();
        }
    }
}
