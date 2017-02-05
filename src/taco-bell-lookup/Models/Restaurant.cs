using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table; 

namespace TacoBellFinder.Web.Models
{
    /// <summary>
    /// This class holds information about a single Taco Bell restaurant. Note that it 
    /// inherits from TableEntity. All entities in a table that are manipulated through the Azure Table Storage SDK
    /// must inherit from TableEntity. 
    /// </summary>
    public class Restaurant : TableEntity
    {
        public Restaurant()
        { }

        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="city">The city where the restaurant is located. This will be half of the partition key.</param>
        /// <param name="state">The state where the restaurant is located. This will be other half of the partition key.</param>
        /// <param name="restaurantNumber">The unique restaurant Id for this Taco Bell. This will become the row key.</param>
        public Restaurant(string city, string state, string restaurantId)
        {
            string pKey = state + "_" + city;
            pKey = pKey.ToLower();
            this.PartitionKey = pKey; 
            this.RowKey = restaurantId;
            this.City = city;
            this.State = state;
            this.RestaurantId = restaurantId;
        }

        /// <summary>
        /// This is a convenience method so that we can initialize a Restaurant record in one line of code. 
        /// </summary>
        public Restaurant(string city, string state, string restaurantId, string address, string zipCode, int healthRating, bool hasGorditas) 
            : this(city, state, restaurantId)
        {
            this.Address = address;
            this.Zipcode = zipCode;
            this.HealthRating = healthRating;
            this.HasGorditas = hasGorditas; 
        }

        //Now we can define other properties that are not the row or partition key. 
        public string Address { get; set; }
        public string Zipcode { get; set; }
        public int HealthRating { get; set; }
        public bool HasGorditas { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string RestaurantId { get; set; }
    }
}
