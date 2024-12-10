using Hercules.Shell;
using System;

namespace Hercules
{
    public abstract class CoreModule
    {
        protected Core Core { get; }
        protected Workspace Workspace => Core.Workspace;

        protected CoreModule(Core core)
        {
            this.Core = core;
        }

        public virtual void OnLoad(Uri? startUri)
        {
        }

        public virtual void OnLoaded()
        {
        }

        public virtual void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
        }

        public virtual void OnSaveProject(ISettingsWriter settingsWriter)
        {
        }

        public virtual void OnCloseProject()
        {
        }

        public virtual void OnShutdown()
        {
        }
    }
}
