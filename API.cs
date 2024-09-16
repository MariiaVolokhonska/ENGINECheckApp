using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp
{
    public static class API
    {
        public static async ValueTask<string> InvokeRequestResponseService()
        {
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                        (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            using (var client = new HttpClient(handler))
            {
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
        }
    }
}
