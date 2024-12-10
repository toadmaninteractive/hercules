using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Forms
{
    public enum ExpandElementType
    {
        [Description("Expand root elements")]
        Expand,

        [Description("Expand elements recursively")]
        ExpandTree,

        [Description("Do not expand")]
        DoNotExpand,
    }

    public class FormSettings : ISettingGroup
    {
        public Setting<ExpandElementType> ExpandNewElement { get; } = new(nameof(ExpandNewElement), ExpandElementType.Expand);

        public Setting<ExpandElementType> ExpandNewDocument { get; } = new(nameof(ExpandNewDocument), ExpandElementType.DoNotExpand);

        public Setting<HorizontalAlignment> NumberTextAlignment { get; } = new(nameof(NumberTextAlignment), HorizontalAlignment.Left);

        public Setting<bool> SortEnumValues { get; } = new(nameof(SortEnumValues), false);

        public Setting<int> MaxStructFieldLabelSize { get; } = new(nameof(MaxStructFieldLabelSize), 100);

        public Setting<TimeZoneInfo> TimeZone { get; } = new TimeZoneSetting(nameof(TimeZone), TimeZoneInfo.Utc);

        public ICommand SetLocalTimeZoneCommand { get; }
        public ICommand SetUtcTimeZoneCommand { get; }

        public FormSettings()
        {
            SetLocalTimeZoneCommand = Commands.Execute(() => TimeZone.Value = TimeZoneInfo.Local);
            SetUtcTimeZoneCommand = Commands.Execute(() => TimeZone.Value = TimeZoneInfo.Utc);
        }

        public IEnumerable<ISetting> GetSettings()
        {
            yield return ExpandNewElement;
            yield return ExpandNewDocument;
            yield return NumberTextAlignment;
            yield return SortEnumValues;
            yield return MaxStructFieldLabelSize;
            yield return TimeZone;
        }
    }
}