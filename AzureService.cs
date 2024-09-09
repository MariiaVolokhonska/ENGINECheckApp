using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using WeatherApp.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WeatherApp
{
    public class AzureService
    {
        static string connectionString = "DefaultEndpointsProtocol=https;AccountName=serviceblobstorage1;AccountKey=jMSjaBX5XHPYvkPBWiH49WUR7hf2m2vCSeMTxCcfhtKbq5Ag8WvJt8q8LilTZmjgK9yo0Q3BapVA+AStObPq6w==;EndpointSuffix=core.windows.net";
        static string telemetryContainer = "telemetry";
        static string credentialContainer = "credential";

        public List<TempratureData> GetWeatherData()
        {
            var weatherData = new List<TempratureData>();

            BlobContainerClient blobContainerClient = new BlobContainerClient(connectionString, telemetryContainer);

            // Ensure the container exists
            blobContainerClient.CreateIfNotExists();

            // List all blobs in the container
            var blobs = blobContainerClient.GetBlobs();
            foreach (BlobItem blobItem in blobs.Where(x => x.Name.Equals("data.csv")))
            {
                BlobClient blobClient = new BlobClient(connectionString, telemetryContainer, blobItem.Name);
                using var stream = blobClient.OpenRead();
                using StreamReader reader = new StreamReader(stream);
                {
                    bool isHeader = true;
                    while (!reader.EndOfStream)
                    {   
                        string? data = reader.ReadLine();
                        if (data != null)
                        {
                            if (!isHeader)
                            {
                                string[] tempratureInfo = data.Split(",");
                                if (tempratureInfo.Length >= 3) // Ensure correct number of elements
                                {
                                    if (DateTime.TryParse(tempratureInfo[0], out DateTime creationTime))
                                    {
                                        weatherData.Add(new TempratureData
                                        {
                                            CreationTime = creationTime.ToUniversalTime(),
                                            Temperature = double.Parse(tempratureInfo[1], CultureInfo.InvariantCulture),
                                            //Humidity = double.Parse(tempratureInfo[2], CultureInfo.InvariantCulture),
                                            Info = tempratureInfo[2]
                                        });
                                    }
                                }
                            }
                            isHeader = false;
                        }
                    }
                }
            }
            Console.WriteLine("asdasdasd");
            return weatherData;
        }

        public bool IsValidUser(string username, string password)
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(connectionString, credentialContainer);

            // Ensure the container exists
            blobContainerClient.CreateIfNotExists();

            var blobs = blobContainerClient.GetBlobs();
            foreach (BlobItem blobItem in blobs)
            {
                BlobClient blobClient = new BlobClient(connectionString, credentialContainer, blobItem.Name);
                using var stream = blobClient.OpenRead();
                using StreamReader reader = new StreamReader(stream);
                {
                    while (!reader.EndOfStream)
                    {
                        string? data = reader.ReadLine();
                        if (data != null)
                        {
                            string[] userData = data.Split(",");
                            if (userData.Length >= 3 && userData[1].Equals(username) && userData[2].Equals(password))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public async Task RegisterUserAsync(string newUserEmail, string newUsername, string newPassword)
        {
            try
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(connectionString, credentialContainer);

                // Ensure the container exists
                await blobContainerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = blobContainerClient.GetBlobClient("credentials.csv");

                // Download the existing CSV content
                string csvContent = string.Empty;
                if (await blobClient.ExistsAsync())
                {
                    var blobDownloadInfo = await blobClient.DownloadAsync();
                    using var streamReader = new StreamReader(blobDownloadInfo.Value.Content);
                    csvContent = await streamReader.ReadToEndAsync();
                }

                // Add new user data
                string newUserRecord = $"{newUserEmail},{newUsername},{newPassword}\n";
                string updatedCsvContent = csvContent + newUserRecord;

                // Upload the updated CSV content
                using var updatedStream = new MemoryStream(Encoding.UTF8.GetBytes(updatedCsvContent));
                await blobClient.UploadAsync(updatedStream, overwrite: true);
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
