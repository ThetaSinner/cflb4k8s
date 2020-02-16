using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

namespace LoadBalancer
{
    public class HttpLoadBalancer
    {
        public static void Run(object data)
        {
            Console.WriteLine("Starting HTTP load balancer.");
            
            var loadBalancerConfig = (LoadBalancerConfig) data;
            
            var local = IPAddress.Parse(loadBalancerConfig.Configuration.GetValue<string>("HttpBindHost"));
            var server = new TcpListener(local, loadBalancerConfig.Configuration.GetValue<int>("HttpBindPort"));
            
            server.Start();

            while (true)
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    AcceptClient(client, loadBalancerConfig.RoutingRules);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        
        private static void AcceptClient(TcpClient client, RoutingRules rules)
        {
            var stream = client.GetStream();
            
            var buffer = new byte[2048];
            var byteCount = -1;

            var parser = new Parser();

            while (byteCount != 0)
            {
                byteCount = stream.Read(buffer, 0, buffer.Length);
                
                parser.Accept(buffer, byteCount);

                if (parser.IsComplete) break;
            }
            
            parser.Headers.ToList().ForEach(pair =>
            {
                Console.WriteLine(pair.Key);
                Console.WriteLine(pair.Value);
            });
            
            if (parser.Headers.TryGetValue("Host", out var hostHeader))
            {
                var target = rules.GetTarget(hostHeader);
                
                Console.WriteLine($"Forward request to {target}");
            }

            stream.Close();

            // Process the connection here. (Add the client to a
            // server table, read data, etc.)
            Console.WriteLine("Client connected completed");
        }
    }
}