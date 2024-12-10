using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Hercules.Shell.View
{
    public class ViewModelTemplateSelector : DataTemplateSelector
    {
        private static readonly Dictionary<Type, DataTemplate> Templates = new();

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return base.SelectTemplate(item, container);
            var type = item.GetType();
            if (!Templates.TryGetValue(type, out var result))
            {
                if (ViewModelTypes.TryGetViewTypeByViewModelType(type, out var viewType))
                {
                    result = CreateTemplate(item.GetType(), viewType);
                    result.Seal();
                    Templates.Add(type, result);
                    return result;
                }
                return base.SelectTemplate(item, container);
            }
            else
                return result;
        }

        private DataTemplate CreateTemplate(Type viewModelType, Type viewType)
        {
            var xaml = $@"<DataTemplate DataType=""{{x:Type vm:{viewModelType.Name}}}""><v:{viewType.Name} /></DataTemplate>";

            var context = new ParserContext
            {
                XamlTypeMapper = new XamlTypeMapper(Array.Empty<string>())
            };

            context.XamlTypeMapper.AddMappingProcessingInstruction("vm", viewModelType.Namespace, viewModelType.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);

            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("vm", "vm");
            context.XmlnsDictionary.Add("v", "v");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            return template;
        }
    }
}
