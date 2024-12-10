using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Shell;
using Json;
using System;
using System.Reactive.Linq;

namespace Hercules.Diagrams
{
    public class DiagramModule : CoreModule
    {
        public DiagramModule(Core core)
            : base(core)
        {
            Workspace.WindowService.AddTool(ToolBoxTool = new ToolBoxTool());

            documentPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<DocumentEditorPage>().Subscribe(page => page.Services.Add(new DiagramPageService(page, ToolBoxTool, Workspace.PropertyEditorTool)));

            var viewToolboxOption = new UiCommandOption("Toolbox", Fugue.Icons.Toolbox, () => ToolBoxTool.Show());
            Workspace.OptionManager.AddMenuOption(viewToolboxOption, "View#10");

            var createDiagramSchemaCommand = Commands.Execute(CreateDiagramSchema).If(() => Core.Project?.SchemafulDatabase.Schema != null && Core.Project.SchemafulDatabase.DiagramSchema == null);
            var createDiagramSchemaOption = new UiCommandOption("Create Diagram Schema...", null, createDiagramSchemaCommand);
            Workspace.OptionManager.AddMenuOption(createDiagramSchemaOption, "Document#0/New#20");
        }

        public ToolBoxTool ToolBoxTool { get; }

        readonly IDisposable documentPageSubscription;

        void CreateDiagramSchema()
        {
            var schema = Igor.Schema.DiagramSchemaJsonSerializer.Instance.Serialize(new Igor.Schema.DiagramSchema(Array.Empty<Igor.Schema.Prototype>(), Array.Empty<string>()));
            var schemaJson = new JsonObject(schema.AsObject) { ["scope"] = "schema" };
            Core.GetModule<DocumentsModule>().CreateDocument("diagram_schema", new DocumentDraft(schemaJson));
        }
    }
}