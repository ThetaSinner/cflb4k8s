using System;
using System.Threading;
using Cflb4K8S;
using Grpc.Core;

namespace MockIngressAgent
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var channel = new Channel("127.0.0.1:3301", ChannelCredentials.Insecure);
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
                    var ruleAck = client.PushRule("mocka", "https://mock-web-api:5001");

                    if (ruleAck.Accepted)
                    {
                        Console.WriteLine("Created rule for 'mocka'");
                    }
                }
                
                Thread.Sleep(5000);
            }
        }
    }
}