using Hercules.Documents.Editor;
using Hercules.Forms.Elements;
using Json;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls.Diagrams.Extensions.ViewModels;
using ICommand = System.Windows.Input.ICommand;

namespace Hercules.InteractiveMaps
{
    /// <summary>
    /// ViewModel for Interactive Maps
    /// </summary>
    public class InteractiveMapViewModel : ObservableGraphSourceBase<PropertyBlockShape, LinkViewModelBase<NodeViewModelBase>>
    {
        public ElementProperties Properties { get; private set; }
        private IDisposable formElementSubscription;

        // For commands
        public ICommand CreateNewBlockCommand { get; }

        public ICommand SynchronizeCommand { get; private set; }

        // For transactions
        private readonly DocumentForm form;

        private readonly DocumentEditorPage editor;
        private readonly InteractiveMapElement element;

        // Background image
        private readonly string imageName;

        public BitmapSource BackgroundImage { get; private set; }

        // CurrentSelected property with backing field
        private PropertyBlockShape currentSelected;

        public PropertyBlockShape CurrentSelected
        {
            get => currentSelected;
            set
            {
                if (value is PropertyBlockShape)
                {
                    var newValue = value;

                    if (newValue.FormListItem != null)
                        currentSelected = newValue;

                    Properties.SetFields(currentSelected?.FormListItem == null
                        ? form.Root.Record.Children
                        : ((RecordElement)currentSelected.FormListItem.Element).Children);
                }
                else
                    Properties.SetFields(null);

                editor.Properties.Value = Properties;
            }
        }

        // IsDrawingNewShape property with backing field
        public bool isDrawingNewShape = false;

        public bool IsDrawingNewShape
        {
            get => isDrawingNewShape;
            set
            {
                isDrawingNewShape = value;
                base.OnPropertyChanged(nameof(IsDrawingNewShape));
            }
        }

        public ForegroundJob Job { get; private set; }

        public InteractiveMapViewModel(DocumentEditorPage editor, InteractiveMapElement element)
        {
            Job = new ForegroundJob();
            CreateNewBlockCommand = Commands.Execute<JsonObject>(CreateBlock);

            this.editor = editor;
            this.element = element;
            form = editor.FormTab.Form;
            Properties = new ElementProperties(editor, null);
            imageName = element.GetImageFile().Value;
            var attachment = editor.Attachments.Items.FirstOrDefault(d => d.Name == imageName);
            BackgroundImage = FileUtils.LoadImageFromFile(attachment.Attachment.File.FileName);
            LoadContent();
            //Logger.LogError("Cannot open image. Check if current document contains at least one image attached and this image filename is specified in ");
        }

        public void LoadContent()
        {
            Clear();

            if (formElementSubscription != null)
                formElementSubscription.Dispose();

            foreach (var element in element.GetBlocks().Children.Cast<ListItem>())
            {
                element.PropertyChanged += ElementChanged;
                AddBlockToDiagram(element);
            }

            formElementSubscription = element.GetBlocks().Children.OnCollectionChanged().Subscribe(FormElementListChanged);
        }

        public void CreateBlock(JsonObject block)
        {
            var blocksList = element.GetBlocks();
            form.Run(transaction => blocksList.PasteElement(block, transaction));
        }

        private void AddBlockToDiagram(ListItem element)
        {
            currentSelected = new PropertyBlockShape();
            currentSelected.SetFields(element);
            base.AddNode(currentSelected);
        }

        private void FormElementListChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (ListItem element in e.NewItems!.Cast<ListItem>())
                    {
                        element.PropertyChanged += ElementChanged;
                        AddBlockToDiagram(element);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (ListItem element in e.OldItems!.Cast<ListItem>())
                    {
                        element.PropertyChanged -= ElementChanged;
                        var node = Items.Cast<PropertyBlockShape>().SingleOrDefault(x => x.FormListItem == element);
                        node.OnRemove();
                        base.RemoveItem(node);
                    }
                    break;
                default:
                    //Refresh();
                    break;
            }
        }

        public override bool RemoveItem(PropertyBlockShape node)
        {
            form.Run(transaction => node.FormListItem.List.Remove(node.FormListItem, transaction));
            return true;
        }

        private void ElementChanged(object? sender, PropertyChangedEventArgs e)
        {
            // var element = (ListItem)sender;
        }
    }
}
