using System;
using System.Linq;
using Cflb4K8S;
using Grpc.Core;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;

namespace IngressAgent
{
    internal static class Program
    {
        private static ConfigRemoteClient _client;
        private static Kubernetes _k8sClient;

        private static void Main(string[] args)
        {
            
            var appConfig = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var loadBalancerTarget = appConfig["LOAD_BALANCER_TARGET"];
            
            var channel = new Channel(loadBalancerTarget, ChannelCredentials.Insecure);
            _client = new ConfigRemoteClient(new ConfigRemote.ConfigRemoteClient(channel));
            
            var config = KubernetesClientConfiguration.IsInCluster()
                ? KubernetesClientConfiguration.InClusterConfig()
                : KubernetesClientConfiguration.BuildDefaultConfig();

            _k8sClient = new Kubernetes(config);
            
            Console.WriteLine($"Current context {config.CurrentContext}");
            Console.WriteLine($"Connecting to {_k8sClient.BaseUri}");
            
            var listNodeAsync = _k8sClient.ListNodeAsync();

            var nodesInfo = new NodesInfo(listNodeAsync.Result);

            var ingressLister = _k8sClient.ListIngressForAllNamespacesWithHttpMessagesAsync(watch: true);
            using (ingressLister.Watch<Extensionsv1beta1Ingress, Extensionsv1beta1IngressList>((type, item) =>
            {
                switch (type)
                {
                    case WatchEventType.Added:
                        HandleIngressAdded(item, nodesInfo);
                        break;
                    default:
                        Console.WriteLine($"Unhandled event for ingress {item.Metadata.Name}");
                        break;
                }
            }))
            {
                Console.WriteLine("Watching for ingress changes...");
                Console.ReadLine();
            }
        }

        private static void HandleIngressAdded(Extensionsv1beta1Ingress item, NodesInfo nodesInfo)
        {
            item.Spec.Rules.ToList().ForEach(rule =>
            {
                rule.Http.Paths.ToList().ForEach(path =>
                {
                    var targetPort = LookupPort(path, item.Metadata.NamespaceProperty);
                    
                    var addRule = new Rule
                    {
                        Name = item.Metadata.Name,
                        Host = rule.Host,
                        Port = 443,
                        Protocol = "https" // look up from annotation
                    };
                    
                    addRule.Targets.AddRange(nodesInfo.NodeAddresses.Select(nodeAddress => $"{nodeAddress}:{targetPort}"));

                    try
                    {
                        var pushRule = _client.PushRule(addRule);
                        Console.WriteLine($"Rule pushed {pushRule.Accepted}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                });
            });
        }

        private static int LookupPort(Extensionsv1beta1HTTPIngressPath path, string namespaceProperty)
        {
            var portNameOrValue = path.Backend.ServicePort.Value;
            if (int.TryParse(portNameOrValue, out var port))
            {
                return port;
            }
            
            var serviceWithHttpMessagesAsync = _k8sClient.ReadNamespacedServiceWithHttpMessagesAsync(path.Backend.ServiceName, namespaceProperty);
            
            var service = serviceWithHttpMessagesAsync.Result.Body;
            if (service.Spec.Type == "NodePort")
            {
                var servicePorts = service.Spec.Ports.Where(servicePort => portNameOrValue.Equals(servicePort.Name)).ToList();

                if (servicePorts.Count != 1)
                {
                    throw new InvalidOperationException("Unrecognised service spec!");
                }

                var nodePort = servicePorts.First().NodePort;
                if (nodePort != null) return (int) nodePort;
            }
            
            throw new InvalidOperationException("Unable to lookup port!");
        }
    }
}
