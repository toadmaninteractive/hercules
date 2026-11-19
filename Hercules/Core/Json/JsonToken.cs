namespace Json
{
    public enum JsonToken
    {
        Null,
        True,
        False,

        String,
        Number,

        ObjectStart, // {
        ObjectEnd, // }

        ArrayStart, // [
        ArrayEnd, // ]
        Comma, // ,
        KeyValueSeparator, // :
    }
}
