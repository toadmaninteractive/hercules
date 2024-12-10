using AvalonDock;
using AvalonDock.Layout.Serialization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Hercules.Shell.View
{
    public class DockingLayoutService : IDockingLayoutService
    {
        private readonly DockingManager dockingManager;
        private readonly bool resetLayout;

        public string LayoutFileName { get; }

        public DockingLayoutService(DockingManager dockingManager, string layoutFileName, bool resetLayout)
        {
            this.dockingManager = dockingManager;
            this.LayoutFileName = layoutFileName;
            this.resetLayout = resetLayout;
            this.dockingManager.Loaded += DockingManager_Loaded;
            this.dockingManager.Unloaded += DockingManager_Unloaded;
            this.dockingManager.LayoutUpdateStrategy = new LayoutInitializer();
        }

        public void LoadLayout()
        {
            var layoutSerializer = new XmlLayoutSerializer(dockingManager);
            layoutSerializer.LayoutSerializationCallback += LayoutSerializer_LayoutSerializationCallback;
            layoutSerializer.Deserialize(LayoutFileName);
        }

        public void SaveLayout()
        {
            XmlLayoutSerializer xmlLayout = new XmlLayoutSerializer(dockingManager);
            using var stream = new MemoryStream();
            xmlLayout.Serialize(stream);
            stream.Position = 0;
            var xmlDoc = XDocument.Load(stream);
            RemoveLayoutDocuments(xmlDoc.Root);
            xmlDoc.Save(LayoutFileName);
        }

        void RemoveLayoutDocuments(XElement element)
        {
            var children = element.Descendants().Where(i => i.Name == "LayoutDocument").ToList();
            foreach (var child in children)
                child.Remove();
            foreach (var d in element.Descendants())
                RemoveLayoutDocuments(d);
        }

        public void LoadDefaultLayout()
        {
            using var stream = GetType().Assembly.GetManifestResourceStream("Hercules.Resources.layout.xml");
            var layoutSerializer = new XmlLayoutSerializer(dockingManager);
            layoutSerializer.LayoutSerializationCallback += LayoutSerializer_LayoutSerializationCallback;
            layoutSerializer.Deserialize(stream);
        }

        void LayoutSerializer_LayoutSerializationCallback(object? sender, LayoutSerializationCallbackEventArgs e)
        {
            string sId = e.Model.ContentId;

            if (string.IsNullOrWhiteSpace(sId) || e.Content == null)
                e.Cancel = true;
        }

        void DocumentContextMenu_Closed(object? sender, RoutedEventArgs e)
        {
            // we don't want DocumentContextMenu to store reference to data context, cause it prevents viewmodel from being garbage collected
            this.dockingManager.DocumentContextMenu.DataContext = null;
        }

        private void DockingManager_Loaded(object? sender, RoutedEventArgs e)
        {
            if (!resetLayout && File.Exists(LayoutFileName))
                LoadLayout();
            else
                LoadDefaultLayout();
            this.dockingManager.DocumentContextMenu.Closed += DocumentContextMenu_Closed;
        }

        private void DockingManager_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveLayout();
            this.dockingManager.DocumentContextMenu.Closed -= DocumentContextMenu_Closed;
        }
    }
}
