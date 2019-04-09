using System;
using System.Data;

namespace Dapper.Tests.TestEntities
{
    public enum ItemType
    {
        Foo = 1,
        Bar = 2,
    }

    public class ItemTypeHandler : SqlMapper.TypeHandler<ItemType>
    {
        public override ItemType Parse(object value)
        {
            var c = ((string)value)[0];
            switch (c)
            {
                case 'F': return ItemType.Foo;
                case 'B': return ItemType.Bar;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public override void SetValue(IDbDataParameter p, ItemType value)
        {
            p.DbType = DbType.AnsiStringFixedLength;
            p.Size = 1;

            switch (value)
            {
                case ItemType.Foo:
                {
                    p.Value = "F";
                    break;
                }
                case ItemType.Bar:
                {
                    p.Value = "B";
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    public class Item : IEquatable<Item>
    {
        public long Id { get; set; }
        public ItemType Type { get; set; }

        public Item(long id, ItemType type)
        {
            Id = id;
            Type = type;
        }

        public Item()
            : this(default(long), default(ItemType))
        {

        }

        public bool Equals(Item other)
        {
            bool returnValue;
            if ((Id == other.Id) && (Type == other.Type))
            {
                returnValue = true;
            }
            else
            {
                returnValue = false;
            }
            return returnValue;
        }
    }
}
