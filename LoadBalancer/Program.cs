using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cflb4K8S;
using Grpc.Core;

namespace LoadBalancer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var local = IPAddress.Parse("127.0.0.1");
            var server = new TcpListener(local, 443);

            // openssl pkcs12 -export -out server.p12 -in cert.pem -inkey key.pem
            var certificate = X509Certificate.CreateFromCertFile("./server.p12");

            var rules = new RoutingRules();
            
            var configServer = new Server()
            {
                Services = {ConfigRemote.BindService(new ConfigRemoteImpl(rules))},
                Ports = {new ServerPort("localhost", 3301, ServerCredentials.Insecure)}
            };
            configServer.Start();
            
            server.Start();

            while (true)
            {
                var client = server.AcceptTcpClient();
                AcceptClient(client, certificate, rules);
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

            var buffer = new byte[2048];
            var messageData = new StringBuilder();
            var byteCount = -1;

            while (byteCount != 0)
            {
                byteCount = sslStream.Read(buffer, 0, buffer.Length);

                Console.WriteLine($"Bytes read {byteCount}.");

                var decoder = Encoding.UTF8.GetDecoder();
                var chars = new char[decoder.GetCharCount(buffer, 0, byteCount)];
                decoder.GetChars(buffer, 0, byteCount, chars, 0);
                messageData.Append(chars);

                Console.WriteLine(messageData.ToString());
            }

            Console.WriteLine(messageData.ToString());

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