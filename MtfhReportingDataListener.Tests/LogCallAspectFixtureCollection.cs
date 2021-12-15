using Hackney.Core.Testing.Shared;
using Xunit;

namespace MtfhReportingDataListener.Tests
{
    [CollectionDefinition("LogCall collection")]
    public class LogCallAspectFixtureCollection : ICollectionFixture<LogCallAspectFixture>
    { }
}
