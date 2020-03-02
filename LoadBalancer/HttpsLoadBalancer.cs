using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace LoadBalancer
{
    public static class HttpsLoadBalancer
    {
        public static void Run(object data)
        {
            Console.WriteLine("Starting HTTPS load balancer.");
            
            var loadBalancerConfig = (LoadBalancerConfig) data;
            
            var local = IPAddress.Parse(loadBalancerConfig.Configuration.GetValue<string>("HttpsBindHost"));
            var server = new TcpListener(local, loadBalancerConfig.Configuration.GetValue<int>("HttpsBindPort"));

            // openssl pkcs12 -export -out server.p12 -in cert.pem -inkey key.pem
            var certificate = X509Certificate.CreateFromCertFile("./server.p12");

            server.Start();

            while (true)
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    AcceptClient(client, certificate, loadBalancerConfig.RoutingRules);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        
        private static void AcceptClient(TcpClient client, X509Certificate certificate, RoutingRules rules)
        {
            var stream = client.GetStream();
            var sslStream = new SslStream(stream, false);

            sslStream.AuthenticateAsServer(certificate, clientCertificateRequired: false,
                checkCertificateRevocation: false);

            if (!sslStream.IsAuthenticated)
            {
                Console.WriteLine("Not authenticated.");
                sslStream.Close();
                return;
            }

            DisplaySecurityLevel(sslStream);
            DisplaySecurityServices(sslStream);
            DisplayCertificateInformation(sslStream);
            DisplayStreamProperties(sslStream);

            sslStream.ReadTimeout = 15000;
            sslStream.WriteTimeout = 15000;

            StreamHandler.Handle(rules, stream);

            sslStream.Close();

            // Process the connection here. (Add the client to a
            // server table, read data, etc.)
            Console.WriteLine("Client connected completed");
        }

        private static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm,
                stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }

        private static void DisplaySecurityServices(AuthenticatedStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }

        private static void DisplayStreamProperties(Stream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }

        private static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            var localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }

            // Display the properties of the client's certificate.
            var remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }
    }
}