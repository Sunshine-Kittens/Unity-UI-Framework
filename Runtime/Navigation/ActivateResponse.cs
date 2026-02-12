using UIFramework.Core.Interfaces;

using UnityEngine;

namespace UIFramework.Navigation
{
    public readonly struct ActivateResponse<TWindow> where TWindow : class, IWindow 
    {
        public readonly ActivateResult<TWindow> ActivateResult;
        private readonly Awaitable _completionTask;

        public ActivateResponse(in ActivateResult<TWindow> activateResult, Awaitable completionTask)
        {
            ActivateResult = activateResult;
            if (completionTask != null)
            {
                _completionTask = completionTask;    
            }
            else
            {
                AwaitableCompletionSource completionSource = new();
                completionSource.SetResult();
                _completionTask = completionSource.Awaitable;
            }
        }
            
        public Awaitable GetCompletionTask() => _completionTask;
        public Awaitable.Awaiter GetAwaiter() => _completionTask.GetAwaiter();
    }
}
