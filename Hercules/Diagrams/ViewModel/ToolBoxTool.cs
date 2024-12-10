using Hercules.Forms.Schema;
using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Hercules.Diagrams
{
    public class ToolBoxItem
    {
        public SchemaBlock Prototype { get; }
        public BitmapImage? IconSource { get; }
        public string Name { get; }

        public ToolBoxItem(SchemaBlock prototype)
        {
            Prototype = prototype;
            if (prototype.IconName != null)
                IconSource = (BitmapImage)Application.Current.FindResource(prototype.IconName);
            Name = prototype.Name;
        }
    }

    public class ToolBoxTool : Tool
    {
        public ObservableCollection<ToolBoxItem> ToolBoxItems { get; } = new ObservableCollection<ToolBoxItem>();

        public ToolBoxTool()
        {
            this.Title = "Toolbox";
            this.ContentId = "{Toolbox}";
            this.Pane = "RightToolsPane";
            this.IsVisible = false;
        }

        public void Refresh(IEnumerable<SchemaBlock> prototypes)
        {
            ToolBoxItems.Clear();
            foreach (var prototype in prototypes)
            {
                var missionToolboxItem = new ToolBoxItem(prototype);
                ToolBoxItems.Add(missionToolboxItem);
            }
        }

        public IReadOnlyList<ToolBoxItem> GetApplicableBlocks(BaseConnector sourceConnector)
        {
            switch (sourceConnector.Kind)
            {
                case ConnectorKind.In:
                case ConnectorKind.Invalid:
                case ConnectorKind.Asset:
                    return Array.Empty<ToolBoxItem>();
            }

            return ToolBoxItems
                .Where(item =>
                {
                    return item.Prototype.Connectors.Any(c =>
                            {
                                var result = sourceConnector.Kind switch
                                {
                                    ConnectorKind.Out => c.Kind == ConnectorKind.In,
                                    ConnectorKind.Property => c.Kind == ConnectorKind.Asset,
                                    _ => false
                                };

                                if (result && !string.IsNullOrEmpty(sourceConnector.Category))
                                    result = sourceConnector.Category == c.Category;

                                return result;
                            });
                }).ToList();
        }
    }
}