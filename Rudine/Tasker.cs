using System;

namespace Rudine
{
    internal static class Tasker
    {
        public static void StartNewTask<TResult>(Func<TResult> a)
        {
#if TASKS_OFF
            a.Invoke();
#else

            Task.Factory.StartNew(
                a,
                CancellationToken.None,
                TaskCreationOptions.PreferFairness,
                TaskScheduler.Default);
#endif
        }
    }
}