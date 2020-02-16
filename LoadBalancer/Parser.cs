using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadBalancer
{
    public class Parser
    {
        public string RequestLine { get; private set; }

        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        
        public bool IsComplete { get; private set; }
        
        public byte[] MessageBytes { get; set; }
        
        public void Accept(byte[] bytes, int parseBytes)
        {
            parseBytes = Math.Min(bytes.Length, parseBytes);
            Console.WriteLine($"Parsing {parseBytes} bytes");

            var offset = 0;
            (RequestLine, offset) = ReadLine(bytes, parseBytes, offset);

            string headerName;
            do
            {
                string headerValue;
                (headerName, headerValue, offset) = ReadHeaderLine(bytes, parseBytes, offset);
                
                Headers.Add(headerName, headerValue?.Trim());
            } while (headerName != "");
            
            if (offset == parseBytes)
            {
                Console.WriteLine("No body, request complete.");
                MessageBytes = bytes.AsEnumerable().Take(offset).ToArray();
                IsComplete = true;
                return;
            }

            if (Headers.TryGetValue("Transfer-Encoding", out var transferEncoding))
            {
                if (transferEncoding.Contains("chunked"))
                {
                    AcceptChunkedBody(bytes, offset, parseBytes);
                    return;
                }
            }
            
            // Should handle data being sent that goes beyond the header block but isn't body. Discard?
            if (!Headers.TryGetValue("Content-Length", out var contentLength))
            {
                throw new InvalidOperationException("Missing Content-Length header.");
            }

            var contentLengthValue = int.Parse(contentLength);
            if (offset + contentLengthValue < parseBytes)
            {
                // We've received the whole message!
                MessageBytes = bytes.AsEnumerable().Take(offset + contentLengthValue).ToArray();
                IsComplete = true;
            }
        }

        private void AcceptChunkedBody(byte[] bytes, int offset, int parseBytes)
        {
            var line = "";

            do
            {
                (line, offset) = ReadLine(bytes, parseBytes, offset);
                var chunkLength = int.Parse(line, System.Globalization.NumberStyles.HexNumber);

                offset += chunkLength + 2;

                if (chunkLength == 0)
                {
                    // We've received the whole message!
                    MessageBytes = bytes.AsEnumerable().Take(offset).ToArray();
                    IsComplete = true;
                    break;
                }
            } while (offset < parseBytes);
        }

        private static (string line, int) ReadLine(byte[] bytes, int parseBytes, int begin)
        {
            var lineEndIndex = -1;
            // Offset by + 1 to look behind.
            for (var i = begin + 1; i < parseBytes; i++)
            {
                var carriageReturn = bytes[i - 1].Equals((byte)'\r');
                var lineFeed = bytes[i].Equals((byte) '\n');

                if (!carriageReturn || !lineFeed) continue;
                
                lineEndIndex = i;
                break;
            }
            
            var line = Encoding.ASCII.GetString(bytes.Skip(begin).Take(lineEndIndex - 1 - begin).ToArray());
            return (line, lineEndIndex + 1);
        }
        
        private static (string name, string value, int) ReadHeaderLine(byte[] bytes, int parseBytes, int begin)
        {
            var colonIndex = -1;
            var lineEndIndex = -1;
            // Offset by + 1 to look behind.
            for (var i = begin + 1; i < parseBytes; i++)
            {
                if (colonIndex == -1 && bytes[i - 1].Equals((byte)':'))
                {
                    colonIndex = i;
                }
                
                var carriageReturn = bytes[i - 1].Equals((byte)'\r');
                var lineFeed = bytes[i].Equals((byte) '\n');
                
                if (!carriageReturn || !lineFeed) continue;
                
                lineEndIndex = i;
                break;
            }

            if (colonIndex == -1)
            {
                return ("", "", lineEndIndex + 1);
            }
            
            var name = Encoding.ASCII.GetString(bytes.Skip(begin).Take(colonIndex - 1 - begin).ToArray());
            var value = Encoding.ASCII.GetString(bytes.Skip(colonIndex).Take(lineEndIndex - 1 - colonIndex).ToArray());
            return (name, value, lineEndIndex + 1);
        }
    }
}