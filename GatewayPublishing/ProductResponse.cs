using System;
using System.Collections.Generic;

namespace GatewayPublishing
{
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool ApiKeyRequired { get; set; }
        public bool ClientCredentialRequired { get; set; }
        public Guid? ApplicationId { get; set; }
        public bool ProviderApprovalRequired { get; set; }
        public string ProductTerms { get; set; }
        public GatewayVisibility Visibility { get; set; }
        public ICollection<Guid> ApiIds { get; set; }
        public Guid ProviderId { get; set; }
        public Uri LogoUrl { get; set; }
        public Uri DocumentationUrl { get; set; }
        public GatewaySynchronization Synchronization { get; set; }
    }
}
