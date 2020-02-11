using System;
using System.Linq;
using k8s;
using k8s.Models;

namespace IngressAgent
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var config = KubernetesClientConfiguration.IsInCluster()
                ? KubernetesClientConfiguration.InClusterConfig()
                : KubernetesClientConfiguration.BuildDefaultConfig();

            IKubernetes client = new Kubernetes(config);
            
            Console.WriteLine($"Current context {config.CurrentContext}");
            Console.WriteLine($"Connecting to {client.BaseUri}");
            
            var listNodeAsync = client.ListNodeAsync();
            
            listNodeAsync.Result.Items.ToList().ForEach(node => Console.WriteLine(node.Metadata.Name));

            var ingressLister = client.ListIngressForAllNamespacesWithHttpMessagesAsync(watch: true);
            using (ingressLister.Watch<Extensionsv1beta1Ingress, Extensionsv1beta1IngressList>((type, item) =>
            {
                Console.WriteLine(type);
                Console.WriteLine(item.Metadata.Name);
            }))
            {
                Console.WriteLine("Watching for ingress changes...");
                Console.ReadLine();
            }
        }
    }
}
