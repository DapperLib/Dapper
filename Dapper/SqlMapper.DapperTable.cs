using System;
using System.Collections.Generic;

namespace Dapper
{
    public static partial class SqlMapper
    {
        private static IEqualityComparer<string> DefaultFieldNameComparer = StringComparer.Ordinal;

        /// <summary>
        /// Ability to override default field comparer (EX: enable case insensitive comparison)
        /// </summary>
        /// <param name="comparer"></param>
        public static void SetFieldNameComparer(IEqualityComparer<string> comparer)
        {
            DefaultFieldNameComparer = comparer;
        }

        private sealed class DapperTable
        {
            private string[] fieldNames;
            private readonly Dictionary<string, int> fieldNameLookup;

            internal string[] FieldNames => fieldNames;

            public DapperTable(string[] fieldNames)
            {
                this.fieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));

                fieldNameLookup = new Dictionary<string, int>(fieldNames.Length, DefaultFieldNameComparer);
                // if there are dups, we want the **first** key to be the "winner" - so iterate backwards
                for (int i = fieldNames.Length - 1; i >= 0; i--)
                {
                    string key = fieldNames[i];
                    if (key != null) fieldNameLookup[key] = i;
                }
            }

            internal int IndexOfName(string name)
            {
                return (name != null && fieldNameLookup.TryGetValue(name, out int result)) ? result : -1;
            }

            internal int AddField(string name)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (fieldNameLookup.ContainsKey(name)) throw new InvalidOperationException("Field already exists: " + name);
                int oldLen = fieldNames.Length;
                Array.Resize(ref fieldNames, oldLen + 1); // yes, this is sub-optimal, but this is not the expected common case
                fieldNames[oldLen] = name;
                fieldNameLookup[name] = oldLen;
                return oldLen;
            }

            internal bool FieldExists(string key) => key != null && fieldNameLookup.ContainsKey(key);

            public int FieldCount => fieldNames.Length;
        }
    }
}
