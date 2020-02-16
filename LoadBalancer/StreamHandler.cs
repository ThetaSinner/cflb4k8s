using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace LoadBalancer
{
    public static class StreamHandler
    {
        public static void Handle(RoutingRules rules, Stream stream)
        {
            var buffer = new byte[2048];
            var byteCount = -1;

            var parser = new Parser();

            while (byteCount != 0)
            {
                byteCount = stream.Read(buffer, 0, buffer.Length);
                
                parser.Accept(buffer, byteCount);

                if (parser.IsComplete) break;
            }
            
            Console.Write("In bytes: ");
            buffer.ToList().ForEach(b => Console.Write($"{b}, "));
            Console.WriteLine("");

            if (parser.Headers.TryGetValue("Host", out var hostHeader))
            {
                var target = rules.GetTarget(hostHeader);
                
                Console.WriteLine($"Forward request to {target}");

                var uri = new Uri(target);
                var client = new TcpClient(uri.Host, uri.Port);

                Console.Write("Send message: ");
                parser.MessageBytes.ToList().ForEach(b => Console.Write($"{b}, "));
                Console.WriteLine("");
                
                var networkStream = client.GetStream();
                networkStream.Write(parser.MessageBytes);
                
                buffer = new byte[2048];
                byteCount = -1;

                parser = new Parser();

                while (byteCount != 0)
                {
                    byteCount = networkStream.Read(buffer, 0, buffer.Length);
                
                    Console.Write("Out bytes: ");
                    buffer.ToList().ForEach(b => Console.Write($"{b}, "));
                    Console.WriteLine("");
                    
                    parser.Accept(buffer, byteCount);

                    Console.WriteLine(parser.RequestLine);

                    if (parser.IsComplete) break;
                }
                
                stream.Write(parser.MessageBytes);
                
                Console.WriteLine("Received response");
            }
        }
    }
}