using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CouchDB
{
    public class CdbQueryBuilder
    {
        public const string TrueString = "true";
        public const string FalseString = "false";

        private readonly StringBuilder builder;

        public CdbQueryBuilder(Uri baseUrl, params string[] urlParts)
        {
            if (baseUrl == null)
                throw new ArgumentNullException(nameof(baseUrl));
            builder = new StringBuilder(baseUrl.AbsoluteUri);
            foreach (var urlPart in urlParts)
            {
                builder.Append('/');
                builder.Append(urlPart);
            }
        }

        public CdbQueryBuilder(string baseUrl, params string[] urlParts)
        {
            if (baseUrl == null)
                throw new ArgumentNullException(nameof(baseUrl));
            builder = new StringBuilder(baseUrl);
            foreach (var urlPart in urlParts)
            {
                builder.Append('/');
                builder.Append(urlPart);
            }
        }

        public bool HasQueryArgs { get; set; }

        public override string ToString() => builder.ToString();

        public Uri ToUri() => new(builder.ToString(), UriKind.Relative);

        void AppendParam(string param, string value)
        {
            builder.Append(HasQueryArgs ? '&' : '?');
            HasQueryArgs = true;
            builder.Append(param);
            builder.Append('=');
            builder.Append(value);
        }

        public CdbQueryBuilder AppendBoolean(string param, bool value)
        {
            AppendParam(param, value ? TrueString : FalseString);
            return this;
        }

        public CdbQueryBuilder AppendBoolean(string param, bool? value)
        {
            if (value.HasValue)
                AppendBoolean(param, value.Value);
            return this;
        }

        public CdbQueryBuilder AppendBoolean(string param, bool value, bool defaultValue)
        {
            if (value != defaultValue)
                AppendBoolean(param, value);
            return this;
        }

        public CdbQueryBuilder AppendString(string param, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                AppendParam(param, Uri.EscapeDataString(value));
            return this;
        }

        public CdbQueryBuilder AppendStringArray(string param, IEnumerable<string>? values)
        {
            if (values != null)
            {
                var json = new JsonArray(values.Select(ImmutableJson.Create));
                AppendParam(param, Uri.EscapeDataString(json.ToString()));
            }
            return this;
        }

        public CdbQueryBuilder AppendJson(string param, ImmutableJson? value)
        {
            if (value != null)
                AppendParam(param, Uri.EscapeDataString(value.ToString()));
            return this;
        }

        public CdbQueryBuilder AppendNumber(string param, int number)
        {
            AppendParam(param, number.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        public CdbQueryBuilder AppendNumber(string param, int number, int defaultValue)
        {
            if (number != defaultValue)
                AppendParam(param, number.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        public CdbQueryBuilder AppendNumber(string param, int? number)
        {
            if (number.HasValue)
                AppendParam(param, number.Value.ToString(CultureInfo.InvariantCulture));
            return this;
        }
    }
}
