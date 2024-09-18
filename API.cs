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

namespace WeatherApp
{
    public  class API
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
            var minTemperature= items.Min(x => x.temperature);
            var maxTemperature = items.Max(x => x.temperature);
            int avgTemperature = (int)items.Average(x => x.temperature);
            var medTemperature = FindMedianTemperature(items);

            string date = items.OrderBy(x => x.creationTime).FirstOrDefault().creationTime.ToString();
            string currentDate= $"{DateTime.Parse(date):u}";
            string dateToPredict =$"{DateTime.Parse(date).AddDays(2):u}";
            string[] partsOfDate= currentDate.Split(' ');
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
            List<TemperatureForDb> sortedList = list.OrderBy(x=>x.temperature).ToList();
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
            var list =unsortedList.OrderBy(r => r.creationTime).ToList();
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
            using (var client = new HttpClient(handler))
            {
                IConfiguration configuration;
                API api = new API();

                var requestBody = await api.requestBodyBuilder();
             
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

