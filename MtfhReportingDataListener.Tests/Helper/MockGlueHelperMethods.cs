using Amazon.Glue.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Tests.Helper
{
    public static class MockGlueHelperMethods
    {

        public static bool CheckRequestsEquivalent(GetSchemaRequest expectedRequest, GetSchemaRequest receivedRequest)
        {
            return receivedRequest.SchemaId.RegistryName == expectedRequest.SchemaId.RegistryName
                && receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn
                && receivedRequest.SchemaId.SchemaName == expectedRequest.SchemaId.SchemaName;
        }
        public static bool CheckVersionRequestsEquivalent(GetSchemaVersionRequest expectedRequest, GetSchemaVersionRequest receivedRequest)
        {
            return receivedRequest.SchemaId.RegistryName == expectedRequest.SchemaId.RegistryName
                && receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn
                && receivedRequest.SchemaId.SchemaName == expectedRequest.SchemaId.SchemaName
                && receivedRequest.SchemaVersionNumber.LatestVersion == expectedRequest.SchemaVersionNumber.LatestVersion
                && receivedRequest.SchemaVersionNumber.VersionNumber == expectedRequest.SchemaVersionNumber.VersionNumber;
        }
    }
}
