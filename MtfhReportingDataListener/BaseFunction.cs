using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Strategies;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using MtfhReportingDataListener.Infrastructure;
using Hackney.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Confluent.SchemaRegistry;

namespace MtfhReportingDataListener
{
    /// <summary>
    /// Base class used for all functions
    /// Sets up the necessary DI container for the function.
    /// Any function-specific registration and configuration should be done in the derived class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class BaseFunction
    {
        protected readonly static JsonSerializerOptions _jsonOptions = JsonOptions.CreateJsonOptions();

        protected IConfigurationRoot Configuration { get; }

        internal BaseFunction()
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            var builder = new ConfigurationBuilder();

            Configure(builder);
            Configuration = builder.Build();
        }

        /// <summary>
        /// Base implementation
        /// Automatically adds environment variables and the appsettings file
        /// </summary>
        /// <param name="builder"></param>
        protected virtual void Configure(IConfigurationBuilder builder)
        {
            builder.AddJsonFile("appsettings.json");
            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
            if (!string.IsNullOrEmpty(environment))
            {
                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"appsettings.{environment}.json");
                if (File.Exists(path))
                    builder.AddJsonFile(path);
            }
            builder.AddEnvironmentVariables();
        }

        /// <summary>>
        /// Base implementation
        /// Automatically adds LogCallAspect
        /// </summary>
        /// <param name="services"></param>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddLogCallAspect();
        }
    }
}
