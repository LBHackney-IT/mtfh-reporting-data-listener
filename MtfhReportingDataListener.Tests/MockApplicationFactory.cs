using Hackney.Core.Testing.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;


namespace MtfhReportingDataListener.Tests
{
    public class MockApplicationFactory
    {
        private readonly IHost _host;

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

    }
}
