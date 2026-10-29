using System;
using System.Collections;

using UnityEngine;

namespace UIFramework.TestUtils
{
    // Captures how an Awaitable finished. [UnityTest] methods return IEnumerator and cannot await, so the
    // result is collected through a continuation and polled by frame instead.
    public sealed class AwaitableProbe
    {
        public bool IsComplete { get; private set; }
        public Exception Exception { get; private set; }

        public static AwaitableProbe Watch(Awaitable awaitable)
        {
            AwaitableProbe probe = new();
            Awaitable.Awaiter awaiter = awaitable.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception exception)
                {
                    probe.Exception = exception;
                }
                probe.IsComplete = true;
            });
            return probe;
        }

        // Bounded so a stalled awaitable fails the test instead of hanging the runner. Bounded by elapsed
        // seconds rather than frames: batchmode renders nothing, so frames are sub-millisecond and a frame
        // budget expires long before a short animation has run.
        public IEnumerator WaitForCompletion(float timeoutSeconds = 10f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!IsComplete && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!IsComplete)
                throw new TimeoutException($"Awaitable did not complete within {timeoutSeconds}s.");
        }
    }
}
