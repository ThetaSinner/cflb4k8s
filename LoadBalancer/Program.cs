using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Cflb4K8S;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace LoadBalancer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();

            var routingRules = new RoutingRules();
            StartConfigServer(config, routingRules);

            var lbConfig = new LoadBalancerConfig
            {
                Configuration = config,
                RoutingRules = routingRules
            };
            
            if (config.GetValue<bool>("HttpsEnabled"))
            {
                var httpsThread = new Thread(HttpsLoadBalancer.Run);
                httpsThread.Start(lbConfig);
            }
            
            var httpThread = new Thread(HttpLoadBalancer.Run);
            httpThread.Start(lbConfig);
        }

        private static void StartConfigServer(IConfiguration config, RoutingRules routingRules)
        {
            var configServer = new Server()
            {
                Services = {ConfigRemote.BindService(new ConfigRemoteImpl(routingRules))},
                Ports = {new ServerPort(
                    config.GetValue<string>("ConfigServerHost"), 
                    config.GetValue<int>("ConfigServerPort"), 
                    ServerCredentials.Insecure)}
            };
            
            configServer.Start();
        }
    }
}