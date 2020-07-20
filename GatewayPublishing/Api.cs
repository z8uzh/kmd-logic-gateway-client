using System;
using System.Collections.Generic;

namespace GatewayPublishing
{
    public class Api
    {
        public string Name { get; set; }
        public List<ApiVersion> ApiVersions { get; set; }
        public string Path { get; set; }
    }
}