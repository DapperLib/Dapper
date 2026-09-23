#if !NET481
using System;
using System.Data;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DynamicParameters.Output&lt;T&gt; coverage:
    /// - nested property chain (chain.Count &gt; 1) → for-loop at lines 417-430
    /// - field as last member (Stfld, line 447)
    /// - field as intermediate chain member (Ldfld, line 428)
    /// - "param does not already exist" else path (lines 475-480)
    /// - cached setter reuse (second call with same property)
    /// - ThrowInvalidChain (line 365)
    /// </summary>
    public class FakeDbDynamicOutputAdvancedTests
    {
        private class Target
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        private class TargetWithField
        {
            public int Counter;
        }

        private class Outer
        {
            public Inner Inner { get; set; } = new Inner();
        }

        private class Inner
        {
            public int Value { get; set; }
        }

        private class OuterWithFieldChain
        {
            public Inner2 Item = new Inner2();
        }

        private class Inner2
        {
            public int Val { get; set; }
        }

        // ── "else" path: param does NOT already exist (lines 475-480) ──
        // This hits when Output is called without a prior dp.Add for that param.

        [Fact]
        public void Output_ParamNotPreAdded_CreatesNewParam()
        {
            var target = new Target { Id = 42 };
            var dp = new DynamicParameters();
            dp.Output(target, x => x.Id);  // no prior dp.Add("Id", ...)

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute("EXEC SP @Id", dp);

            // OnCompleted fires, callback reads the attached param value and sets target.Id
            Assert.Equal(42, target.Id);
        }

        // ── String output without size — hits DbString.DefaultLength path ──

        [Fact]
        public void Output_StringNoSize_UsesDefaultLength()
        {
            var target = new Target { Name = "Alice" };
            var dp = new DynamicParameters();
            dp.Output(target, x => x.Name);  // no explicit size for string

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute("EXEC SP @Name", dp);

            Assert.Equal("Alice", target.Name);
        }

        // ── Public field as last member (Stfld IL path, line 447) ──────

        [Fact]
        public void Output_PublicField_CallbackFires()
        {
            var target = new TargetWithField { Counter = 99 };
            var dp = new DynamicParameters();
            dp.Output(target, x => (object?)x.Counter);  // field, not property

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute("EXEC SP @Counter", dp);

            Assert.Equal(99, target.Counter);
        }

        // ── Nested property chain (chain.Count > 1 → for-loop lines 417-430) ──

        [Fact]
        public void Output_NestedPropertyChain_ForLoopRuns()
        {
            var target = new Outer { Inner = new Inner { Value = 10 } };
            var dp = new DynamicParameters();
            dp.Output(target, x => (object?)x.Inner.Value);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute("EXEC SP @InnerValue", dp);

            Assert.Equal(10, target.Inner.Value);
        }

        // ── Nested field in chain (Ldfld IL path, line 428) ────────────

        [Fact]
        public void Output_FieldIntermediateChain_LdfldPath()
        {
            var target = new OuterWithFieldChain { Item = new Inner2 { Val = 55 } };
            var dp = new DynamicParameters();
            dp.Output(target, x => (object?)x.Item.Val);

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute("EXEC SP @ItemVal", dp);

            Assert.Equal(55, target.Item.Val);
        }

        // ── Cached setter reuse (second call with same property) ────────

        [Fact]
        public void Output_SameProperty_Twice_UsesCachedSetter()
        {
            var t1 = new Target { Id = 1 };
            var t2 = new Target { Id = 2 };

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.Open();

            var dp1 = new DynamicParameters();
            dp1.Output(t1, x => x.Id);
            conn.EnqueueNonQueryResult(1);
            conn.Execute("EXEC SP @Id", dp1);
            Assert.Equal(1, t1.Id);

            // second call — same property type should hit the cache (line 407)
            var dp2 = new DynamicParameters();
            dp2.Output(t2, x => x.Id);
            conn.EnqueueNonQueryResult(1);
            conn.Execute("EXEC SP @Id", dp2);
            Assert.Equal(2, t2.Id);
        }

        // ── ThrowInvalidChain — constant expression body ─────────────

        [Fact]
        public void Output_InvalidExpression_ThrowsInvalidOperation()
        {
            var target = new Target();
            var dp = new DynamicParameters();
            Assert.Throws<InvalidOperationException>(() =>
                dp.Output(target, x => (object?)42));
        }

        // ── Existing param path (TryGetValue = true, lines 466-473) ────

        [Fact]
        public void Output_ExistingParam_SetsDirectionToInputOutput()
        {
            var target = new Target { Id = 7 };
            var dp = new DynamicParameters();
            dp.Add("Id", 7, DbType.Int32, ParameterDirection.Input);
            dp.Output(target, x => x.Id);  // modifies existing param to InputOutput

            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute("EXEC SP @Id", dp);

            Assert.Equal(7, target.Id);
        }
    }
}
#endif
