using System;

namespace GatewayPublishing
{
    public class PolicyRequest
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Xml { get; set; }
        public PolicyEntityType EntityType { get; set; }
    }
}
