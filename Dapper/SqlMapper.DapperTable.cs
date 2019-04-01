using System;
using System.Collections.Generic;
using System.Data;

namespace Dapper
{
    public static partial class SqlMapper
    {
        internal sealed class DapperTable
        {
            internal readonly struct DapperColumn
            {
                public readonly string Name;
                public readonly Type Type;
                public DapperColumn(string name, Type type)
                {
                    Name = name;
                    Type = type;
                }
            }
            private DapperColumn[] _columns;
            private readonly Dictionary<string, int> _fieldNameLookup;
            private readonly int _readerOffset, _readerCount;

            internal DapperColumn[] Columns => _columns;

            private DapperTable(DapperColumn[] columns, int offset)
            {
                _readerOffset = offset;
                _readerCount = columns.Length;
                _columns = columns ?? throw new ArgumentNullException(nameof(columns));
                _fieldNameLookup = new Dictionary<string, int>(_columns.Length, StringComparer.Ordinal);
                // if there are dups, we want the **first** key to be the "winner" - so iterate backwards
                for (int i = columns.Length - 1; i >= 0; i--)
                {
                    string key = columns[i].Name;
                    if (key != null) _fieldNameLookup[key] = i;
                }
            }

            internal int IndexOfName(string name)
            {
                return (name != null && _fieldNameLookup.TryGetValue(name, out int result)) ? result : -1;
            }

            internal int AddField(string name, Type type)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (_fieldNameLookup.ContainsKey(name)) throw new InvalidOperationException("Field already exists: " + name);
                int oldLen = _columns.Length;
                Array.Resize(ref _columns, oldLen + 1); // yes, this is sub-optimal, but this is not the expected common case
                _columns[oldLen] = new DapperColumn(name, type);
                _fieldNameLookup[name] = oldLen;
                return oldLen;
            }

            public int FieldCount => _columns.Length;

            private static readonly DapperColumn[] _nixColumns = new DapperColumn[0];
            internal static DapperTable Create(IDataRecord reader, int offset, int count)
            {
                if (count == 0) return new DapperTable(_nixColumns, offset);
                var columns = new DapperColumn[count];
                var colIndex = offset;
                for (int i = 0; i < count; i++)
                {
                    columns[i] = new DapperColumn(
                        reader.GetName(colIndex),
                        reader.GetFieldType(colIndex));
                    colIndex++;
                }
                return new DapperTable(columns, offset);
            }

            internal object AddRow(IDataReader reader)
            {
                object[] values = new object[_readerCount];
                int offset = _readerOffset;
                for(int i = 0; i < values.Length; i++)
                {
                    object val = reader.GetValue(offset++);
                    values[i] = val is DBNull ? null : val;
                }
                return new DapperRow(this, values);
            }
            internal object AddRowUnlessFirstMissing(IDataReader reader)
            {
                int offset = _readerOffset;
                object value = reader.GetValue(offset++);
                if (value is DBNull) return null;

                object[] values = new object[_readerCount];
                values[0] = value;
                for (int i = 1; i < values.Length; i++)
                {
                    object val = reader.GetValue(offset++);
                    values[i] = val is DBNull ? null : val;
                }
                return new DapperRow(this, values);

            }
        }
    }
}
