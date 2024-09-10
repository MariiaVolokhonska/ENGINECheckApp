using System;
using System.Collections.Generic;
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
                // Request data goes here
                // The example below assumes JSON formatting which may be updated
                // depending on the format your endpoint expects.
                // More information can be found here:
                // https://docs.microsoft.com/azure/machine-learning/how-to-deploy-advanced-entry-script
                var requestBody = "{\r\n  \"Inputs\": {\r\n    \"input1\": [\r\n      {\r\n        \"Column1\": \"2024-07-04T00:00:00Z\",\r\n        \"Column2\": \"2024-07-02T00:00:00Z\",\r\n        \"Column3\": 88.5,\r\n        \"Column4\": 88.5,\r\n        \"Column5\": 85,\r\n        \"Column6\": 92,\r\n        \"Column7\": false\r\n      },\r\n      {\r\n        \"Column1\": \"2024-07-07T00:00:00Z\",\r\n        \"Column2\": \"2024-07-05T00:00:00Z\",\r\n        \"Column3\": 115.985,\r\n        \"Column4\": 115.971951,\r\n        \"Column5\": 88.32,\r\n        \"Column6\": 121.98,\r\n        \"Column7\": false\r\n      },\r\n      {\r\n        \"Column1\": \"2024-07-21T00:00:00Z\",\r\n        \"Column2\": \"2024-07-19T00:00:00Z\",\r\n        \"Column3\": 88.5,\r\n        \"Column4\": 88.5,\r\n        \"Column5\": 85,\r\n        \"Column6\": 92,\r\n        \"Column7\": false\r\n      },\r\n      {\r\n        \"Column1\": \"2024-07-22T00:00:00Z\",\r\n        \"Column2\": \"2024-07-20T00:00:00Z\",\r\n        \"Column3\": 88.5,\r\n        \"Column4\": 88.5,\r\n        \"Column5\": 85,\r\n        \"Column6\": 92,\r\n        \"Column7\": false\r\n      },\r\n      {\r\n        \"Column1\": \"2024-07-05T00:00:00Z\",\r\n        \"Column2\": \"2024-07-03T00:00:00Z\",\r\n        \"Column3\": 88.5,\r\n        \"Column4\": 88.5,\r\n        \"Column5\": 85,\r\n        \"Column6\": 92,\r\n        \"Column7\": true\r\n      }\r\n    ]\r\n  },\r\n  \"GlobalParameters\": {}\r\n}";

                // primary key for the endpoint
                const string apiKey = "hlUtfZh6h5sVUxedaBkefwvVppTb1LyH";
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("A key should be provided to invoke the endpoint");
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
               
                //uri of "post" method of api
                client.BaseAddress = new Uri("http://1e73423e-54da-4b42-a88d-e98c18c09013.westeurope.azurecontainer.io/score");

                var content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                
                HttpResponseMessage response = await client.PostAsync("", content).ConfigureAwait(false);
                
                //if status code 200 then get result from the model and extract word you need ("true" or "false", will or won't be a breakdown)
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    char[] separators = ['?', '!', ' ', ';', ':', ',', '{', '}', '[', ']'];
                    string[] source = result.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    var wordResult = source[source.Length - 4];
                    return wordResult;
                    
                }

                //if status code wasn't successful it will print to console status code and return string with the headers
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp,
                    // which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    //Console.WriteLine(responseContent);
                }
            }
        }

    }

}

