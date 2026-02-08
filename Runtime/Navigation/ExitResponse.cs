using UnityEngine;

namespace UIFramework.Navigation
{
    public class ExitResponse
    {
        public readonly bool Success;
        private readonly Awaitable _completionTask;

        public ExitResponse(bool success, Awaitable completionTask)
        {
            Success = success;
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
