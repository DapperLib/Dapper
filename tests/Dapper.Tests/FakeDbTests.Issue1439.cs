#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue1439Tests
    {
        private sealed class Marker<T> { }

        // distinct closed types: Marker<int>, Marker<Marker<int>>, ...
        private static Type[] MakeTypes(int count)
        {
            var types = new Type[count];
            var t = typeof(Marker<int>);
            for (int i = 0; i < count; i++)
            {
                types[i] = t;
                t = typeof(Marker<>).MakeGenericType(t);
            }
            return types;
        }

        [Fact]
        public void ConcurrentAddTypeMapUpdatesAreNotLost()
        {
            const int Threads = 8, PerThread = 16, Rounds = 25;
            var types = MakeTypes(Threads * PerThread);
            try
            {
                for (int round = 0; round < Rounds; round++)
                {
                    using var barrier = new Barrier(Threads);
                    var tasks = Enumerable.Range(0, Threads).Select(n => Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        foreach (var type in types.Skip(n * PerThread).Take(PerThread))
                        {
                            SqlMapper.AddTypeMap(type, DbType.String);
                        }
                    }, TaskCreationOptions.LongRunning)).ToArray();
                    Task.WaitAll(tasks);

                    foreach (var type in types)
                    {
#pragma warning disable 618
                        Assert.Equal(DbType.String, SqlMapper.LookupDbType(type, "value", true, out _));
#pragma warning restore 618
                    }

                    // clear again so the next round races from a clean map
                    foreach (var type in types) SqlMapper.RemoveTypeMap(type);
                }
            }
            finally
            {
                foreach (var type in types) SqlMapper.RemoveTypeMap(type);
            }
        }

        [Fact]
        public void ConcurrentAddAndRemoveDoNotLoseOtherUpdates()
        {
            const int PerThread = 64, Rounds = 25;
            var keep = MakeTypes(PerThread);
            var churn = MakeTypes(PerThread * 2).Skip(PerThread).ToArray();
            try
            {
                for (int round = 0; round < Rounds; round++)
                {
                    using var barrier = new Barrier(2);
                    var adder = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        foreach (var type in keep) SqlMapper.AddTypeMap(type, DbType.String);
                    }, TaskCreationOptions.LongRunning);
                    var churner = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        foreach (var type in churn)
                        {
                            SqlMapper.AddTypeMap(type, DbType.Int32);
                            SqlMapper.RemoveTypeMap(type);
                        }
                    }, TaskCreationOptions.LongRunning);
                    Task.WaitAll(adder, churner);

                    foreach (var type in keep)
                    {
#pragma warning disable 618
                        Assert.Equal(DbType.String, SqlMapper.LookupDbType(type, "value", true, out _));
#pragma warning restore 618
                    }
                    foreach (var type in keep) SqlMapper.RemoveTypeMap(type);
                }
            }
            finally
            {
                foreach (var type in keep.Concat(churn)) SqlMapper.RemoveTypeMap(type);
            }
        }
    }
}
#endif
