using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Json;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls.Diagrams.Extensions.ViewModels;
using Telerik.Windows.Diagrams.Core;

namespace Hercules.Diagrams
{
    public abstract class BlockBase : NodeViewModelBase
    {
        private static readonly JsonPath LayoutXPath = new JsonPath("layout", "x");
        private static readonly JsonPath LayoutYPath = new JsonPath("layout", "y");

        public SchemaBlock Prototype { get; }
        public BlockListItem FormListItem { get; }
        public BitmapImage? IconSource { get; }
        public ArchetypeType Type => Prototype.Archetype.Type;
        public ConnectorCollection Connectors { get; }
        public Visibility VisibilityInOutConnectors { get; protected set; }
        public Visibility VisibilityPropertyConnectors { get; protected set; }

        /// <summary>
        /// ToolBox or Diagram hosting (need for copy/past blocks)
        /// </summary>
        private readonly DocumentForm form;
        private readonly FloatElement? layoutX;
        private readonly FloatElement? layoutY;
        private readonly object layoutUndoRedoGroup = new();
        private bool isSettingPositionFromForm;
        private bool isSettingPositionFromDiagram;

        protected BlockBase(SchemaBlock prototype, BlockListItem element)
        {
            Width = prototype.Archetype.BlockSize.Width;
            Height = prototype.Archetype.BlockSize.Height;
            this.VisibilityInOutConnectors = Visibility.Visible;
            this.VisibilityPropertyConnectors = Visibility.Visible;
            this.Prototype = prototype;
            this.FormListItem = element;
            this.form = element.Form;
            if (prototype.IconName != null)
                IconSource = (BitmapImage)Application.Current.FindResource(prototype.IconName);
            this.Connectors = new ConnectorCollection();
            FillConnectorsFromPrototype();
            this.layoutX = this.FormListItem.GetByPath(LayoutXPath) as FloatElement;
            this.layoutY = this.FormListItem.GetByPath(LayoutYPath) as FloatElement;
            if (layoutX == null || layoutY == null)
            {
                this.Position = new Point(0, 0);
            }
            else
            {
                SetPositionFromForm();
            }
        }

        /// <summary>
        /// Factory for drop new block and for ToolBox
        /// </summary>
        public static BlockBase Factory(SchemaBlock? prototype, BlockListItem element)
        {
            prototype ??= SchemaBlock.InvalidBlock;
            BlockBase result = prototype.Archetype.CreateBlock(prototype, element);
            result.SetSpecialFields();
            result.UpdateConnectorFillColors();
            return result;
        }

        void SetPositionFromForm()
        {
            if (isSettingPositionFromDiagram)
                return;
            SetPositionFromFormNoCheck();
        }

        void SetPositionFromFormNoCheck()
        {
            isSettingPositionFromForm = true;
            Position = new Point(layoutX?.Value ?? 0, layoutY?.Value ?? 0);
            isSettingPositionFromForm = false;
        }

        public virtual void SetSpecialFields()
        {
        }

        public string GetRefValue()
        {
            return FormListItem.RefElement.Value;
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(Position) && layoutX != null && layoutY != null && !isSettingPositionFromForm)
            {
                isSettingPositionFromDiagram = true;
                form.Run(transaction =>
                {
                    layoutX.SetValue(Position.X, transaction, layoutUndoRedoGroup);
                    layoutY.SetValue(Position.Y, transaction, layoutUndoRedoGroup);
                });
                isSettingPositionFromDiagram = false;
            }

            base.OnPropertyChanged(propertyName);
        }

        public void SetPosition(Point position, ITransaction transaction)
        {
            layoutX?.SetValue(position.X, transaction);
            layoutY?.SetValue(position.Y, transaction);
        }

        private void FillConnectorsFromPrototype()
        {
            foreach (var connector in Prototype.Connectors)
            {
                if (connector.Field == null)
                {
                    Connectors.Add(new SimpleConnector(this, connector, VisibilityInOutConnectors));
                }
                else
                {
                    IConnector? assetDiagramConnector;
                    if (Prototype.Record.AllFields.First(f => f.Name == connector.Field).Type is ListSchemaType)
                        assetDiagramConnector = new PropertyListConnector(this, connector, VisibilityPropertyConnectors);
                    else
                        assetDiagramConnector = new PropertySingleConnector(this, connector, VisibilityPropertyConnectors);

                    Connectors.Add(assetDiagramConnector);
                }
            }
        }

        public void UpdateConnectorFillColors()
        {
            foreach (var connector in Connectors.OfType<PropertyConnector>())
                connector.UpdateFillColor();
        }

        public void UpdatePosition(ITransaction transaction)
        {
            if (layoutX != null && layoutY != null && (layoutX.WasModified(transaction) || layoutY.WasModified(transaction)))
            {
                SetPositionFromForm();
            }
        }
    }
}