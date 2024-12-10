using Hercules.Forms;
using Hercules.Forms.Elements;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Hercules.Shortcuts;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class DocumentFormTab : PageTab, ICommandContext
    {
        public static RoutedUICommand AddCustomFieldCommand { get; } =
            new("Add Custom Field", "AddCustomField", typeof(DocumentFormTab), new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Alt) });

        public ICommand<Element> GoToJsonCommand { get; }
        public ICommand<Element> ClearCommand { get; }
        public ICommand<Element> CollapseSelectionCommand { get; }
        public ICommand<Element> CopyCommand { get; }
        public ICommand<Element> CopyFullPathCommand { get; }
        public ICommand<Element> CopyPathCommand { get; }
        public ICommand<Element> DuplicateItemCommand { get; }
        public ICommand<Element> ExpandSelectionCommand { get; }
        public ICommand OptionalFieldVisibilityChangeCommand { get; }
        public ICommand<Element> PasteChildCommand { get; }
        public ICommand<Element> PasteCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand RemoveAllCustomFieldsCommand { get; }
        public ICommand SummaryTableCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RevertToBaseCommand { get; }
        public ICommand RevertToOriginalCommand { get; }
        public ICommand SortCommand { get; }

        public DocumentForm Form { get; }
        public FormPresentation Presentation { get; }
        public DocumentEditorPage Editor { get; }
        public ImmutableJsonObject DraftJson => Form.Json;

        private readonly SearchNotification searchViewModel;

        public DocumentFormTab(DocumentEditorPage editor, FormSettings formSettings, IElementFactory elementFactory, ImmutableJsonObject json, ImmutableJsonObject originalJson, SchemaRecord schemaRecord)
        {
            this.Editor = editor;
            this.Title = "Form";

            Form = new DocumentForm(json, originalJson, schemaRecord, elementFactory, formSettings);
            Presentation = new FormPresentation(Form);
            Presentation.RefreshPresentation();

            searchViewModel = new SearchNotification(RoutedCommands.FindNext);

            RoutedCommandBindings.Add(RoutedCommands.Find, Find);
            RoutedCommandBindings.Add(RoutedCommands.FindNext, FindNext, searchViewModel.HasContent);
            RoutedCommandBindings.Add(RoutedCommands.FindPrevious, FindPrevious, searchViewModel.HasContent);

            GoToJsonCommand = Commands.Execute<Element>(GoToJson).IfNotNull();
            UndoCommand = Commands.Execute(() => Form.Undo()).If(() => Form.History.CanUndo);
            RedoCommand = Commands.Execute(() => Form.Redo()).If(() => Form.History.CanRedo);
            ExpandSelectionCommand = Commands.Execute<Element>(element => element.ExpandSubtree()).IfNotNull();
            CollapseSelectionCommand = Commands.Execute<Element>(element => element.CollapseSubtree()).IfNotNull();
            CopyPathCommand = Commands.Execute<Element>(element => Clipboard.SetText(element.Path.ToString())).IfNotNull();
            CopyFullPathCommand = Commands.Execute<Element>(element => Clipboard.SetText(Editor.Document.DocumentId + "." + element.Path.ToString())).IfNotNull();
            CopyCommand = Commands.Execute<Element>(element => ClipboardHelper.SetJson(element.Json)).IfNotNull();
            PasteCommand = Commands.Execute<Element>(Paste).IfNotNull();
            PasteChildCommand = Commands.Execute<Element>(PasteChild).If(element => element is IPasteChildElement && (ClipboardHelper.GetJson() != null));
            DuplicateItemCommand = Commands.Execute<Element>(element => element.GetParentByType<IDuplicateElement>()!.Duplicate())
                .If(element => (element != null) && (element is IDuplicateElement || element.Parent is IDuplicateElement));
            ClearCommand = Commands.Execute<Element>(element => ((IClearElement)element).Clear()).If(element => element is IClearElement);
            RemoveAllCustomFieldsCommand = Commands.Execute(RemoveAllCustomFields);
            SummaryTableCommand = Commands.Execute(SummaryTable).If(() => Editor.Category.IsSchemaful);
            RevertToBaseCommand = Commands.Execute(() => RevertToBase(Presentation.SelectedElement!)).If(() => Presentation.SelectedElement != null && Editor.PatchHandler.IsPatch && !Presentation.SelectedElement.IsInherited);
            RevertToOriginalCommand = Commands.Execute(() => RevertToOriginal(Presentation.SelectedElement!)).If(() => Presentation.SelectedElement != null && Presentation.SelectedElement.IsModified);
            SortCommand = Commands.Execute(() => ((ISortableElement)Presentation.SelectedElement!).Sort()).If(() => Presentation.SelectedElement is ISortableElement { CanSort: true });

            OptionalFieldVisibilityChangeCommand = Commands.Execute(() => Form.IsOptionFieldsVisible = !Form.IsOptionFieldsVisible);

            RoutedCommandBindings.Add(ApplicationCommands.Undo, UndoCommand);
            RoutedCommandBindings.Add(ApplicationCommands.Redo, RedoCommand);
            RoutedCommandBindings.Add(RoutedCommands.ExpandAll, () => Form.Root.ExpandSubtree());
            RoutedCommandBindings.Add(RoutedCommands.CollapseAll, () => Form.Root.CollapseSubtree());
            RoutedCommandBindings.Add(RoutedCommands.ExpandSelection, ExpandSelectionCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.CollapseSelection, CollapseSelectionCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.CopyPath, CopyPathCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.CopyFullPath, CopyFullPathCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.GoToJson, GoToJsonCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.DuplicateItem, DuplicateItemCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.Clear, ClearCommand, this);
            RoutedCommandBindings.Add(ApplicationCommands.Copy, CopyCommand, this);
            RoutedCommandBindings.Add(ApplicationCommands.Paste, PasteCommand, this);
            RoutedCommandBindings.Add(RoutedCommands.PasteChild, PasteChildCommand, this);
            RoutedCommandBindings.Add(AddCustomFieldCommand, ShowAddCustomFieldNotification);
        }

        public IReadOnlyList<string> GetErrorList()
        {
            List<string> errors = new List<string>();
            foreach (var entry in Form.Root.Record.Children)
            {
                if (!entry.IsValid)
                    errors.Add($"Invalid value for element <{entry.Name}>");
            }
            return errors;
        }

        public void GoToPath(JsonPath path)
        {
            var element = Form.ElementByPath(path);
            IContainer? container = element.Parent;
            Form.Run(transaction =>
            {
                while (container != null)
                {
                    if (container is IExpandableElement expandableElement)
                        expandableElement.Expand(true, transaction);
                    if (container is LocalizedElement localizedElement && element != localizedElement.Text)
                        localizedElement.RecordView = true;
                    if (container is Element el)
                        container = el.Parent;
                    else
                        container = null;
                }
            });
            Presentation.SelectElement(element);
        }

        public override void OnDeactivate()
        {
            Notifications.Remove(searchViewModel);
        }

        public void RunAndLogWarnings(Action<ITransaction> fun)
        {
            IReadOnlyList<Warning> warnings = Array.Empty<Warning>();
            Form.Run(transaction =>
            {
                fun(transaction);
                warnings = transaction.Warnings;
            });
            if (warnings.Any())
            {
                var documentId = Editor.Document.DocumentId;
                var shortcut = new DocumentShortcut(documentId);
                foreach (var warning in warnings)
                    Logger.LogWarning($"{documentId}: {warning.Text} at {warning.Path}", shortcut);
            }
        }

        public void UpdateJson(ImmutableJsonObject json)
        {
            RunAndLogWarnings(transaction =>
            {
                Form.Root.SetJson(json, transaction);
            });
        }

        void Find()
        {
            if (!Notifications.Items.Contains(searchViewModel))
                Notifications.Show(searchViewModel);
            searchViewModel.Activate();
        }

        void FindNext()
        {
            var search = new ElementSearch { MatchCase = searchViewModel.MatchCase, WholeWord = searchViewModel.WholeWord, Text = searchViewModel.Text, Current = Presentation.SelectedElement };
            search.Search(Form.Root);
            Element? result = null;
            if (search.After.Any())
                result = search.After.First();
            else if (search.Before.Any())
                result = search.Before.First();
            if (result != null)
                GoToPath(result.Path);
        }

        void FindPrevious()
        {
            var search = new ElementSearch { MatchCase = searchViewModel.MatchCase, WholeWord = searchViewModel.WholeWord, Text = searchViewModel.Text, Current = Presentation.SelectedElement };
            search.Search(Form.Root);
            Element? result = null;
            if (search.Before.Any())
                result = search.Before.Last();
            else if (search.After.Any())
                result = search.After.Last();
            if (result != null)
                GoToPath(result.Path);
        }

        void GoToJson(Element element)
        {
            var path = element.Path;
            Editor.GoTo(DestinationTab.Source, path);
        }

        void Paste(Element element)
        {
            var json = ClipboardHelper.GetJson();
            if (json != null)
            {
                RunAndLogWarnings(transaction => element.SetJson(json, transaction));
            }
        }

        void PasteChild(Element element)
        {
            var json = ClipboardHelper.GetJson();
            if (json != null)
            {
                RunAndLogWarnings(transaction => ((IPasteChildElement)element).PasteElement(json, transaction));
            }
        }

        void RemoveAllCustomFields()
        {
            Form.Run(transaction =>
                {
                    foreach (var field in Form.Root.SchemalessFields.ToList())
                        Form.Root.RemoveSchemalessField(field, transaction);
                });
        }

        void ShowAddCustomFieldNotification()
        {
            if (!Notifications.Items.OfType<AddCustomFieldNotification>().Any())
                Notifications.Show(new AddCustomFieldNotification(this));
        }

        void SummaryTable()
        {
            Editor.SummaryTable(Presentation.SelectedElement?.Path);
        }

        private void RevertToBase(Element element)
        {
            Editor.PatchHandler.RevertToBase(element);
        }

        private void RevertToOriginal(Element element)
        {
            Form.Run(transaction => element.SetJson(element.OriginalJson, transaction));
        }

        public object? GetCommandParameter(Type type)
        {
            return type switch
            {
                _ when type == typeof(Element) => Presentation.SelectedElement,
                _ when type == typeof(VirtualRowItem) => Presentation.SelectedItem,
                _ => null
            };
        }
    }
}