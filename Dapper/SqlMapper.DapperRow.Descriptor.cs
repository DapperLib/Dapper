using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dapper
{
    public static partial class SqlMapper
    {
        [TypeDescriptionProvider(typeof(DapperRowTypeDescriptionProvider))]
        private sealed partial class DapperRow
        {
            private sealed class DapperRowTypeDescriptionProvider : TypeDescriptionProvider
            {
                public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
                    => new DapperRowTypeDescriptor(instance);
                public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
                    => new DapperRowTypeDescriptor(instance);
            }

            //// in theory we could implement this for zero-length results to bind; would require
            //// additional changes, though, to capture a table even when no rows - so not currently provided
            //internal sealed class DapperRowList : List<DapperRow>, ITypedList
            //{
            //    private readonly DapperTable _table;
            //    public DapperRowList(DapperTable table) { _table = table; }
            //    PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
            //    {
            //        if (listAccessors != null && listAccessors.Length != 0) return PropertyDescriptorCollection.Empty;

            //        return DapperRowTypeDescriptor.GetProperties(_table);
            //    }

            //    string ITypedList.GetListName(PropertyDescriptor[] listAccessors) => null;
            //}

            private sealed class DapperRowTypeDescriptor : ICustomTypeDescriptor
            {
                private readonly DapperRow _row;
                public DapperRowTypeDescriptor(object instance)
                    => _row = (DapperRow)instance;

                AttributeCollection ICustomTypeDescriptor.GetAttributes()
                    => AttributeCollection.Empty;

                string ICustomTypeDescriptor.GetClassName() => typeof(DapperRow).FullName;

                string ICustomTypeDescriptor.GetComponentName() => null;

                private static readonly TypeConverter s_converter = new ExpandableObjectConverter();
                TypeConverter ICustomTypeDescriptor.GetConverter() => s_converter;

                EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => null;

                PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => null;

                object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

                EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;

                EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;

                internal static PropertyDescriptorCollection GetProperties(DapperRow row) => GetProperties(row?.table, row);
                internal static PropertyDescriptorCollection GetProperties(DapperTable table, IDictionary<string,object> row = null)
                {
                    string[] names = table?.FieldNames;
                    if (names == null || names.Length == 0) return PropertyDescriptorCollection.Empty;
                    var arr = new PropertyDescriptor[names.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var type = row != null && row.TryGetValue(names[i], out var value) && value != null
                            ? value.GetType() : typeof(object);
                        arr[i] = new RowBoundPropertyDescriptor(type, names[i], i);
                    }
                    return new PropertyDescriptorCollection(arr, true);
                }
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => GetProperties(_row);

                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => GetProperties(_row);

                object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => _row;
            }

            private sealed class RowBoundPropertyDescriptor : PropertyDescriptor
            {
                private readonly Type _type;
                private readonly int _index;
                public RowBoundPropertyDescriptor(Type type, string name, int index) : base(name, null)
                {
                    _type = type;
                    _index = index;
                }
                public override bool CanResetValue(object component) => true;
                public override void ResetValue(object component) => ((DapperRow)component).Remove(_index);
                public override bool IsReadOnly => false;
                public override bool ShouldSerializeValue(object component) => ((DapperRow)component).TryGetValue(_index, out _);
                public override Type ComponentType => typeof(DapperRow);
                public override Type PropertyType => _type;
                public override object GetValue(object component)
                    => ((DapperRow)component).TryGetValue(_index, out var val) ? (val ?? DBNull.Value): DBNull.Value;
                public override void SetValue(object component, object value)
                    => ((DapperRow)component).SetValue(_index, value is DBNull ? null : value);
            }
        }
    }
}
