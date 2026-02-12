using UIFramework.Core.Interfaces;

using UnityEngine;

namespace UIFramework.Navigation
{
    public readonly struct NavigationResponse<TWindow> where TWindow : class, IWindow 
    {
        public readonly NavigationResult<TWindow> NavigationResult;
        private readonly Awaitable _completionTask;

        public NavigationResponse(in NavigationResult<TWindow> navigationResult, Awaitable completionTask)
        {
            NavigationResult = navigationResult;
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