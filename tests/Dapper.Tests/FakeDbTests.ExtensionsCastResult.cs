#if !NET481
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers Extensions.CastResult&lt;TFrom,TTo&gt; (lines 12-22) and OnTaskCompleted (lines 25-42)
    /// via reflection. Triggers the async continuation path when the source task is not yet complete.
    /// </summary>
    public class FakeDbExtensionsCastResultTests
    {
        private static MethodInfo GetCastResult()
        {
            var type = typeof(SqlMapper).Assembly.GetType("Dapper.Extensions")!;
            return type.GetMethod("CastResult",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)!;
        }

        // ── Fast path: already-completed task (RanToCompletion) ───────

        [Fact]
        public async Task CastResult_CompletedTask_FastPath()
        {
            var method = GetCastResult().MakeGenericMethod(typeof(string), typeof(object));
            var completed = Task.FromResult("hello");

            var result = (Task<object>)method.Invoke(null, new object[] { completed })!;
            Assert.Equal("hello", await result);
        }

        // ── Null task → ArgumentNullException ─────────────────────────

        [Fact]
        public void CastResult_NullTask_Throws()
        {
            var method = GetCastResult().MakeGenericMethod(typeof(string), typeof(object));
            var ex = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object?[] { null }));
            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }

        // ── Async path: RanToCompletion (OnTaskCompleted SetResult) ───

        [Fact]
        public async Task CastResult_AsyncPath_RanToCompletion()
        {
            var method = GetCastResult().MakeGenericMethod(typeof(string), typeof(object));
            var tcs = new TaskCompletionSource<string>();

            var castTask = (Task<object>)method.Invoke(null, new object[] { tcs.Task })!;
            Assert.False(castTask.IsCompleted); // not yet done

            tcs.SetResult("world");
            var result = await castTask;
            Assert.Equal("world", result);
        }

        // ── Async path: Canceled (OnTaskCompleted SetCanceled) ────────

        [Fact]
        public async Task CastResult_AsyncPath_Canceled()
        {
            var method = GetCastResult().MakeGenericMethod(typeof(string), typeof(object));
            var tcs = new TaskCompletionSource<string>();

            var castTask = (Task<object>)method.Invoke(null, new object[] { tcs.Task })!;
            tcs.SetCanceled();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => castTask);
        }

        // ── Async path: Faulted (OnTaskCompleted SetException) ────────

        [Fact]
        public async Task CastResult_AsyncPath_Faulted()
        {
            var method = GetCastResult().MakeGenericMethod(typeof(string), typeof(object));
            var tcs = new TaskCompletionSource<string>();

            var castTask = (Task<object>)method.Invoke(null, new object[] { tcs.Task })!;
            tcs.SetException(new InvalidOperationException("async error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => castTask);
        }
    }
}
#endif
