using Hercules.Forms.Schema;
using Json;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Forms.Elements
{
    public class ReplicaListItem : ListItem
    {
        public Element SpeakerValue => this.GetByPath(new JsonPath("speaker")) as StringElement;

        public RecordElement RecordElement => this.Element as RecordElement;

        public StringElement RefValue => this.GetByPath(new JsonPath("ref")) as StringElement;

        public TextElement TextValue => this.GetByPath(new JsonPath("text")) as TextElement;

        public ListElement AnswerList => (ListElement)this.GetByPath(new JsonPath("answers"));

        public IEnumerable<ListItem> AnswersElements => ((ListElement)this.GetByPath(new JsonPath("answers"))).Children.Cast<ListItem>();

        public List<string> AnswerIds
        {
            get
            {
                var listElement = (ListElement)this.GetByPath(new JsonPath("answers"));
                return listElement.Children.Cast<ListItem>().
                            Select(x => ((RefElement)((RecordElement)x.DeepElement).
                                Children.First(f => f.Name == "ref").DeepElement).Value).ToList();
            }
        }

        private List<string> OldAnswerIds;

        public ReplicaListItem(ListElement parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, int index, int? originalIndex, ITransaction transaction)
            : base(parent, type, json, originalJson, index, originalIndex, transaction)
        {
            OldAnswerIds = new List<string>();
        }

        public override void ChildChanged(ITransaction transaction)
        {
            base.ChildChanged(transaction);

            if (IsValid && !AnswerIds.SequenceEqual(OldAnswerIds))
            {
                OldAnswerIds = AnswerIds;
                RaisePropertyChanged(nameof(AnswerIds));
            }
        }
    }
}
