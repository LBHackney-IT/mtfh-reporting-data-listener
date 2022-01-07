using Amazon.Glue;
using Amazon.Glue.Model;
using AutoFixture;
using Hackney.Core.Testing.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using MtfhReportingDataListener.Tests.Helper;
using System.Threading;

namespace MtfhReportingDataListener.Tests
{
    public class MockApplicationFactory
    {
        private readonly IHost _host;
        private readonly Fixture _fixture = new Fixture();


        public MockApplicationFactory()
        {
            EnsureEnvVarConfigured("DATAPLATFORM_KAFKA_HOSTNAME", "localhost:9092");

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
               var serviceProvider = services.BuildServiceProvider();

               LogCallAspectFixture.SetupLogCallAspect();
           });

        public IAmazonGlue MockAWSGlue(string registryName, string schemaArn, string schemaName, string schemaDefinition)
        {
             var mockAmazonGlue = new Mock<IAmazonGlue>();

            var getSchemaRequest = new GetSchemaRequest()
            {
                SchemaId = new SchemaId()
                {

                    RegistryName = registryName,
                    SchemaArn = schemaArn,
                    SchemaName = schemaName
                }
            };

            var getSchemaVersion = new GetSchemaVersionRequest()
            {
                SchemaId = getSchemaRequest.SchemaId,
                SchemaVersionNumber = new SchemaVersionNumber()
                {
                    LatestVersion = true,
                    VersionNumber = 2
                }
            };
            var getSchemaResponse = new GetSchemaResponse()
            {
                LatestSchemaVersion = getSchemaVersion.SchemaVersionNumber.VersionNumber,
                RegistryName = registryName,
                SchemaArn = schemaArn,
                SchemaName = schemaName
            };

            mockAmazonGlue.Setup(x => x.GetSchemaAsync(It.Is<GetSchemaRequest>(x => MockGlueHelperMethods.CheckRequestsEquivalent(getSchemaRequest, x)), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(getSchemaResponse);

            mockAmazonGlue.Setup(x => x.GetSchemaVersionAsync(
                    It.Is<GetSchemaVersionRequest>(x=> MockGlueHelperMethods.CheckVersionRequestsEquivalent(getSchemaVersion,x)),
                    It.IsAny<CancellationToken>()
                )).ReturnsAsync(new GetSchemaVersionResponse { SchemaDefinition = schemaDefinition});



            return mockAmazonGlue.Object;
        }
    }
}
