using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadBalancer
{
    public class Parser
    {
        private ParserStateMachine _parserStateMachine;
        
        public string RequestLine { get; private set; }

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public bool IsComplete { get; private set; }

        public byte[] MessageBytes { get; set; }

        public Parser()
        {
            _parserStateMachine = new ParserStateMachine();
        }
        
        public void Accept(byte[] bytes, int parseBytes)
        {
            // Ensure that we don't run off the end of the byte array.
            parseBytes = Math.Min(bytes.Length, Math.Abs(parseBytes));
            // Ensure that there is something to parse.
            if (parseBytes == 0)
            {
                return;
            }
            
            var offset = 0;

            if (_parserStateMachine.State == ParserState.Start)
            {
                _parserStateMachine.BeginReadingStatusLine();
                (RequestLine, offset) = ReadLine(bytes, parseBytes, offset);

                if (!_parserStateMachine.PartialLineEnding && !_parserStateMachine.PartialLineWithoutEnding)
                {
                    _parserStateMachine.BeginReadingHeaderLines();
                }
            }

            // Exhausted the input, return
            if (offset == parseBytes)
            {
                return;
            }

            if (_parserStateMachine.State == ParserState.ReadStatusLine)
            {
                if (_parserStateMachine.PartialLineEnding)
                {
                    if (bytes[offset].Equals((byte) '\n'))
                    {
                        offset++;
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid request: bad line ending");
                    }

                    _parserStateMachine.ClearPartialLineEnding();
                }
                else
                {
                    (RequestLine, offset) = ReadLine(bytes, parseBytes, offset);
                }
                
                if (!_parserStateMachine.PartialLineEnding && !_parserStateMachine.PartialLineWithoutEnding)
                {
                    _parserStateMachine.BeginReadingHeaderLines();
                }
            }

            // Exhausted the input, return
            if (offset == parseBytes)
            {
                return;
            }

            if (_parserStateMachine.State == ParserState.ReadHeaderLines)
            {
                string headerName;
                do
                {
                    if (_parserStateMachine.PartialLineEnding)
                    {
                        if (bytes[offset].Equals((byte) '\n'))
                        {
                            offset++;
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid request: bad line ending");
                        }

                        _parserStateMachine.ClearPartialLineEnding();
                    }       
                    
                    string headerValue;
                    (headerName, headerValue, offset) = ReadHeaderLine(bytes, parseBytes, offset);

                    if (headerName == null)
                    {
                        // Haven't got enough data to get the complete header yet.
                        break;
                    }

                    // Found empty line at the end of the headers block
                    if (headerName == "" && headerValue == "")
                    {
                        _parserStateMachine.BeginReadingBody();
                        break;
                    }
                    
                    Headers.Add(headerName, headerValue?.Trim());
                } while (headerName != "" && offset < parseBytes);
            }
            
            // Exhausted the input, return
            if (offset == parseBytes)
            {
                return;
            }

            if (_parserStateMachine.State == ParserState.ReadBody)
            {
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

        private (string line, int) ReadLine(byte[] bytes, int parseBytes, int begin)
        {
            var extractIndex = 0;
            var lineEndIndex = -1;
            var carriageReturn = false;
            var lineFeed = false;
            // Offset by +1 to look behind.
            for (var i = begin + 1; i < parseBytes; i++)
            {
                carriageReturn = bytes[i - 1].Equals((byte)'\r');
                lineFeed = bytes[i].Equals((byte) '\n');
                
                extractIndex++;
                if (!carriageReturn || !lineFeed) continue;

                lineEndIndex = i;
                break;
            }

            // Found partial end of line.
            if (carriageReturn && !lineFeed)
            {
                var partialLine = Encoding.ASCII.GetString(bytes.Skip(begin).Take(extractIndex - begin).ToArray());
                _parserStateMachine.SetPartialLineEnding();

                if (_parserStateMachine.PartialLineWithoutEnding)
                {
                    partialLine = _parserStateMachine.ClearPartialLineWithoutEnding() + partialLine;
                }
                return (partialLine, extractIndex + 1);
            }

            // No partial line ending, must be mid line.
            if (lineEndIndex == -1)
            {
                var partialLine = Encoding.ASCII.GetString(bytes.Skip(begin).Take(extractIndex + 1 - begin).ToArray());

                if (_parserStateMachine.PartialLineWithoutEnding)
                {
                    partialLine = _parserStateMachine.ClearPartialLineWithoutEnding() + partialLine;
                }
                _parserStateMachine.SetPartialLineWithoutEnding(partialLine);
                return (null, extractIndex + 1);
            }
            
            var line = Encoding.ASCII.GetString(bytes.Skip(begin).Take(lineEndIndex - 1 - begin).ToArray());
            if (_parserStateMachine.PartialLineWithoutEnding)
            {
                line = _parserStateMachine.ClearPartialLineWithoutEnding() + line;
            }
            return (line, extractIndex + 1);
        }
        
        private (string name, string value, int) ReadHeaderLine(byte[] bytes, int parseBytes, int begin)
        {
            var colonIndex = _parserStateMachine.ColonIndex ?? -1;

            var extractIndex = 0;
            var lineEndIndex = -1;
            var carriageReturn = false;
            var lineFeed = false;
            // Offset by +1 to look behind.
            for (var i = begin; i < parseBytes; i++)
            {
                if (colonIndex == -1 && bytes[i].Equals((byte)':'))
                {
                    colonIndex = i;
                    if (_parserStateMachine.PartialLineWithoutEnding)
                    {
                        var tmp = _parserStateMachine.ClearPartialLineWithoutEnding();
                        colonIndex += tmp.Length;
                        _parserStateMachine.SetPartialLineWithoutEnding(tmp);
                    }

                    _parserStateMachine.SetColonIdex(colonIndex);
                }
                
                carriageReturn = i > 0 && bytes[i - 1].Equals((byte)'\r');
                lineFeed = bytes[i].Equals((byte) '\n');
                
                extractIndex++;
                if (!carriageReturn || !lineFeed) continue;

                lineEndIndex = i;
                break;
            }

            string name;
            string value;
            
            // Found partial end of line.
            if (carriageReturn && !lineFeed)
            {
                var partialLine = Encoding.ASCII.GetString(bytes.Skip(begin).Take(extractIndex - begin).ToArray());
                _parserStateMachine.SetPartialLineEnding();

                if (_parserStateMachine.PartialLineWithoutEnding)
                {
                    partialLine = _parserStateMachine.ClearPartialLineWithoutEnding() + partialLine;
                }

                name = partialLine.Substring(0, colonIndex);
                value = partialLine.Substring(colonIndex + 1).Trim();
                _parserStateMachine.ClearColonIndex();
                return (name, value, extractIndex);
            }

            // No partial line ending, must be mid line.
            if (lineEndIndex == -1)
            {
                var partialLine = Encoding.ASCII.GetString(bytes.Skip(begin).Take(extractIndex + 1 - begin).ToArray());

                if (_parserStateMachine.PartialLineWithoutEnding)
                {
                    partialLine = _parserStateMachine.ClearPartialLineWithoutEnding() + partialLine;
                }
                _parserStateMachine.SetPartialLineWithoutEnding(partialLine);
                return (null, null, extractIndex);
            }
            
            var line = Encoding.ASCII.GetString(bytes.Skip(begin).Take(lineEndIndex - 1 - begin).ToArray());
            if (_parserStateMachine.PartialLineWithoutEnding)
            {
                line = _parserStateMachine.ClearPartialLineWithoutEnding() + line;
            }
            
            name = line.Substring(0, colonIndex);
            value = line.Substring(colonIndex + 1).Trim();
            _parserStateMachine.ClearColonIndex();
            return (name, value, extractIndex);
        }
        
        /*private (string name, string value, int) ReadHeaderLine(byte[] bytes, int parseBytes, int begin)
        {
            var colonIndex = -1;
            var lineEndIndex = -1;
            var extractIndex = 0;
            var carriageReturn = false;
            var lineFeed = false;
            // Offset by + 1 to look behind.
            for (var i = begin + 1; i < parseBytes; i++)
            {
                if (colonIndex == -1 && bytes[i - 1].Equals((byte)':'))
                {
                    colonIndex = i;
                }
                
                carriageReturn = bytes[i - 1].Equals((byte)'\r');
                lineFeed = bytes[i].Equals((byte) '\n');

                extractIndex++;
                if (!carriageReturn || !lineFeed) continue;
                
                lineEndIndex = i;
                break;
            }
            
            // Detect empty header name
            if (extractIndex == 2 && carriageReturn && lineFeed)
            {
                if (_parserStateMachine.PartialLineWithoutEnding)
                {
                    return (_parserStateMachine.ClearPartialHeaderName(),
                        _parserStateMachine.ClearPartialLineWithoutEnding(), extractIndex + 1);
                }
                
                return ("", "", lineEndIndex + 1);
            }

            if (carriageReturn && lineFeed)
            {
                string name;
                if (_parserStateMachine.HasHeaderName)
                {
                    name = _parserStateMachine.ClearHeaderName();
                }
                else
                {
                    name = Encoding.ASCII.GetString(bytes.Skip(begin).Take(colonIndex - 1 - begin).ToArray());
                    if (_parserStateMachine.PartialHeaderName)
                    {
                        name = _parserStateMachine.ClearPartialHeaderName() + name;
                    }
                }

                string value;
                if (colonIndex != -1)
                {
                    value = Encoding.ASCII.GetString(bytes.Skip(colonIndex).Take(lineEndIndex - 1 - colonIndex).ToArray());
                }
                else
                {
                    value = _parserStateMachine.ClearPartialLineWithoutEnding() + Encoding.ASCII.GetString(bytes.Skip(colonIndex).Take(lineEndIndex - 1 - begin).ToArray());
                }

                return (name, value, lineEndIndex + 1);
            }
            
            if (extractIndex + 1 == parseBytes)
            {
                // We've exhausted the provided bytes, figure out what state we're in.

                if (colonIndex == -1)
                {
                    var partialHeaderName = Encoding.ASCII.GetString(bytes.Skip(begin).Take(extractIndex + 1 - begin).ToArray());

                    if (_parserStateMachine.PartialHeaderName)
                    {
                        partialHeaderName = _parserStateMachine.ClearPartialHeaderName() + partialHeaderName;
                    }
                    _parserStateMachine.SetPartialHeaderName(partialHeaderName);
                    return (null, null, extractIndex + 1);
                }

                if (!_parserStateMachine.HasHeaderName)
                {
                    var headerName = Encoding.ASCII.GetString(bytes.Skip(begin).Take(colonIndex - 1 - begin).ToArray());
                    _parserStateMachine.SetHeaderName(headerName);
                    
                    return (null, null, extractIndex + 1);
                }

                if (!carriageReturn)
                {
                    var headerValue = Encoding.ASCII.GetString(bytes.Skip(colonIndex).Take(extractIndex + 1 - colonIndex).ToArray());

                    if (_parserStateMachine.PartialLineWithoutEnding)
                    {
                        headerValue = _parserStateMachine.ClearPartialLineWithoutEnding() + headerValue;
                    }
                    _parserStateMachine.SetPartialLineWithoutEnding(headerValue);
                    
                    return (null, null, extractIndex + 1);
                }

                if (!lineFeed)
                {
                    var headerValue = Encoding.ASCII.GetString(bytes.Skip(begin).Take(extractIndex - begin).ToArray());
                    _parserStateMachine.SetPartialLineEnding();

                    if (_parserStateMachine.PartialLineWithoutEnding)
                    {
                        headerValue = _parserStateMachine.ClearPartialLineWithoutEnding() + headerValue;
                    }

                    return (_parserStateMachine.ClearPartialHeaderName(), headerValue, extractIndex + 1);
                }
            }
            
            throw new InvalidOperationException("Failed to be caught in a case while extracting headers.");
        }*/
    }
}
