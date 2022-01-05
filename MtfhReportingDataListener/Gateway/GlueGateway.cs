using Amazon.Glue;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Gateway
{
    public class GlueGateway : IGlueGateway
    {
        public AmazonGlueClient _amazonGlueClient;
        public GlueGateway(AmazonGlueClient amazonGlueClient)
        {
            _amazonGlueClient = amazonGlueClient;
        }

        public string GetSchema()
        {
            return "";
        }
    }
}
