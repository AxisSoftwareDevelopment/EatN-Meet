

namespace eatMeet.Utilities
{
    public static class DebounceHelper
    {
        public static Action<T> Debounce<T>(Action<T> action, int milliseconds = 300)
        {
            CancellationTokenSource? cancelTokenSource = null;

            return arg =>
            {
                cancelTokenSource?.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                Task.Delay(milliseconds, cancelTokenSource.Token)
                    .ContinueWith(task =>
                    {
                        if (!task.IsCanceled)
                        {
                            action(arg);
                        }
                    }, TaskScheduler.Default);
            };
        }
    }
}
