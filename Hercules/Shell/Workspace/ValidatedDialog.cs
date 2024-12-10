using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Hercules.Shell
{
    public abstract class ValidatedDialog : Dialog, IDataErrorInfo
    {
        public string Error
        {
            get
            {
                var type = this.GetType();
                var modelClassProperties = TypeDescriptor
                    .GetProperties(type)
                    .Cast<PropertyDescriptor>();

                return
                    (from modelProp in modelClassProperties
                     let error = this[modelProp.Name]
                     where !string.IsNullOrWhiteSpace(error)
                     select error)
                        .Aggregate(new StringBuilder(), (acc, next) => acc.Append(" ").Append(next))
                        .ToString();
            }
        }

        public virtual string this[string columnName]
        {
            get
            {
                var type = this.GetType();
                var modelClassProperties = TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>();

                var errorText =
                    (from modelProp in modelClassProperties
                     where modelProp.Name == columnName
                     from attribute in modelProp.Attributes.OfType<ValidationAttribute>()
                     where !attribute.IsValid(modelProp.GetValue(this))
                     select attribute.FormatErrorMessage(modelProp.Name))
                        .FirstOrDefault();

                if (string.IsNullOrEmpty(errorText))
                    errorText =
                    (from method in type.GetMethods()
                     let validator = method.GetCustomAttributes(typeof(PropertyValidatorAttribute), false).Cast<PropertyValidatorAttribute>().FirstOrDefault(v => v.Property == columnName)
                     where validator != null
                     let result = method.Invoke(this, null) as string
                     where !string.IsNullOrEmpty(result)
                     select result).FirstOrDefault();

                return errorText ?? string.Empty;
            }
        }

        protected override bool IsOkEnabled()
        {
            return string.IsNullOrEmpty(Error);
        }
    }
}
