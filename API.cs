using System.Net.Http.Headers;
using Azure;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace WeatherApp
{
    public static class API
    {
        private readonly CosmosDb<TemperatureForDb> _cosmosDbService;
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
