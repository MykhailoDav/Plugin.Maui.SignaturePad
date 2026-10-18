using System.Runtime.CompilerServices;

namespace Plugin.Maui.SignaturePad.Tests.TestHelpers;

/// <summary>
/// Lets MAUI controls be created without a running app: a <c>BindableObject</c> needs a dispatcher for the current
/// thread, and the test host has none, so a synchronous one is registered before any test runs.
/// </summary>
internal static class MauiTestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize() => DispatcherProvider.SetCurrent(new SyncDispatcherProvider());

    private sealed class SyncDispatcherProvider : IDispatcherProvider
    {
        public IDispatcher GetForCurrentThread() => SyncDispatcher.Instance;
    }

    private sealed class SyncDispatcher : IDispatcher
    {
        public static readonly SyncDispatcher Instance = new();

        public bool IsDispatchRequired => false;

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

        public IDispatcherTimer CreateTimer() => throw new NotSupportedException("The tests use no timers.");
    }
}
