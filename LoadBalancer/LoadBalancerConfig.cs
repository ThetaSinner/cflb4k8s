using Microsoft.Extensions.Configuration;

namespace LoadBalancer
{
    public class LoadBalancerConfig
    {
        public IConfiguration Configuration { get; set; }
        
        public RoutingRules RoutingRules { get; set; }
    }
}