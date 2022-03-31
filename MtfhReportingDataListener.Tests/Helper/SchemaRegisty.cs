using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MtfhReportingDataListener.Tests.Helper
{
    public static class SchemaRegistry
    {
        public async static Task SaveSchemaForTopic(HttpClient httpClient, string schemaRegistryUrl, string schema, string topicName)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.schemaregistry.v1+json"));
            var payload = JsonSerializer.Serialize(new { schema = schema });
            var data = new StringContent(payload, Encoding.UTF8, "application/vnd.schemaregistry.v1+json");
            Console.WriteLine(schemaRegistryUrl);
            var response = await httpClient.PostAsync($"http://{schemaRegistryUrl}/subjects/{topicName}-value/versions", data);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Saving Schema unsuccessful!");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }
        }
    }

}
