namespace LoadBalancer
{
    public enum ParserState
    {
        Start,
        ReadStatusLine,
        ReadHeaderLines,
        ReadBody,
        End
    }
}