using System;
using System.Threading.Tasks;

namespace Dapper
{
    internal static class Extensions
    {
        /// <summary>
        /// Creates a <see cref="Task{TResult}"/> with a less specific generic parameter that perfectly mirrors the
        /// state of the specified <paramref name="task"/>.
        /// </summary>
        internal static Task<TTo> CastResult<TFrom, TTo>(this Task<TFrom> task)
            where TFrom : TTo
        {
            if (task is null) throw new ArgumentNullException(nameof(task));

            if (task.Status == TaskStatus.RanToCompletion)
                return Task.FromResult((TTo)task.Result);

            var source = new TaskCompletionSource<TTo>();
            task.ContinueWith(OnTaskCompleted<TFrom, TTo>, state: source, TaskContinuationOptions.ExecuteSynchronously);
            return source.Task;
        }

        private static void OnTaskCompleted<TFrom, TTo>(Task<TFrom> completedTask, object state)
            where TFrom : TTo
        {
            var source = (TaskCompletionSource<TTo>)state;

            switch (completedTask.Status)
            {
                case TaskStatus.RanToCompletion:
                    source.SetResult(completedTask.Result);
                    break;
                case TaskStatus.Canceled:
                    source.SetCanceled();
                    break;
                case TaskStatus.Faulted:
                    source.SetException(completedTask.Exception.InnerExceptions);
                    break;
            }
        }
    }
}
