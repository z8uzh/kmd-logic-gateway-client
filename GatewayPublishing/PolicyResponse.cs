using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayPublishing
{
    public class PolicyResponse
    {
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Xml { get; set; }
        public PolicyEntityType EntityType { get; set; }
    }
}
