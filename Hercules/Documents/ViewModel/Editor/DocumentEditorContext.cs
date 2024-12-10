using Hercules.Forms;
using Hercules.Forms.Elements;
using Hercules.Shell;
using Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.Documents.Editor
{
    /// <summary>
    /// Common parameters passed to DocumentEditorPage on creation
    /// </summary>
    public record DocumentEditorContext(
        Func<IDocument, IDocumentEditor, IDocumentProxy> GetDocumentProxy,
        IReadOnlyObservableValue<SchemafulDatabase> ObservableSchemafulDatabase,
        IDialogService DialogService,
        Func<ElementFactoryContext, IElementFactory> GetElementFactory,
        FormSettings FormSettings,
        IObservableValue<bool> AskSchemaUpdateConfirmationForModified,
        TempStorage TempStorage,
        Action<Category, JsonPath?> SummaryTableAction,
        Func<string, IReadOnlyList<IDocument>?, IProgress<string>, CancellationToken, Task> RunScriptAction);
}
