using Hercules.Forms.Schema;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Diagrams.Core;

namespace Hercules.Diagrams.View
{
    public class BlockView : RadDiagramShape
    {
        public static readonly DependencyProperty CustomConnectorsProperty =
            DependencyProperty.Register("CustomConnectors",
            typeof(ConnectorCollection),
            typeof(BlockView),
            new FrameworkPropertyMetadata(OnCustomConnectorsBind));

        public ConnectorCollection? CustomConnectors
        {
            get => (ConnectorCollection?)GetValue(CustomConnectorsProperty);
            set => SetValue(CustomConnectorsProperty, value);
        }

        private static void OnCustomConnectorsBind(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BlockView target)
            {
                target.SetCustomConnectors(e.OldValue as ConnectorCollection, e.NewValue as ConnectorCollection);
            }
        }

        private void SetCustomConnectors(ConnectorCollection? oldCollection, ConnectorCollection? newCollection)
        {
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= Connectors_CollectionChanged;
            }
            Connectors.Clear();
            if (newCollection != null)
            {
                Connectors.AddRange(newCollection);
                newCollection.CollectionChanged += Connectors_CollectionChanged;
            }
        }

        private void Connectors_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Connectors.AddRange(e.NewItems!.Cast<IConnector>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Connectors.RemoveRange(e.OldItems!.Cast<IConnector>());
                    break;
            }
        }

        public BlockView(SchemaBlock prototype)
        {
            MinWidth = prototype.Archetype.BlockSize.Width;
            MinHeight = prototype.Archetype.BlockSize.Height;
            Width = MinWidth;
            Height = MinHeight;
            Geometry = null;
        }
    }
}