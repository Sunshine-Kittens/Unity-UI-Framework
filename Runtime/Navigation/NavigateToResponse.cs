using UIFramework.Core.Interfaces;

using UnityEngine;

namespace UIFramework.Navigation
{
    public readonly struct NavigateToResponse<TWindow> where TWindow : class, IWindow
    {
        public readonly NavigateToResult<TWindow> Result;
        private readonly Awaitable _completionTask;

        public NavigateToResponse(in NavigateToResult<TWindow> result, Awaitable completionTask)
        {
            Result = result;
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