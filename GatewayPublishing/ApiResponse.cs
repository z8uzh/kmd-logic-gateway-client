using System;
using System.Collections.Generic;

namespace GatewayPublishing
{
    public class ApiResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid ProviderId { get; set; }
        public string Path { get; set; }
        public GatewayVisibility Visibility { get; set; }
        public Guid? ApiVersionSetId { get; set; }
        public string ApiVersion { get; set; }
        public string ApiVersionDescription { get; set; }
        public Uri OpenApiSpecUrl { get; set; }
        public ICollection<Guid> ProductIds { get; set; }
        public Uri LogoUrl { get; set; }
        public Uri DocumentationUrl { get; set; }
        public GatewaySynchronization Synchronization { get; set; }
        public ApiStatus? Status { get; set; }
        public bool? IsCurrent { get; set; }
    }
}
