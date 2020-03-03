using System.Threading.Tasks;
using Cflb4K8S;
using Grpc.Core;
using LoadBalancerAgent.Nginx;

namespace LoadBalancer
{
    public class ConfigRemoteImpl : ConfigRemote.ConfigRemoteBase 
    {
        private readonly RoutingRules _routingRules = new RoutingRules();
        
        private readonly ConfigExporter _configExporter;

        public ConfigRemoteImpl(ConfigExporter configExporter)
        {
            _configExporter = configExporter;
        }
        
        public override Task<StatusResponse> Status(StatusQuery request, ServerCallContext context)
        {
            return Task.FromResult(new StatusResponse
            {
                Initialised = _routingRules.Initialised 
            });
        }

        public override Task<RuleAck> PushRule(Rule request, ServerCallContext context)
        {
            _routingRules.AddRule(request.Name, request);
            
            _configExporter.Export(_routingRules);
            
            return Task.FromResult(new RuleAck
            {
                Accepted = true
            });
        }

        public override Task<RuleAck> DropRule(RuleDrop request, ServerCallContext context)
        {
            _routingRules.DropRule(request.Host);
            
            _configExporter.Export(_routingRules);
            
            return Task.FromResult(new RuleAck()
            {
                Accepted = true
            });
        }
    }
}
