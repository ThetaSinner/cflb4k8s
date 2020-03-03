using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cflb4K8S;
using LoadBalancer;
using LoadBalancerAgent.Nginx;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LoadBalancerAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var rules = new RoutingRules();
            rules.AddRule("singer", new Rule
            {
                Host = "artfactory.evelyn.internal",
                Name = "artifactory",
                Port = 443,
                Protocol = "https",
                Targets = { "https://node1.evelyn.internal:30092", "https://node1.evelyn.internal:30092" }
            });
            var render = new ConfigRenderer().Render(rules.GetRenderData());
            
            Console.WriteLine(render);

            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}