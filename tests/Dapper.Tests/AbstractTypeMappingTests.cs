using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientAbstractTypeMappingTests : AbstractTypeMappingTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientAbstractTypeMappingTests : AbstractTypeMappingTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class AbstractTypeMappingTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestAbstractTypeMapping()
        {
            var previousMapping = SqlMapper.CurrentAbstractTypeMap;
            SqlMapper.PurgeQueryCache();
            try
            {
                SqlMapper.SetAbstractTypeMap(t => t == typeof(AbstractTypeMapping.IThing) ? typeof(AbstractTypeMapping.Thing) : null);
                
                var thing = connection.Query<AbstractTypeMapping.IThing>("select 'Hello!' Name, 42 Power").First();
                Assert.Equal(42, thing.Power);
                Assert.Equal("Hello!", thing.Name);
            }
            finally
            {
                SqlMapper.SetAbstractTypeMap( previousMapping );
                SqlMapper.PurgeQueryCache();
           }
        }

        [Fact]
        public void TestAbstractTypeMappingCombination()
        {
            var previousMapping = SqlMapper.CurrentAbstractTypeMap;
            SqlMapper.PurgeQueryCache();
            try
            {
                // IThing is mapped to Thing.
                SqlMapper.SetAbstractTypeMap(t => t == typeof(AbstractTypeMapping.IThing) ? typeof(AbstractTypeMapping.Thing) : null);

                // "Override": IThing is mapped to ThingMultiplier.
                SqlMapper.AddAbstractTypeMap(current =>
                {
                    return t =>
                    {
                        if (t == typeof(AbstractTypeMapping.IThing)) return typeof(AbstractTypeMapping.ThingMultiplier);
                        return current?.Invoke(t);
                    };
                });

                var thing = connection.Query<AbstractTypeMapping.IThing>("select 'Hello!' Name, 42 Power").First();
                Assert.Equal(84, thing.Power);
                Assert.Equal("Hello!", thing.Name);
            }
            finally
            {
                SqlMapper.SetAbstractTypeMap( previousMapping );
                SqlMapper.PurgeQueryCache();
            }
        }

        public static class AbstractTypeMapping
        {
            public interface IThing
            {
                int Power { get; }

                string Name { get; }
            }

            public class Thing : IThing
            {
                public int Power { get; set; }

                public string Name { get; set; }
            }

            public class ThingMultiplier : IThing
            {
                int _power;

                public int Power { get => _power * 2; set => _power = value; }

                public string Name { get; set; }
            }
        }

    }
}
