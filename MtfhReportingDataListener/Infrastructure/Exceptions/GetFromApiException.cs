using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MtfhReportingDataListener.Infrastructure.Exceptions
{
    public class GetFromApiException : Exception
    {
        public string EntityType { get; }
        public string Route { get; }
        public Guid EntityId { get; }
        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; }
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public GetFromApiException(string type, string route, Guid id, HttpStatusCode statusCode, string responseBody)
            : this(type, route, new List<KeyValuePair<string, IEnumerable<string>>>(), id, statusCode, responseBody)
        { }

        public GetFromApiException(string type, string route, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            Guid id, HttpStatusCode statusCode, string responseBody)
            : base($"Failed to get {type} details for id {id}. Route: {route}; Status code: {statusCode}; Message: {responseBody}")
        {
            EntityType = type;
            Route = route;
            Headers = headers;
            EntityId = id;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
