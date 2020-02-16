using System;
using Cflb4K8S;
using Grpc.Core;

namespace MockIngressAgent
{
    public class ConfigRemoteClient
    {
        private ConfigRemote.ConfigRemoteClient Client { get; set; }
        
        public ConfigRemoteClient(ConfigRemote.ConfigRemoteClient configRemoteClient)
        {
            Client = configRemoteClient;
        }

        public StatusResponse Status()
        {
            try
            {
                var statusQuery = new StatusQuery();
                var statusResponse = Client.Status(statusQuery);
                return statusResponse;
            }
            catch (RpcException e)
            {
                Console.WriteLine(e.Status.Detail);
                return null;
            }
        }

        public RuleAck PushRule(string host, string target)
        {
            var rule = new Rule
            {
                Host = host,
                Target = target
            };

            var ruleAck = Client.PushRule(rule);

            return ruleAck;
        }
    }
}