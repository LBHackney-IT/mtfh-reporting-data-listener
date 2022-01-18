using Amazon.Glue.Model;

namespace MtfhReportingDataListener.Tests.Helper
{
    public static class MockGlueHelperMethods
    {

        public static bool CheckRequestsEquivalent(GetSchemaRequest expectedRequest, GetSchemaRequest receivedRequest)
        {
            return receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn;
        }
        public static bool CheckVersionRequestsEquivalent(GetSchemaVersionRequest expectedRequest, GetSchemaVersionRequest receivedRequest)
        {
            return receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn
                && receivedRequest.SchemaVersionNumber.LatestVersion == expectedRequest.SchemaVersionNumber.LatestVersion
                && receivedRequest.SchemaVersionNumber.VersionNumber == expectedRequest.SchemaVersionNumber.VersionNumber;
        }
    }
}
