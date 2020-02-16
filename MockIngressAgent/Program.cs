using System;
using System.Threading;
using Cflb4K8S;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace MockIngressAgent
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var targetConfig = config["LOAD_BALANCER_TARGET"];
            var target = string.IsNullOrWhiteSpace(targetConfig) ? "127.0.0.1:3301" : targetConfig;
            
            Console.WriteLine($"Connecting to load balancer configuration server on [{target}]");
            
            var channel = new Channel(target, ChannelCredentials.Insecure);
            var client = new ConfigRemoteClient(new ConfigRemote.ConfigRemoteClient(channel));

            while (true)
            {
                var statusResponse = client.Status();

                if (statusResponse == null)
                {
                    Console.WriteLine("Service not available.");
                }
                else if (!statusResponse.Initialised)
                {
                    var ruleAck = client.PushRule("mocka", "http://mock-web-api:5000");
                    if (ruleAck.Accepted)
                    {
                        Console.WriteLine("Created rule for 'mocka'");
                    }
                    
                    ruleAck = client.PushRule("mockb", "http://mock-web-api:5000");
                    if (ruleAck.Accepted)
                    {
                        Console.WriteLine("Created rule for 'mockb'");
                    }
                }
                
                Thread.Sleep(5000);
            }
        }
    }
}