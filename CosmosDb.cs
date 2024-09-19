using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
namespace WeatherApp
{
    public class CosmosDb<T> where T : class
    {
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public CosmosDb(string accountEndpoint, string accountKey, string databaseName, string containerName)
        {
            _cosmosClient = new CosmosClient(accountEndpoint, accountKey);
            _database = _cosmosClient.GetDatabase(databaseName);
            _container = _database.GetContainer(containerName);
        }
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Check if the database exists
                DatabaseResponse databaseResponse = await _database.ReadAsync();
                Console.WriteLine($"Database id: {databaseResponse.Database.Id}");

                // Check if the container exists
                ContainerResponse containerResponse = await _container.ReadContainerAsync();
                Console.WriteLine($"Container id: {containerResponse.Container.Id}");

                return true;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"CosmosException: {ex.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return false;
            }
        }
        public async Task<List<T>> GetAllItemsAsync()
        {
            var query = _container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true)
                                  .AsQueryable();

            // To List Execution
            return query.ToList();
        }


    }
}
