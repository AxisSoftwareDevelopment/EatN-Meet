namespace eatMeet.Utilities
{
    public class DebouncedAction<T>
    {
        private CancellationTokenSource? _cancelTokenSource = null;
        private readonly Func<T, Task> _action;
        private readonly int _delay;

        public DebouncedAction(Func<T, Task> action, int msDelay = 500)
        {
            _action = action;
            _delay = msDelay;
        }

        public async Task Run(T arguments)
        {
            bool bShouldRunTask = true;
//#if ANDROID
            _cancelTokenSource?.Cancel();
            _cancelTokenSource = new CancellationTokenSource();

            try
            {
                Task delayTask = Task.Delay(_delay, _cancelTokenSource.Token);
                await delayTask;
                bShouldRunTask = !delayTask.IsCanceled;
            }
            catch (TaskCanceledException)
            {
                // Expected: ignore, as this is part of debounce logic
                bShouldRunTask = false;
            }
//#endif
            if (bShouldRunTask)
            {
                await _action(arguments);
            }
        }
    }
}
