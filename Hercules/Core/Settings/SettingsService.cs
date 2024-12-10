using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Hercules
{
    public interface ISettingGroup
    {
        IEnumerable<ISetting> GetSettings();
    }

    public class SettingsService
    {
        private readonly List<ISetting> settings = new List<ISetting>();

        public void AddSetting(ISetting setting)
        {
            settings.Add(setting);
            setting.PropertyChanged += PropertyChanged;
        }

        public void AddSettingGroup(ISettingGroup settingGroup)
        {
            foreach (var setting in settingGroup.GetSettings())
                AddSetting(setting);
        }

        private void PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (SaveOnChange)
                Save();
        }

        private readonly string filename;

        public SettingsService(string filename)
        {
            this.filename = filename;
        }

        public void Load(ISettingsReader settingsReader)
        {
            foreach (var setting in settings)
            {
                try
                {
                    setting.Read(settingsReader);
                }
                catch (Exception exception)
                {
                    Logger.LogException($"Failed to load setting {setting.Name}", exception);
                }
            }
        }

        public bool SaveOnChange { get; set; }

        public void Save()
        {
            var writer = new JsonSettingsWriter();
            foreach (var setting in settings)
                setting.Write(writer);
            writer.Save(filename);
        }
    }
}
