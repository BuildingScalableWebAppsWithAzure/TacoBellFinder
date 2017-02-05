using Microsoft.AspNetCore.Mvc;
using TacoBellFinder.Web.Models;
using TacoBellFinder.Web.Services;
using System.Collections.Generic; 

namespace TacoBellFinder.Web.Controllers
{

    public class HomeController : Controller
    {
        private IRestaurantService _restaurantService; 

        public HomeController(IRestaurantService restaurantService)
        {
            _restaurantService = restaurantService; 
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Performs a point query to find the single restaurant with the specified restaurant Id
        /// that is within the specified city and state. 
        /// </summary>
        /// <param name="cityState"></param>
        /// <param name="restaurantId"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SearchByCityStateRestaurantId(string cityState, string restaurantId)
        {
            //In table storage, an empty row or partition key is valid, but not a null. Our MVC model binder will
            //give us a null string if a string is not submitted. We'll replace null values with an empty string here. 
            if (restaurantId == null)
            {
                restaurantId = string.Empty; 
            }
            Restaurant result = _restaurantService.SearchByCityStateRestaurantId(cityState, restaurantId).Result;
            List<Restaurant> results = new List<Restaurant>();
            if (result != null)
            {
                results.Add(result);
            }
            
            return View("Index", results); 
        }

        /// <summary>
        /// Retrieves all restaurants that have Gorditas on the menu. 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SearchForGorditas()
        {
            List<Restaurant> results = _restaurantService.HasGorditas().Result;
            return View("Index", results);
        }

        /// <summary>
        /// Retrieves all restaurants in a given city, state, and zip. This will execute a row range scan 
        /// query against a single partition for matching records. 
        /// </summary>
        [HttpGet]
        public IActionResult SearchByCityStateZip(string cityState, string address, string zipCode)
        {
            if (address == null) { address = string.Empty; }
            if (zipCode == null) { zipCode = string.Empty; }

            List<Restaurant> results = _restaurantService.SearchByCityStateAndZip(cityState, zipCode).Result;

            return View("Index", results);
        }

        /// <summary>
        /// Searches by state and health rating. This will result in a partition range scan with all matching
        /// states. To simplify this example, if the user has supplied a non-numeric healthRating, we will return no results. 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="healthRating"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SearchByStateAndHealthRating(string state, string healthRating)
        {
            int intHealthRating = 0; 
            if (! int.TryParse(healthRating, out intHealthRating))
            {
                return View("Index", new List<Restaurant>());
            }

            List<Restaurant> results = _restaurantService.SearchByStateAndMinimumHealthRating(state, intHealthRating).Result;
            return View("Index", results); 
        }

        /// <summary>
        /// This method displays a page with a single button that allows us to reset our test data. 
        /// </summary>
        /// <returns></returns>
        [HttpGet("initialize-data")]
        public IActionResult InitializeData()
        {
            return View();
        }

        /// <summary>
        /// Displays the Edit Restaurant form. 
        /// </summary>
        /// <returns></returns>
        [HttpGet("restaurant/{cityState}/{restaurantId}")]
        public IActionResult EditRestaurant(string cityState, string restaurantId)
        {
            Restaurant restaurantToEdit = _restaurantService.SearchByCityStateRestaurantId(cityState, restaurantId).Result;
            return View(restaurantToEdit); 
        }

        [HttpPost]
        public IActionResult UpdateRestaurant(Restaurant restaurantToUpdate)
        {
            _restaurantService.UpdateRestaurant(restaurantToUpdate);
            return View("RestaurantUpdated");
        }

        [HttpPost]
        public IActionResult DeleteRestaurant(Restaurant restaurantToDelete)
        {
           _restaurantService.DeleteRestaurant(restaurantToDelete);
            return View("RestaurantDeleted");
        }

    }
}
