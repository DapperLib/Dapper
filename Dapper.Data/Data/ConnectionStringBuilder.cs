using System.Collections.Generic;
using System.Text;

namespace System.Data
{
	/// <summary>
	/// Name tells it all
	/// </summary>
	public abstract class ConnectionStringBuilder
	{
		private readonly IDictionary<string, string> _properties =
			new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

		protected ConnectionStringBuilder AddProperty(string name, string value)
		{
			_properties[name] = value;
			return this;
		}

		protected ConnectionStringBuilder AddProperty(string name, object value)
		{
			return AddProperty(name, string.Format("{0}", value));
		}

		public override string ToString()
		{
			var buffer = new StringBuilder(255);
			foreach (var kv in _properties)
			{
				buffer.AppendFormat("{0}={1};", kv.Key, kv.Value);
			}
			return buffer.ToString();
		}
	}
}