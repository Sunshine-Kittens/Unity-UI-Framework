using System;
using System.Collections.Generic;

namespace UIFramework.Core
{
    public interface IHistoryEvent { }

    // Non-generic base — allows HistoryEventCollection.Dispose() to call Release()
    // on any pooled event without knowing the closed type.
    public abstract class PooledHistoryEvent : IHistoryEvent
    {
        public abstract void Release();
    }

    // CRTP base for pooled history events. Each closed TSelf gets its own static pool.
    public abstract class PooledHistoryEvent<TSelf> : PooledHistoryEvent
        where TSelf : PooledHistoryEvent<TSelf>, new()
    {
        private static readonly Stack<TSelf> _pool = new();

        protected static TSelf Get()
        {
            TSelf e = _pool.Count > 0 ? _pool.Pop() : new TSelf();
            e.OnGet();
            return e;
        }

        public sealed override void Release()
        {
            OnRelease();
            _pool.Push((TSelf)this);
        }

        protected virtual void OnGet() { }
        protected virtual void OnRelease() { }
    }

    internal static class HistoryEventListPool
    {
        private static readonly Stack<List<IHistoryEvent>> _pool = new();

        internal static List<IHistoryEvent> Get()
            => _pool.Count > 0 ? _pool.Pop() : new List<IHistoryEvent>();

        internal static void Release(List<IHistoryEvent> list)
        {
            list.Clear();
            _pool.Push(list);
        }
    }

    // Owning collection — returned by Pop(). Caller took ownership; use with 'using'.
    // Dispose() releases each pooled event back to its pool and returns the list to
    // HistoryEventListPool.
    public readonly ref struct HistoryEventCollection
    {
        private readonly List<IHistoryEvent> _events;

        internal HistoryEventCollection(List<IHistoryEvent> events) => _events = events;

        public bool TryGetEvent<T>(out T historyEvent) where T : IHistoryEvent
        {
            if (_events != null)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    if (_events[i] is T match)
                    {
                        historyEvent = match;
                        return true;
                    }
                }
            }
            historyEvent = default;
            return false;
        }

        public void Dispose()
        {
            if (_events == null) return;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i] is PooledHistoryEvent pooled)
                    pooled.Release();
            }
            HistoryEventListPool.Release(_events);
        }
    }

    // Non-owning view — returned by TryPeek(). The entry retains ownership.
    // No Dispose() — the absence of it signals there is nothing to release.
    public readonly ref struct HistoryEventView
    {
        private readonly List<IHistoryEvent> _events;

        internal HistoryEventView(List<IHistoryEvent> events) => _events = events;

        public bool TryGetEvent<T>(out T historyEvent) where T : IHistoryEvent
        {
            if (_events != null)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    if (_events[i] is T match)
                    {
                        historyEvent = match;
                        return true;
                    }
                }
            }
            historyEvent = default;
            return false;
        }
    }

    public interface IReadOnlyHistoryEntry
    {
        public Guid ID { get; }
        public IReadOnlyList<IHistoryEvent> Events { get; }
        public bool IsCommited { get; }
        public bool IsCancelled { get; }
    }

    public interface IHistoryEntry : IReadOnlyHistoryEntry
    {
        public void Append(IHistoryEvent historyEvent);
    }

    public interface IReadOnlyHistoryStack
    {
        public int Count { get; }

        public bool TryPeek(out HistoryEventView events);
    }

    public interface IHistoryStack : IReadOnlyHistoryStack
    {
        public IHistoryEntry PushNewEntry();
        public void CommitEntry(Guid guid);
        public void CancelEntry(Guid guid);
        public HistoryEventCollection Pop();
    }

    public interface IHistory : IHistoryStack { }

    // Base class for pooled history entries. Static pool is per closed type — each
    // TSelf gets its own Stack<TSelf> with no extra code required.
    public abstract class PooledHistoryEntry<TSelf> : IHistoryEntry where TSelf : PooledHistoryEntry<TSelf>, new()
    {
        private static readonly Stack<TSelf> _Pool = new();

        public static TSelf Get(Guid guid)
        {
            TSelf entry = _Pool.Count > 0 ? _Pool.Pop() : new TSelf();
            entry.ID = guid;
            entry.StartedTime = DateTime.UtcNow;
            entry.CommitedTime = null;
            entry.CancelledTime = null;
            if (entry._events == null)
                entry._events = HistoryEventListPool.Get();
            else
                entry._events.Clear();
            entry.OnGet();
            return entry;
        }

        public void Release()
        {
            if (_events != null)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    if (_events[i] is PooledHistoryEvent pooled)
                        pooled.Release();
                }
                _events.Clear();
            }
            OnRelease();
            _Pool.Push((TSelf)this);
        }

        protected virtual void OnGet() { }
        protected virtual void OnRelease() { }

        public Guid ID { get; private set; }
        public DateTime StartedTime { get; private set; }
        public DateTime? CommitedTime { get; private set; }
        public DateTime? CancelledTime { get; private set; }
        public bool IsCommited => CommitedTime.HasValue;
        public bool IsCancelled => CancelledTime.HasValue;

        public IReadOnlyList<IHistoryEvent> Events => _events;
        private List<IHistoryEvent> _events;

        public void Append(IHistoryEvent historyEvent)
        {
            if (IsCommited) throw new InvalidOperationException("Unable to append to the entry, it is already finalized.");
            if (IsCancelled) throw new InvalidOperationException("Unable to append to the entry, it is already cancelled.");
            _events.Add(historyEvent);
        }

        internal HistoryEventCollection DetachToCollection()
        {
            List<IHistoryEvent> events = _events;
            _events = null;
            return new HistoryEventCollection(events);
        }

        internal HistoryEventView PeekCollection() => new HistoryEventView(_events);

        internal void Commit()
        {
            if (IsCommited) throw new InvalidOperationException("Unable to finalize the entry, it is already finalized.");
            if (IsCancelled) throw new InvalidOperationException("Unable to finalize the entry, it is already cancelled.");
            CommitedTime = DateTime.UtcNow;
        }

        internal void Cancel()
        {
            if (IsCommited) throw new InvalidOperationException("Unable to cancel the entry, it is already finalized.");
            if (IsCancelled) throw new InvalidOperationException("Unable to cancel the entry, it is already cancelled.");
            CancelledTime = DateTime.UtcNow;
        }
    }

    // Note: This is not thread safe
    public class History : IHistory
    {
        private class HistoryEntry : PooledHistoryEntry<HistoryEntry> { }

        public int Count => _history.Count;

        private readonly List<HistoryEntry> _history;

        public History(int capacity)
        {
            _history = new List<HistoryEntry>(capacity);
        }

        public IHistoryEntry PushNewEntry()
        {
            HistoryEntry entry = HistoryEntry.Get(Guid.NewGuid());
            _history.Add(entry);
            return entry;
        }

        public void CommitEntry(Guid guid)
        {
            if (!FindEntry(guid, out HistoryEntry entry, out _))
                throw new InvalidOperationException("Unable to commit, no valid entry found for guid.");
            entry.Commit();
        }

        public void CancelEntry(Guid guid)
        {
            if (!FindEntry(guid, out HistoryEntry entry, out int index))
                throw new InvalidOperationException("Unable to cancel, no valid entry found for guid.");
            entry.Cancel();
            _history.RemoveAt(index);
            entry.Release();
        }

        public HistoryEventCollection Pop()
        {
            if (_history.Count > 0)
            {
                HistoryEntry entry = _history[^1];
                _history.RemoveAt(_history.Count - 1);
                HistoryEventCollection events = entry.DetachToCollection();
                entry.Release();
                return events;
            }
            throw new InvalidOperationException("The history stack is empty.");
        }

        public bool TryPeek(out HistoryEventView events)
        {
            if (_history.Count > 0)
            {
                events = _history[^1].PeekCollection();
                return true;
            }
            events = default;
            return false;
        }

        public bool Clear()
        {
            if (_history.Count > 0)
            {
                _history.Clear();
                return true;
            }
            return false;
        }

        private bool FindEntry(Guid guid, out HistoryEntry entry, out int entryIndex)
        {
            for (int i = 0; i < _history.Count; i++)
            {
                if (_history[i].ID == guid)
                {
                    entry = _history[i];
                    entryIndex = i;
                    return true;
                }
            }
            entry = null;
            entryIndex = -1;
            return false;
        }
    }
}
