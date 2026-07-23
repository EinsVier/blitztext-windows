using System.Diagnostics.CodeAnalysis;

namespace BlitzText.Windows.Services;

internal sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = @"Local\BlitzText.Windows.SingleInstance";
    private const string ActivationEventName = @"Local\BlitzText.Windows.Activate";

    private readonly Mutex mutex;
    private readonly EventWaitHandle activationEvent;
    private RegisteredWaitHandle? activationRegistration;
    private bool isDisposed;

    private SingleInstanceManager(Mutex mutex, EventWaitHandle activationEvent)
    {
        this.mutex = mutex;
        this.activationEvent = activationEvent;
    }

    public static bool TryAcquire([NotNullWhen(true)] out SingleInstanceManager? manager)
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            SignalExistingInstance();
            manager = null;
            return false;
        }

        var activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ActivationEventName);
        manager = new SingleInstanceManager(mutex, activationEvent);
        return true;
    }

    public void RegisterActivationHandler(Action activationHandler)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(activationHandler);

        activationRegistration = ThreadPool.RegisterWaitForSingleObject(
            activationEvent,
            (_, _) => activationHandler(),
            state: null,
            millisecondsTimeOutInterval: Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        activationRegistration?.Unregister(waitObject: null);
        activationEvent.Dispose();
        mutex.ReleaseMutex();
        mutex.Dispose();
    }

    private static void SignalExistingInstance()
    {
        using var activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ActivationEventName);
        activationEvent.Set();
    }
}
