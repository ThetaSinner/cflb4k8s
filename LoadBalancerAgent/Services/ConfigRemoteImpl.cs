using System.Threading.Tasks;
using Cflb4K8S;
using Grpc.Core;

namespace LoadBalancer
{
    public class ConfigRemoteImpl : ConfigRemote.ConfigRemoteBase 
    {
        private readonly RoutingRules _routingRules;

        public ConfigRemoteImpl(RoutingRules routingRules)
        {
            this._routingRules = routingRules;
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
            
            return Task.FromResult(new RuleAck
            {
                Accepted = true
            });
        }

        public override Task<RuleAck> DropRule(RuleDrop request, ServerCallContext context)
        {
            _routingRules.DropRule(request.Host);
            
            return Task.FromResult(new RuleAck()
            {
                Accepted = true
            });
        }
    }
}
