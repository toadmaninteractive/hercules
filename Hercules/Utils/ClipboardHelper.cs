using Json;
using System.Windows;

namespace Hercules
{
    public static class ClipboardHelper
    {
        public static void SetJson(ImmutableJson json)
        {
            Clipboard.SetText(json.ToString());
        }

        public static ImmutableJson? GetJson()
        {
            if (Clipboard.ContainsText())
            {
                try
                {
                    return JsonParser.Parse(Clipboard.GetText());
                }
                catch
                {
                    return null;
                }
            }
            else
                return null;
        }
    }
}
