using MtfhReportingDataListener.Gateway.Interfaces;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace MtfhReportingDataListener.Gateway
{
    public class SchemaRegistry : ISchemaRegistry
    {

        private HttpClient _httpClient;

        public SchemaRegistry(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetSchemaForTopic(string topic)
        {
            var schemaRegistryUrl = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_HOSTNAME");
            var response = await _httpClient.GetAsync($"http://{schemaRegistryUrl}/subjects/{topic}-value/versions/latest").ConfigureAwait(true);
            var stringResponse = await response.Content.ReadAsStringAsync();

            var deserializedResp = JsonSerializer.Deserialize<GetSchemaReponse>(stringResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return deserializedResp.Schema;
        }

    }
    public class GetSchemaReponse
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public int Version { get; set; }
        public string SchemaType { get; set; }
        public string Schema { get; set; }
    }
}
