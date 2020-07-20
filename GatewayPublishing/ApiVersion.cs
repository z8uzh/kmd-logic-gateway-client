using System.Collections.Generic;

namespace GatewayPublishing
{
    public class ApiVersion
    {
        public string PathIdentifier { get; set; }
        public string ApiLogoFile { get; set; }
        public string ApiDocumentation { get; set; }
        public string OpenApiSpecFile { get; set; }
        public string Published { get; set; }
        public string[] ProductNames { get; set; }
        public string PoliciesXmlFile { get; set; }
        public List<Revision> Revisions { get; set; }
    }
}