using System;
using System.Collections.Generic;

namespace UIFramework.Core
{
    public interface IHistoryEvent { }

    public interface IReadOnlyHistoryEntry
    {
        public Guid ID { get; }
        public IReadOnlyList<IHistoryEvent> Events { get; }
        public bool IsCommited { get; }
        public bool IsCancelled { get; }
        
        public bool TryGetEvent<T>(out T historyEvent) where T : IHistoryEvent;
    }
    
    public interface IHistoryEntry : IReadOnlyHistoryEntry
    {
        public void Append(IHistoryEvent historyEvent);
    }

    public interface IReadOnlyHistoryStack
    {
        public int Count { get; }
        
        public IHistoryEntry Peek();
    }

    public interface IHistoryStack : IReadOnlyHistoryStack
    {
        public IHistoryEntry PushNewEntry();
        public void CommitEntry(Guid guid);
        public void CancelEntry(Guid guid);
        public IHistoryEntry Pop();
    }
    
    public interface IHistory : IHistoryStack { }
    
    // Note: This is not thread safe
    public class History : IHistory
    {
        private class HistoryEntry : IHistoryEntry
        {
            public Guid ID { get; }
            public readonly DateTime StartedTime;

            public DateTime? CommitedTime { get; private set; }
            public DateTime? CancelledTime { get; private set; }
            public bool IsCommited => CommitedTime.HasValue;
            public bool IsCancelled => CancelledTime.HasValue;

            public IReadOnlyList<IHistoryEvent> Events => _events;
            private readonly List<IHistoryEvent> _events;

            public HistoryEntry(Guid guid)
            {
                ID = guid;
                StartedTime = DateTime.UtcNow;
                _events = new List<IHistoryEvent>();
            }

            public void Append(IHistoryEvent historyEvent)
            {
                if (IsCommited) throw new InvalidOperationException("Unable to append to the entry, it is already finalized.");
                if (IsCancelled) throw new InvalidOperationException("Unable to append to the entry, it is already cancelled.");
                _events.Add(historyEvent);
            }

            public bool TryGetEvent<T>(out T historyEvent) where T : IHistoryEvent
            {
                foreach (IHistoryEvent e in Events)
                {
                    if (e is T @event)
                    {
                        historyEvent = @event;
                        return true;
                    }
                }
                historyEvent = default;
                return false;
            }

            public void Commit()
            {
                if (IsCommited) throw new InvalidOperationException("Unable to finalize the entry, it is already finalized.");
                if (IsCancelled) throw new InvalidOperationException("Unable to finalize the entry, it is already cancelled.");
                CommitedTime = DateTime.UtcNow;
            }

            public void Cancel()
            {
                if (IsCommited) throw new InvalidOperationException("Unable to cancel the entry, it is already finalized.");
                if (IsCancelled) throw new InvalidOperationException("Unable to cancel the entry, it is already cancelled.");
                CancelledTime = DateTime.UtcNow;
            }
        }
        
        public int Count => _history.Count;
        
        private readonly List<HistoryEntry> _history;
        
        public History(int capacity)
        {
            _history = new List<HistoryEntry>(capacity); 
        }

        public IHistoryEntry PushNewEntry()
        {
            HistoryEntry entry = new HistoryEntry(Guid.NewGuid());
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
        }
        
        public IHistoryEntry Pop()
        {
            if (_history.Count > 0)
            {
                HistoryEntry entry = _history[^1];
                _history.RemoveAt(_history.Count - 1);
                return entry;
            }
            throw new InvalidOperationException("The history stack is empty.");
        }

        public IHistoryEntry Peek()
        {
            if (_history.Count > 0)
            {
                return _history[^1];
            }
            return null;
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