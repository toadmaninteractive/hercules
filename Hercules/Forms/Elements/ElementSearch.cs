using System.Collections.Generic;
using System.Linq;

namespace Hercules.Forms.Elements
{
    public class ElementSearch
    {
        public string Text { get; init; } = string.Empty;
        public bool MatchCase { get; init; }
        public bool WholeWord { get; init; }

        public Element? Current { get; set; }
        public List<Element> Before { get; } = new();
        public List<Element> After { get; } = new();

        bool afterCurrent;
        double? number;

        public void Search(Element element)
        {
            afterCurrent = false;
            number = Numbers.ParseDouble(Text);
            element.Visit(Visitor, VisitOptions.None);
        }

        void Visitor(Element element)
        {
            if (element == Current)
            {
                afterCurrent = true;
                return;
            }

            bool result = element switch
            {
                Field field => MatchString(field.Caption),
                SimpleElement<string?> stringElement => stringElement.Value != null && MatchString(stringElement.Value),
                SimpleElement<double?> doubleElement => number.HasValue && doubleElement.Value.HasValue && Numbers.Compare(number.Value, doubleElement.Value.Value),
                SimpleElement<int?> intElement => number.HasValue && intElement.Value.HasValue && Numbers.Compare(number.Value, intElement.Value.Value),
                SimpleElement<bool> boolElement => MatchString(boolElement.Value ? "true" : "false"),
                MultiSelectElement multiSelectElement => multiSelectElement.Value?.Any(MatchString) ?? false,
                _ => false,
            };
            if (result)
            {
                if (afterCurrent)
                    After.Add(element);
                else
                    Before.Add(element);
            }
        }

        bool MatchString(string value)
        {
            return SearchHelper.MatchString(value, Text, MatchCase, WholeWord);
        }
    }
}
