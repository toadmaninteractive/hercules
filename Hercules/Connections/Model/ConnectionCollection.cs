using Hercules.Documents;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shell;
using System.Xml;

namespace Hercules.Connections
{
    public class DbConnectionCollection : NotifyPropertyChanged
    {
        private readonly string configFilename = Path.Combine(PathUtils.ConfigFolder, "config.xml");
        public ObservableCollection<DbConnection> Items { get; } = new();

        DbConnection? activeConnection;

        public DbConnection? ActiveConnection
        {
            get => activeConnection;
            set => SetField(ref activeConnection, value);
        }

        public void LoadConfiguration()
        {
            this.Items.Clear();

            if (!File.Exists(configFilename))
                return;

            string? lastConnectionTitle = null;

            using (XmlReader xmlReader = XmlReader.Create(configFilename, new XmlReaderSettings() { CloseInput = true }))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);

                // Get last connection node
                XmlNode? xmlNode = xmlDoc.SelectSingleNode("/connections");

                if (xmlNode != null)
                    lastConnectionTitle = xmlNode.Attributes?["last"] != null ? xmlNode.Attributes["last"]?.Value.Trim() : null;

                // Get connection nodes
                XmlNodeList? nodeList = xmlDoc.SelectNodes("/connections/entry");

                if (nodeList != null)
                {
                    foreach (var node in nodeList.Cast<XmlNode>())
                    {
                        // Get title
                        string? title = node.Attributes?["title"] != null ? node.Attributes["title"]?.Value.Trim() : null;

                        // Check title
                        if (string.IsNullOrEmpty(title))
                            continue;

                        // Get url
                        xmlNode = node.SelectSingleNode("url");
                        string url = xmlNode != null ? xmlNode.InnerText : string.Empty;

                        // Check url
                        if (string.IsNullOrEmpty(url))
                            continue;

                        // Get database
                        xmlNode = node.SelectSingleNode("database");
                        string database = xmlNode != null ? xmlNode.InnerText : string.Empty;

                        // Check database
                        if (string.IsNullOrEmpty(database))
                            continue;

                        // Get username
                        xmlNode = node.SelectSingleNode("username");
                        string username = xmlNode != null ? xmlNode.InnerText : string.Empty;

                        // Get password
                        xmlNode = node.SelectSingleNode("password");
                        string password = xmlNode != null ? xmlNode.InnerText : string.Empty;

                        xmlNode = node.SelectSingleNode("folder");
                        string folder = xmlNode != null ? xmlNode.InnerText : PathUtils.GenerateUniqueFolderName();

                        // Get DbConnection instance
                        DbConnection conn = new DbConnection(title, new Uri(url.EnsureTrailingSlash()), username, password, database, folder);

                        // Add to dictionary
                        this.Items.Add(conn);
                    }
                }
            }

            // Check if last connection is valid
            if (!string.IsNullOrEmpty(lastConnectionTitle))
                ActiveConnection = Items.FirstOrDefault(conn => conn.Title == lastConnectionTitle);
        }

        public void SaveConfiguration()
        {
            // Do not add UTF-8 BOM mark
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                NewLineChars = "\r\n",
                Indent = true,
                IndentChars = "\t",
                Encoding = new UTF8Encoding(false),
                CloseOutput = true
            };

            using XmlWriter xml = XmlWriter.Create(configFilename, writerSettings);
            xml.WriteStartDocument();

            // Write last connection data
            xml.WriteStartElement("connections");
            if (ActiveConnection != null)
                xml.WriteAttributeString("last", ActiveConnection.Title);

            // Loop thru items
            foreach (var connection in Items)
            {
                xml.WriteStartElement("entry");
                xml.WriteAttributeString("title", connection.Title);
                xml.WriteElementString("url", connection.Url.ToString());
                xml.WriteElementString("database", connection.Database);
                xml.WriteElementString("username", connection.Username);
                xml.WriteElementString("password", connection.Password);
                xml.WriteElementString("folder", connection.Folder);
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndDocument();
        }

        public void SaveJumpList()
        {
            JumpList jumpList = new JumpList();
            JumpList.SetJumpList(Application.Current, jumpList);

            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            var workingDirectory = Path.GetDirectoryName(exeName);

            foreach (var db in Items)
            {
                JumpTask jumpTask = new JumpTask
                {
                    Title = db.Title,
                    CustomCategory = "Databases",
                    ApplicationPath = exeName,
                    WorkingDirectory = workingDirectory,
                    IconResourcePath = exeName,
                    IconResourceIndex = 0,
                    Arguments = HerculesUrl.GetDatabaseUrl(db)
                };
                jumpList.JumpItems.Add(jumpTask);
            }
            jumpList.Apply();
        }
    }
}
