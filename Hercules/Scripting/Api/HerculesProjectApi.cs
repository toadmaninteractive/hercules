using Hercules.Scripting.JavaScript;
using System.Collections.Generic;

namespace Hercules.Scripting
{
    public class HerculesProjectApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesProjectApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesProjectApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
        }

        [ScriptingApi("rootFolder", "Project root folder")]
        public string? RootFolder => Context.Core.Project?.Settings.ProjectRootFolder;

        [ScriptingApi("title", "Project title")]
        public string? Title => Context.Core.Project?.Connection.Title;

        [ScriptingApi("database", "Project database name")]
        public string? Database => Context.Core.Project?.Connection.Database;

        [ScriptingApi("username", "Project user name")]
        public string? Username => Context.Core.Project?.Connection.Username;

        [ScriptingApi("url", "Project database url")]
        public string? Url
        {
            get
            {
                if (Context.Core.Project == null)
                    return null;
                return Context.Core.Project?.Connection.Url.ToString().EnsureTrailingSlash();
            }
        }
    }
}
