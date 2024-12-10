using Hercules.Shell;
using ICSharpCode.AvalonEdit.Document;
using Json;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class JsonSourceTab : PageTab
    {
        public DocumentEditorPage Editor { get; }

        public TextDocument JsonEditor { get; }
        public int? LastSourceLocation { get; set; }

        public event Action<int>? GoToSourceLocation;

        public SearchNotification Search { get; }

        private ISearchTarget? searchTarget;

        public ISearchTarget? SearchTarget
        {
            get => searchTarget;
            set => searchTarget = value;
        }

        private bool wordWrap;

        public bool WordWrap
        {
            get => wordWrap;
            set => SetField(ref wordWrap, value);
        }

        public ICommand WordWrapCommand { get; }

        public JsonSourceTab(DocumentEditorPage editor)
        {
            this.Editor = editor;
            this.JsonEditor = new TextDocument();
            Title = "Source";

            Search = new SearchNotification(RoutedCommands.FindNext);

            WordWrapCommand = Commands.Execute(() => WordWrap = !WordWrap);

            RoutedCommandBindings.Add(RoutedCommands.Find, Find);
            RoutedCommandBindings.Add(RoutedCommands.FindNext, FindNext, Search.HasContent);
            RoutedCommandBindings.Add(RoutedCommands.FindPrevious, FindPrevious, Search.HasContent);

            RoutedCommandBindings.Add(SubmitSourceCommand, SubmitSource);
            RoutedCommandBindings.Add(SyntaxCheckCommand, () => SyntaxCheck());
        }

        // TODO: submit source is broken when submitting invalid document key
        public static RoutedUICommand SubmitSourceCommand { get; } =
            new RoutedUICommand("Submit Source", "SubmitSource", typeof(JsonSourceTab), new InputGestureCollection { new KeyGesture(Key.Enter, ModifierKeys.Control) });

        public static RoutedUICommand SyntaxCheckCommand { get; } =
            new RoutedUICommand("Syntax Check", "SyntaxCheck", typeof(JsonSourceTab));

        public override void OnActivate()
        {
            SetJson(Editor.FormTab.DraftJson);
        }

        public override void OnDeactivate()
        {
            Notifications.Remove(Search);
        }

        public void GotoOffset(int location)
        {
            LastSourceLocation = location;
            GoToSourceLocation?.Invoke(location);
        }

        public void GoToPath(JsonPath path)
        {
            var location = GotoJsonPath.FindPosition(JsonEditor.Text, path);
            if (location != null)
                GotoOffset(location.Position);
        }

        void SubmitSource()
        {
            var json = SyntaxCheck();
            if (json != null)
            {
                Editor.UpdateData(json);
                Editor.GoTo(DestinationTab.Form);
            }
        }

        public ImmutableJsonObject? SyntaxCheck()
        {
            try
            {
                return JsonParser.Parse(JsonEditor.Text).AsObject;
            }
            catch (JsonParserException err)
            {
                Logger.LogException(err);
                GotoOffset(err.Position);
                return null;
            }
        }

        public void SetJson(ImmutableJsonObject json)
        {
            JsonEditor.Text = json.ToString(JsonFormat.Multiline);
        }

        void Find()
        {
            if (!Notifications.Items.Contains(Search))
                Notifications.Show(Search);
            Search.Activate();
        }

        void FindNext()
        {
            SearchTarget?.FindNext(Search.Text, SearchDirection.Next, Search.MatchCase, Search.WholeWord);
        }

        void FindPrevious()
        {
            SearchTarget?.FindNext(Search.Text, SearchDirection.Previous, Search.MatchCase, Search.WholeWord);
        }
    }
}