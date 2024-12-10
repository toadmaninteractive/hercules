using Hercules.Controls;
using Hercules.Forms.Elements;
using Hercules.Forms.Presentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class ElementPresentation : FormPresentation
    {
        public ElementPresentation(DocumentForm form) : base(form)
        {
        }

        public IReadOnlyList<Field>? Fields { get; set; }

        protected override void Present(PresentationContext context)
        {
            if (Fields != null)
            {
                foreach (var field in Fields)
                {
                    field.Present(context);
                }
            }
        }

        public override void CreateRowPanel(out Panel rootPanel, out Panel defaultPanel)
        {
            rootPanel = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) },
                    new ColumnDefinition { Width = new GridLength(30, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(70, GridUnitType.Star) }
                }
            };
            defaultPanel = (Panel)HorizontalDockPanelPool.Acquire();
            Debug.Assert(defaultPanel.Children.Count == 0);
            defaultPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            defaultPanel.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 0, 0));
            Grid.SetColumn(defaultPanel, 2);
            rootPanel.Children.Add(defaultPanel);
        }

        public override void ReleaseRowPanel(Panel rootPanel, Panel defaultPanel)
        {
            Debug.Assert(rootPanel is Grid);
            Debug.Assert(defaultPanel is HorizontalDockPanel);
            Debug.Assert(rootPanel.Children.Count == 1);
            rootPanel.Children.Remove(defaultPanel);
            Debug.Assert(rootPanel.Children.Count == 0);
            defaultPanel.ClearValue(Grid.ColumnProperty);
            defaultPanel.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
            defaultPanel.ClearValue(FrameworkElement.MarginProperty);
            Debug.Assert(defaultPanel.Children.Count == 0);
            HorizontalDockPanelPool.Release(defaultPanel);
        }
    }

    public class ElementProperties : NotifyPropertyChanged, ICommandContext
    {
        public DocumentEditorPage DocumentEditor { get; }

        public CommandBindingCollection RoutedCommandBindings { get; }

        public ElementPresentation Presentation { get; }

        public void SetFields(IEnumerable<Field>? fields)
        {
            Presentation.Fields = fields?.Where(f => f.IsVisibleInPropertyEditor).ToList();
            Presentation.RefreshPresentation();
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

        public ElementProperties(DocumentEditorPage page, IEnumerable<Field>? fields)
        {
            this.DocumentEditor = page;
            Presentation = new ElementPresentation(page.FormTab.Form);
            SetFields(fields);
            RoutedCommandBindings = new CommandBindingCollection
            {
                {RoutedCommands.GoToJson, DocumentEditor.FormTab.GoToJsonCommand, this},
                {RoutedCommands.GoToForm, () => DocumentEditor.GoTo(DestinationTab.Form, Presentation.SelectedElement!.Path), () => Presentation.SelectedElement != null},
                {ApplicationCommands.Copy, DocumentEditor.FormTab.CopyCommand, this},
                {ApplicationCommands.Paste, DocumentEditor.FormTab.PasteCommand, this},
                {ApplicationCommands.Undo, DocumentEditor.FormTab.UndoCommand},
                {ApplicationCommands.Redo, DocumentEditor.FormTab.RedoCommand},
                {RoutedCommands.ExpandSelection, DocumentEditor.FormTab.ExpandSelectionCommand, this},
                {RoutedCommands.CollapseSelection, DocumentEditor.FormTab.CollapseSelectionCommand, this},
                {RoutedCommands.CopyPath, DocumentEditor.FormTab.CopyPathCommand, this},
                {RoutedCommands.CopyFullPath, DocumentEditor.FormTab.CopyFullPathCommand, this},
                {RoutedCommands.PasteChild, DocumentEditor.FormTab.PasteChildCommand, this},
                {RoutedCommands.DuplicateItem, DocumentEditor.FormTab.DuplicateItemCommand, this},
                {RoutedCommands.Clear, DocumentEditor.FormTab.ClearCommand, this}
            };
        }
    }
}