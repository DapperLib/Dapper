using System;
using System.Collections.Generic;

namespace Dapper
{
    partial class SqlMapper
    {
        private sealed class DapperTable
        {
            private string[] fieldNames;
            private readonly Dictionary<string, int> fieldNameLookup;

            internal string[] FieldNames => fieldNames;

            public DapperTable(string[] fieldNames)
            {
                if (fieldNames == null)
                {
                    throw new ArgumentNullException(nameof(fieldNames));
                }
                this.fieldNames = fieldNames;

                fieldNameLookup = new Dictionary<string, int>(fieldNames.Length, StringComparer.Ordinal);
                // if there are dups, we want the **first** key to be the "winner" - so iterate backwards
                for (var i = fieldNames.Length - 1; i >= 0; i--)
                {
                    var key = fieldNames[i];
                    if (key != null)
                    {
                        fieldNameLookup[key] = i;
                    }
                }
            }

            internal int IndexOfName(string name)
            {
                int result;
                return (name != null && fieldNameLookup.TryGetValue(name, out result)) ? result : -1;
            }

            internal int AddField(string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                if (fieldNameLookup.ContainsKey(name))
                {
                    throw new InvalidOperationException("Field already exists: " + name);
                }
                var oldLen = fieldNames.Length;
                Array.Resize(ref fieldNames, oldLen + 1); // yes, this is sub-optimal, but this is not the expected common case
                fieldNames[oldLen] = name;
                fieldNameLookup[name] = oldLen;
                return oldLen;
            }

            internal bool FieldExists(string key)
            {
                return key != null && fieldNameLookup.ContainsKey(key);
            }

            public int FieldCount => fieldNames.Length;
        }
    }
}
