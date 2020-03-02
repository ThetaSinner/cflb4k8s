using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LoadBalancer
{
    public class Parser2
    {
        private readonly List<byte> _payload = new List<byte>();

        private int _processIndex;
        private bool _resumeFlag;
        private bool _carriageReturn;
        private bool _lineFeed;
        private bool _endOfHeaders;
        private int _statusLineIndex = -1;
        
        private List<int> _headerIndices = new List<int>();
        private List<int> _headerSeparatorIndex = new List<int>();

        public string StatusLine => Encoding.ASCII.GetString(_payload.Take(_statusLineIndex).ToArray());

        public Dictionary<string, string> Headers
        {
            get
            {
                var result = new Dictionary<string, string>();
                
                for (var i = 0; i < _headerIndices.Count; i += 2)
                {
                    var name = Encoding.ASCII.GetString(_payload
                        .Skip(_headerIndices[i])
                        .Take(_headerSeparatorIndex[i] - _headerIndices[i])
                        .ToArray()
                    ).Trim();
                    
                    var value = Encoding.ASCII.GetString(_payload
                        .Skip(_headerSeparatorIndex[i] + 1)
                        .Take(_headerIndices[i + 1] - _headerSeparatorIndex[i])
                        .ToArray()
                    ).Trim();
                    
                    result.Add(name, value);
                }

                return result;
            }
        }
        
        public void Accept(IEnumerable<byte> bytes, int parseBytes)
        {
            _payload.AddRange(bytes.AsEnumerable().Take(parseBytes));

            _resumeFlag = true;
            
            RunParse();
        }

        private void RunParse()
        {
            if (_processIndex >= _payload.Count)
            {
                return;
            }

            if (_statusLineIndex == -1)
            {
                ParseStatusLine();
            }
            else if (!_endOfHeaders)
            {
                ParseHeaders();                
            }
        }

        private void ParseStatusLine()
        {
            while (_processIndex < _payload.Count)
            {
                if (!(_resumeFlag && _carriageReturn) && _processIndex - 1 >= 0)
                {
                    _carriageReturn = _payload[_processIndex - 1].Equals((byte) '\r');
                }

                _lineFeed = _payload[_processIndex].Equals((byte) '\n');

                if (_carriageReturn && _lineFeed)
                {
                    _statusLineIndex = _processIndex - 1;
                    break;
                }

                _resumeFlag = false;
                _processIndex++;
            }

            if (_statusLineIndex != -1)
            {
                _resumeFlag = false;
                _carriageReturn = false;
                _lineFeed = false;
                _processIndex++;
            }
            
            RunParse();
        }
        
        private void ParseHeaders()
        {
            if (_headerIndices.Count % 2 == 0)
            {
                _headerIndices.Add(_processIndex);
            }
            
            while (_processIndex < _payload.Count)
            {
                // Haven't found a separator for this header yet.
                if (_headerSeparatorIndex.Count < _headerIndices.Count / 2.0 && _payload[_processIndex].Equals((byte) ':'))
                {
                    _headerSeparatorIndex.Add(_processIndex);
                }
                
                if (!(_resumeFlag && _carriageReturn) && _processIndex - 1 >= 0)
                {
                    _carriageReturn = _payload[_processIndex - 1].Equals((byte) '\r');
                }

                _lineFeed = _payload[_processIndex].Equals((byte) '\n');

                if (_carriageReturn && _lineFeed)
                {
                    _headerIndices.Add(_processIndex - 1);
                    break;
                }

                _resumeFlag = false;
                _processIndex++;
            }

            if (_headerIndices.Count % 2 == 0)
            {
                _resumeFlag = false;
                _carriageReturn = false;
                _lineFeed = false;
                _processIndex++;

                if (_headerIndices[^1] == _headerIndices[^2])
                {
                    _endOfHeaders = true;
                }
            }
            
            RunParse();
        }
    }
}