using System;
using System.Collections.Generic;
using TacoBellFinder.Web.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;


namespace TacoBellFinder.Web.Services
{
    public class RestaurantService : IRestaurantService
    {
        private AzureStorageConfig _config;

        /// <summary>
        /// Constructor. Our configuration file containing our account name and storage key
        /// are injected in. 
        /// </summary>
        public RestaurantService(IOptions<AzureStorageConfig> config)
        {
            _config = config.Value;
        }

        /// <summary>
        /// This is a point query that will return the single restaurant identified by the partition key cityState and
        /// the row key restaurantId. 
        /// </summary>
        /// <param name="cityState"></param>
        /// <param name="restaurantId"></param>
        /// <returns></returns>
        public async Task<Restaurant> SearchByCityStateRestaurantId(string cityState, string restaurantId)
        {
            CloudTable restaurantsTable = await GetRestaurantsTable();
            TableOperation query = TableOperation.Retrieve<Restaurant>(cityState, restaurantId);
            TableResult retrievedResult = await restaurantsTable.ExecuteAsync(query);
            if (retrievedResult != null)
            {
                return (Restaurant)retrievedResult.Result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Searches by a city, state, and zip. This will result in a row range scan where we look at
        /// all potentially matching entities within a single partition. Note that we are using a continuation token.
        /// While it is extremely unlikely that there are more than a thousand restaurants in a single zip code, 
        /// it doesn't hurt to be be prepared for the unexpected. 
        /// </summary>
        /// <param name="cityState"></param>
        /// <param name="zip"></param>
        /// <returns></returns>
        public async Task<List<Restaurant>> SearchByCityStateAndZip(string cityState, string zip)
        {
            CloudTable restaurantsTable = await GetRestaurantsTable();
            string partitionKeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, cityState);
            string propertyFilter = TableQuery.GenerateFilterCondition("Zipcode", QueryComparisons.Equal, zip);
            string completeFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, propertyFilter);
            TableQuery<Restaurant> query = new TableQuery<Restaurant>().Where(completeFilter);
            List<Restaurant> restaurantsInZipCode = new List<Restaurant>();

            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<Restaurant> results = await restaurantsTable.ExecuteQuerySegmentedAsync(query, token);
                token = results.ContinuationToken;

                foreach (Restaurant r in results.Results)
                {
                    restaurantsInZipCode.Add(r);
                }
            } while (token != null);
            return restaurantsInZipCode;
        }

        /// <summary>
        /// Finds all restaurants in the given state that have the minimum specified health rating. This will require
        /// a partition range scan where we will find all partitions that match the supplied state. 
        /// </summary>
        /// <returns></returns>
        public async Task<List<Restaurant>> SearchByStateAndMinimumHealthRating(string state, int healthRating)
        {
            //we are building our partition key with [state]_[city]. To do a partition range scan, we'll have to combine
            //>= and <= operators, and append the underscore character and a letter to our state. This is because Azure lexicographical
            //order of strings when doing comparisons. We do not neet do worry with upper and lower case comparison issues because 
            //all of our city and state values are lowercased before insert. 
            CloudTable restaurantsTable = await GetRestaurantsTable();
            string partitionKeyGreaterThanOrEqualFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, state + "_a");
            string partitionKeyLessThanOrEqualFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, state + "_z");
            string healthRatingFilter = TableQuery.GenerateFilterConditionForInt("HealthRating", QueryComparisons.GreaterThanOrEqual, healthRating);
            string completeFilter = TableQuery.CombineFilters(partitionKeyGreaterThanOrEqualFilter, TableOperators.And, partitionKeyLessThanOrEqualFilter);
            completeFilter = TableQuery.CombineFilters(completeFilter, TableOperators.And, healthRatingFilter);
            TableQuery<Restaurant> query = new TableQuery<Restaurant>().Where(completeFilter);
            List<Restaurant> restaurantsList = new List<Restaurant>();

            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<Restaurant> results = await restaurantsTable.ExecuteQuerySegmentedAsync(query, token);
                token = results.ContinuationToken;

                foreach (Restaurant r in results.Results)
                {
                    restaurantsList.Add(r);
                }
            } while (token != null);
            return restaurantsList;
        }

        /// <summary>
        /// We're going to look at every record in the restaurants table to see which restaurants have Gorditas. This is the 
        /// most expensive table operation. Please, do not try this at home. Or in a production system. 
        /// </summary>
        /// <returns></returns>
        public async Task<List<Restaurant>> HasGorditas()
        {
            CloudTable restaurantsTable = await GetRestaurantsTable();
            string hasGorditasFilter = TableQuery.GenerateFilterConditionForBool("HasGorditas", QueryComparisons.Equal, true);
            TableQuery<Restaurant> query = new TableQuery<Restaurant>().Where(hasGorditasFilter);
            List<Restaurant> restaurantsList = new List<Restaurant>();

            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<Restaurant> results = await restaurantsTable.ExecuteQuerySegmentedAsync(query, token);
                token = results.ContinuationToken;

                foreach (Restaurant r in results.Results)
                {
                    restaurantsList.Add(r);
                }
            } while (token != null);
            return restaurantsList;
        }

        /// <summary>
        /// Updates the specified restaurant by replacing the record. The replace will be performed based on the row and 
        /// partition key, and all properties within Table storage for the existing restaurant will be overwritten with
        /// the values stored in r. 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public async Task UpdateRestaurant(Restaurant r)
        {
            CloudTable restaurantsTable = await GetRestaurantsTable();
            TableOperation updateOperation = TableOperation.Replace(r);
            await restaurantsTable.ExecuteAsync(updateOperation);
        }

        /// <summary>
        /// Deletes the supplied restaurant. Remember that Restaurant r must have a partition key and 
        /// row key defined, and no other properties matter. 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public async Task DeleteRestaurant(Restaurant r)
        {
            CloudTable restaurantsTable = await GetRestaurantsTable();
            TableOperation deleteOperation = TableOperation.Delete(r);
            await restaurantsTable.ExecuteAsync(deleteOperation);
        }

        /// <summary>
        /// Inserts or replaces all of our initial data for this project. 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> InitializeData()
        {
            CloudTable restaurantsTable = await GetRestaurantsTable();

            //now, let's refresh our data using insert or replace. We'll frame all of our operations for a single partition in a batch.
            //This will give us transaction support, and will ensure that we're only charged for one storage operation.

            TableBatchOperation chattanoogaBatchOp = new TableBatchOperation();
            Restaurant chattanooga1 = new Restaurant("Chattanooga", "TN", "00001", "9918 Pennywood Lane", "37363", 98, true);
            Restaurant chattanooga2 = new Restaurant("Chattanooga", "TN", "00002", "837 Stellar View", "37405", 100, true);
            Restaurant chattanooga3 = new Restaurant("Chattanooga", "TN", "00019", "1467 Market Street", "37409", 97, false);
            chattanoogaBatchOp.InsertOrReplace(chattanooga1);
            chattanoogaBatchOp.InsertOrReplace(chattanooga2);
            chattanoogaBatchOp.InsertOrReplace(chattanooga3);
            await restaurantsTable.ExecuteBatchAsync(chattanoogaBatchOp);

            TableBatchOperation knoxvilleBatchOp = new TableBatchOperation();
            Restaurant knoxville1 = new Restaurant("Knoxville", "TN", "00119", "27 Cumberland Blvd", "37996", 88, true);
            Restaurant knoxville2 = new Restaurant("Knoxville", "TN", "00128", "987 Scenic Highway", "37994", 88, false);
            knoxvilleBatchOp.InsertOrReplace(knoxville1);
            knoxvilleBatchOp.InsertOrReplace(knoxville2);
            await restaurantsTable.ExecuteBatchAsync(knoxvilleBatchOp);

            TableBatchOperation charlestonBatchOp = new TableBatchOperation();
            Restaurant charleston1 = new Restaurant("Charleston", "TN", "02006", "100 Elm Street", "37310", 95, true);
            Restaurant charleston2 = new Restaurant("Charleston", "TN", "02298", "15010 NE 36th Street", "37996", 97, false);
            charlestonBatchOp.InsertOrReplace(charleston1);
            charlestonBatchOp.InsertOrReplace(charleston2);
            await restaurantsTable.ExecuteBatchAsync(charlestonBatchOp);

            //let's throw in one Taco Bell out of state so that we can verify a partition range scan is returning the correct results. 
            Restaurant birmingham = new Restaurant("Birmigham", "AL", "92763", "839 Sherman Oaks Drive", "35235", 70, true);
            TableOperation insertBirminghamOp = TableOperation.InsertOrReplace(birmingham);
            await restaurantsTable.ExecuteAsync(insertBirminghamOp);

            return true;
        }

        /// <summary>
        /// Returns a reference to the restaurants table. Will create the table if the Restaurants table doesn't exist within
        /// the storage account. 
        /// </summary>
        /// <returns></returns>
        private async Task<CloudTable> GetRestaurantsTable()
        {
            CloudStorageAccount storageAccount = GetCloudStorageAccount();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable restaurantsTable = tableClient.GetTableReference("Restaurants");
            await restaurantsTable.CreateIfNotExistsAsync();
            return restaurantsTable;
        }

        /// <summary>
        /// Attempts to connect to the Cloud Storage Account defined by the storage account connection string specified
        /// in appsettings.json. 
        /// </summary>
        /// <returns>A CloudStorageAccount instance if the connection is successful. Otherwise throws an exception.</returns>
        private CloudStorageAccount GetCloudStorageAccount()
        {
            CloudStorageAccount storageAccount = null;
            if (!CloudStorageAccount.TryParse(_config.StorageConnectionString, out storageAccount))
            {
                throw new Exception("Could not connect to the cloud storage account. Please check the storage connection string.");
            }
            return storageAccount;
        }
    }
}
