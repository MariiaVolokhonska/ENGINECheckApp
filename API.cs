using Microsoft.Azure.Cosmos.Serialization.HybridRow.RecordIO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WeatherApp.Models;
using Azure.Communication.Email;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;

namespace WeatherApp
{
    public static class API
    {
        private readonly CosmosDb<TemperatureForDb> _cosmosDbService;
        public API()
        {

            var accountEndpoint = "https://servicecosmosdb.documents.azure.com:443/";
            var accountKey = "8cGksnNqTPVgWPBgFtNuamm2MIChlrUAvyAhMC1C0q9Djo4jBumpf6zTLIjNhWAuLcPn1pXShI1bACDbAxVQjg==";
            var databaseName = "Database";
            var containerName = "Container";

            _cosmosDbService = new CosmosDb<TemperatureForDb>(accountEndpoint, accountKey, databaseName, containerName);
        }
        public async ValueTask<string> requestBodyBuilder()
        {
            List<TemperatureForDb> items = await _cosmosDbService.GetAllItemsAsync();
            var minTemperature = items.Min(x => x.temperature);
            var maxTemperature = items.Max(x => x.temperature);
            int avgTemperature = (int)items.Average(x => x.temperature);
            var medTemperature = FindMedianTemperature(items);

            string date = items.OrderBy(x => x.creationTime).FirstOrDefault().creationTime.ToString();
            string currentDate = $"{DateTime.Parse(date):u}";
            string dateToPredict = $"{DateTime.Parse(date).AddDays(2):u}";
            string[] partsOfDate = currentDate.Split(' ');
            string[] partsOfDateToPredict = dateToPredict.Split(' ');

            string IsBreakdownToday = DetectBreakdown(items).ToString().ToLower();


            var requestBody = $@"
{{
  ""Inputs"": {{
    ""input1"": [
      {{
        ""Column1"": ""{partsOfDateToPredict[0]}T{partsOfDateToPredict[1]}"",
        ""Column2"": ""{partsOfDate[0]}T{partsOfDate[1]}"",
        ""Column3"": {medTemperature},
        ""Column4"": {avgTemperature},
        ""Column5"": {minTemperature},
        ""Column6"": {maxTemperature},
        ""Column7"": {IsBreakdownToday}
      }}
    ]
  }},
  ""GlobalParameters"": {{}}
}}";


            return requestBody;
        }
        public int FindMedianTemperature(List<TemperatureForDb> list)
        {
            List<TemperatureForDb> sortedList = list.OrderBy(x => x.temperature).ToList();
            if (list.Count % 2 != 0)
            {
                return sortedList[sortedList.Count / 2].temperature;
            }
            else
            {

                int mid1 = sortedList[(sortedList.Count / 2) - 1].temperature;
                int mid2 = sortedList[sortedList.Count / 2].temperature;
                return (mid1 + mid2) / 2;
            }
        }
        static bool DetectBreakdown(List<TemperatureForDb> unsortedList)
        {
            var list = unsortedList.OrderBy(r => r.creationTime).ToList();
            const double breakdownThreshold = 120.0; // min temperature that can be considered for a breakdown verification purposes
            const int requiredDurationInMinutes = 5; // minimum amount of minutes IN RAW to be considered a breakdown

            // LINQ: find max amount of minutes inn a raw when temperature >= 120°C
            int consecutiveAboveThreshold = list
                .Select(r => r.temperature >= breakdownThreshold) // convert to bool
                .Aggregate((max: 0, current: 0), (acc, isAboveThreshold) => isAboveThreshold
                    ? (Math.Max(acc.max, acc.current + 1), acc.current + 1) // increase counter if temp>=120
                    : (acc.max, 0) // max = 0 if temp<120
                ).max;

            // return true if more then 5 minutes in raw was 120 degrees
            return consecutiveAboveThreshold >= requiredDurationInMinutes;
        }
        public static async ValueTask<string> InvokeRequestResponseService()
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            using var client = new HttpClient(handler);

            // Request body content
            var requestBody = "{\r\n  \"Inputs\": {\r\n    \"input1\": [\r\n      {\r\n        \"Column1\": \"2024-07-04T00:00:00Z\",\r\n        \"Column2\": \"2024-07-02T00:00:00Z\",\r\n        \"Column3\": 88.5,\r\n        \"Column4\": 88.5,\r\n        \"Column5\": 85,\r\n        \"Column6\": 92,\r\n        \"Column7\": false\r\n      },\r\n      {\r\n        \"Column1\": \"2024-07-07T00:00:00Z\",\r\n        \"Column2\": \"2024-07-05T00:00:00Z\",\r\n        \"Column3\": 115.985,\r\n        \"Column4\": 115.971951,\r\n        \"Column5\": 88.32,\r\n        \"Column6\": 121.98,\r\n        \"Column7\": false\r\n      }\r\n    ]\r\n  },\r\n  \"GlobalParameters\": {}\r\n}";

            const string apiKey = "hlUtfZh6h5sVUxedaBkefwvVppTb1LyH";
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("API key is missing.");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.BaseAddress = new Uri("http://1e73423e-54da-4b42-a88d-e98c18c09013.westeurope.azurecontainer.io/score");

            var content = new StringContent(requestBody);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response = await client.PostAsync("", content).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                // Logic to analyze the result and return a more descriptive message
                if (result.Contains("true"))
                {   
                    return "true";
                }
                else if (result.Contains("false"))
                {
                    return "false";
                }
                else
                {
                    return "Could not determine the status.";
                }
            }
            else
            {
                Console.WriteLine($"The request failed with status code: {response.StatusCode}");
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            }
        }

        public static async Task SendEmail()
        {
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=serviceblobstorage1;AccountKey=jMSjaBX5XHPYvkPBWiH49WUR7hf2m2vCSeMTxCcfhtKbq5Ag8WvJt8q8LilTZmjgK9yo0Q3BapVA+AStObPq6w==;EndpointSuffix=core.windows.net";
            string container = "credential";

            // Get a reference to a container named "sample-container" and then create it
            BlobContainerClient blobContainerClient = new BlobContainerClient(connectionString, container);
            List<string> emailAddresses = new List<string>();
            blobContainerClient.CreateIfNotExists();

            // List all blobs in the container
            var blobs = blobContainerClient.GetBlobs();
            foreach (BlobItem blobItem in blobs)
            {
                BlobClient blobClient = new BlobClient(connectionString, container, blobItem.Name);
                using var stream = blobClient.OpenRead();
                using StreamReader reader = new StreamReader(stream);
                {
                    while (!reader.EndOfStream)
                    {
                        //Console.WriteLine(reader.ReadLine());
                        string? data = reader.ReadLine();
                        if (data != null)
                        {
                            string[] userData = data.Split(",");
                            Console.WriteLine(userData[0]);
                            emailAddresses.Add(userData[0]);
                        }
                    }
                }
            }

            string emailConnectionString = "endpoint=https://servicecomms.europe.communication.azure.com/;accesskey=2jMcD9lAXvh21nNuBEhn9ZJGvYdPXYVaStMVmJGEhKwj84LxY82yJQQJ99AIACULyCpJZdPgAAAAAZCSGgbd";
            var emailClient = new EmailClient(emailConnectionString);

            // Step 4: Create EmailRecipients from the parsed email addresses
            var emailRecipients = new EmailRecipients(
                emailAddresses.ConvertAll(email => new EmailAddress(email))
            );

            var subject = "Send email sample - Multiple recipients";
            var emailContent = new EmailContent(subject)
            {
                PlainText = "Schedule a maintenance to keep the engine in optimal condidtion.",
                Html = @"<html><body><a href=""https://imgbb.com/""><img src=""https://i.ibb.co/x60sg4R/logo.png"" alt=""logo"" border=""0"" style=""max-height: 100px;""></a><br/><h4>Attention, dear User!</h4><p>There could be a possible engine breakdown.<br/>Schedule a maintenance to keep the engine in optimal condidtion.</p></body></html>"
            };
                
            // Step 5: Create the email message
            var emailMessage = new EmailMessage(
                senderAddress: "DoNotReply@b77facce-f4ae-4380-842d-98f541411d23.azurecomm.net",
                recipients: emailRecipients,
                emailContent
            );

            try
            {
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                Console.WriteLine($"Email Sent. Status = {emailSendOperation.Value.Status}");

                /// Get the OperationId so that it can be used for tracking the message for troubleshooting
                string operationId = emailSendOperation.Id;
                Console.WriteLine($"Email operation id = {operationId}");
            }
            catch (RequestFailedException ex)
            {
                /// OperationID is contained in the exception message and can be used for troubleshooting purposes
                Console.WriteLine($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
            }        
        }
    }
}
