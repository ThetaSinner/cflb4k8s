using System;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace LoadBalancer.Test
{
    public class ParserTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Parser2StatusLine()
        {
            const string requestString = @"GET /hello.htm HTTP/1.1
";

            var request = Encoding.ASCII.GetBytes(requestString);

            var parser = new Parser2();
            const int chunkSize = 1;
            for (var i = 0; i < request.Length / chunkSize; i++)
            {
                var requestPart = request.AsEnumerable().Skip(i * chunkSize).Take(chunkSize).ToArray();
                parser.Accept(requestPart, requestPart.Length);
            }
            
            Assert.AreEqual(requestString.Trim(), parser.StatusLine);
        }
        
        [Test]
        public void Parser2Headers()
        {
            const string requestString = @"GET /hello.htm HTTP/1.1
User-Agent: Mozilla/4.0 (compatible; MSIE5.01; Windows NT)
Host: www.tutorialspoint.com
Accept-Language: en-us
Accept-Encoding: gzip, deflate
Connection: Keep-Alive

";

            var request = Encoding.ASCII.GetBytes(requestString);

            var parser = new Parser2();
            const int chunkSize = 1;
            for (var i = 0; i < request.Length / chunkSize; i++)
            {
                var requestPart = request.AsEnumerable().Skip(i * chunkSize).Take(chunkSize).ToArray();
                parser.Accept(requestPart, requestPart.Length);
            }
            
            Assert.AreEqual("GET /hello.htm HTTP/1.1", parser.StatusLine);
            var parserHeaders = parser.Headers;
            Assert.AreEqual(5, parserHeaders.Count);
        }
        
        [Test]
        public void SimpleGetRequest()
        {
            var request = Encoding.ASCII.GetBytes(@"GET /hello.htm HTTP/1.1
User-Agent: Mozilla/4.0 (compatible; MSIE5.01; Windows NT)
Host: www.tutorialspoint.com
Accept-Language: en-us
Accept-Encoding: gzip, deflate
Connection: Keep-Alive

");

            var parser = new Parser();
            for (var i = 0; i < request.Length; i++)
            {
                var requestPart = request.AsEnumerable().Skip(i * 5).Take(5).ToArray();
                parser.Accept(requestPart, requestPart.Length);
            }
            
            Assert.True(parser.IsComplete);
        }
        
        [Test]
        public void ChunkedTransfer()
        {
            var content = new byte[]
            {
                72, 84, 84, 80, 47, 49, 46, 49, 32, 50, 48, 48, 32, 79, 75, 13, 10, 68, 97, 116, 101, 58, 32, 83, 117,
                110, 44, 32, 49, 54, 32, 70, 101, 98, 32, 50, 48, 50, 48, 32, 48, 50, 58, 50, 57, 58, 51, 55, 32, 71,
                77, 84, 13, 10, 67, 111, 110, 116, 101, 110, 116, 45, 84, 121, 112, 101, 58, 32, 97, 112, 112, 108, 105,
                99, 97, 116, 105, 111, 110, 47, 106, 115, 111, 110, 59, 32, 99, 104, 97, 114, 115, 101, 116, 61, 117,
                116, 102, 45, 56, 13, 10, 83, 101, 114, 118, 101, 114, 58, 32, 75, 101, 115, 116, 114, 101, 108, 13, 10,
                84, 114, 97, 110, 115, 102, 101, 114, 45, 69, 110, 99, 111, 100, 105, 110, 103, 58, 32, 99, 104, 117,
                110, 107, 101, 100, 13, 10, 13, 10, 50, 48, 13, 10, 123, 34, 110, 97, 109, 101, 34, 58, 34, 109, 111,
                99, 107, 34, 44, 34, 115, 117, 109, 109, 97, 114, 121, 34, 58, 34, 109, 111, 99, 107, 34, 125, 13, 10,
                48, 13, 10, 13, 10
            };

            Console.WriteLine(Encoding.ASCII.GetString(content));

            var parser = new Parser();
            parser.Accept(content, content.Length);
            
            Assert.True(parser.IsComplete);
        }
    }
}