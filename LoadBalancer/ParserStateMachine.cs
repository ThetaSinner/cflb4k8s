using System.Data;
using System.Diagnostics;

namespace LoadBalancer
{
    public class ParserStateMachine
    {
        public ParserState State { get; private set; }
        
        public bool PartialLineEnding { get; private set; }
        public bool PartialLineWithoutEnding { get; private set; }
        
        public bool PartialHeaderName { get; private set; }
        public bool HasHeaderName { get; private set; }
        
        public int? ColonIndex { get; private set; }
        
        private string partialLine;
        private string partialHeaderName;
        private string headerName;

        public ParserStateMachine()
        {
            State = ParserState.Start;
        }

        public void BeginReadingStatusLine()
        {
            Debug.Assert(State == ParserState.Start);
            State = ParserState.ReadStatusLine;
        }

        public void SetPartialLineEnding()
        {
            PartialLineEnding = true;
        }

        public void SetPartialLineWithoutEnding(string partialLine)
        {
            this.partialLine = partialLine;
            PartialLineWithoutEnding = true;
        }

        public string ClearPartialLineWithoutEnding()
        {
            PartialLineWithoutEnding = false;
            var tmp = partialLine;
            partialLine = null;
            return tmp;
        }

        public void ClearPartialLineEnding()
        {
            PartialLineEnding = false;
        }

        public void BeginReadingHeaderLines()
        {
            State = ParserState.ReadHeaderLines;
        }

        public void SetPartialHeaderName(string partialHeaderName)
        {
            PartialHeaderName = true;
            this.partialHeaderName = partialHeaderName;
        }

        public string ClearPartialHeaderName()
        {
            PartialHeaderName = false;
            var tmp = partialHeaderName;
            partialHeaderName = null;
            return tmp;
        }

        public void SetHeaderName(string headerName)
        {
            HasHeaderName = true;
            this.headerName = headerName;
        }

        public string ClearHeaderName()
        {
            HasHeaderName = false;
            var tmp = headerName;
            headerName = null;
            return tmp;
        }

        public void BeginReadingBody()
        {
            State = ParserState.ReadBody;
        }

        public void SetColonIdex(in int colonIndex)
        {
            ColonIndex = colonIndex;
        }

        public void ClearColonIndex()
        {
            ColonIndex = null;
        }
    }
}