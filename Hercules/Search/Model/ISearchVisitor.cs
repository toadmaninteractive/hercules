using Json;

namespace Hercules.Search
{
    public enum SearchDataType
    {
        Text,
        Number,
        Enum,
        Key,
        Field,
    }

    public interface ISearchVisitor
    {
        void VisitPath(JsonPath path, SearchDataType type, string data);
    }
}
