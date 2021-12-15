using Amazon.DynamoDBv2;
using Hackney.Core.DynamoDb;
using Hackney.Core.Testing.DynamoDb;
using Hackney.Core.Testing.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace BaseListener.Tests
{
    // TODO - Remove DynamoDb parts if not required

    public class MockApplicationFactory
    {
        private readonly List<TableDef> _tables = new List<TableDef>
        {
            // Define all tables required by the application here.
            // The definition should be exactly the same as that used in real deployed environments
            new TableDef { Name = "SomeTable", KeyName = "id", KeyType = ScalarAttributeType.S }
        };
        public IDynamoDbFixture DynamoDbFixture { get; private set; }

        private readonly IHost _host;

        public MockApplicationFactory()
        {
            EnsureEnvVarConfigured("DynamoDb_LocalMode", "true");
            EnsureEnvVarConfigured("DynamoDb_LocalServiceUrl", "http://localhost:8000");

            _host = CreateHostBuilder().Build();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (DynamoDbFixture != null)
                    DynamoDbFixture.Dispose();

                if (null != _host)
                {
                    _host.StopAsync().GetAwaiter().GetResult();
                    _host.Dispose();
                }

                _disposed = true;
            }
        }

        private static void EnsureEnvVarConfigured(string name, string defaultValue)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                Environment.SetEnvironmentVariable(name, defaultValue);
        }

        public IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder(null)
           .ConfigureAppConfiguration(b => b.AddEnvironmentVariables())
           .ConfigureServices((hostContext, services) =>
           {
               services.ConfigureDynamoDB();
               services.ConfigureDynamoDbFixture();

               var serviceProvider = services.BuildServiceProvider();

               LogCallAspectFixture.SetupLogCallAspect();

               DynamoDbFixture = serviceProvider.GetRequiredService<IDynamoDbFixture>();
               DynamoDbFixture.EnsureTablesExist(_tables);
           });
    }
}
