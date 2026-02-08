using System;
using System.Collections.Generic;

namespace UIFramework
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
    
    public interface IHistoryGroups
    {
        public void AddNewGroup();
        public bool ClearActiveGroup();
        public bool Clear();
    }
    
    public interface IHistory : IHistoryStack, IHistoryGroups
    {
        
    }
    
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

        public int Count { get; private set; }

        private readonly List<List<HistoryEntry>> _history = new();
        private readonly int _groupCapacity = 0;
        
        public History(int capacity, int groupCapacity = 0)
        {
            _history.Add(new List<HistoryEntry>(capacity));
            _groupCapacity = groupCapacity;
        }

        public IHistoryEntry PushNewEntry()
        {
            HistoryEntry entry = new HistoryEntry(Guid.NewGuid());
            _history[^1].Add(entry);
            Count++;
            return entry;
        }

        public void CommitEntry(Guid guid)
        {
            if (!FindEntry(guid, out HistoryEntry entry, out _, out _))
                throw new InvalidOperationException("Unable to commit, no valid entry found for guid.");
            entry.Commit();
        }
        
        public void CancelEntry(Guid guid)
        {
            if (!FindEntry(guid, out HistoryEntry entry, out int groupIndex, out int entryIndex))
                throw new InvalidOperationException("Unable to cancel, no valid entry found for guid.");
            entry.Cancel();
            List<HistoryEntry> group = _history[^1];
            group.RemoveAt(groupIndex);
            _history[groupIndex].RemoveAt(entryIndex);
            Count--;
            if (group.Count == 0 && _history.Count > 1)
                _history.RemoveAt(groupIndex);
        }
        
        public IHistoryEntry Pop()
        {
            if (_history[^1].Count > 0)
            {
                Count--;
                List<HistoryEntry> activeGroup = _history[^1];
                HistoryEntry entry = activeGroup[^1];
                activeGroup.RemoveAt(activeGroup.Count - 1);
                if (activeGroup.Count == 0 && _history.Count > 1)
                {
                    _history.RemoveAt(_history.Count - 1);
                }
                return entry;
            }
            throw new InvalidOperationException("The history stack is empty.");
        }

        public IHistoryEntry Peek()
        {
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                List<HistoryEntry> group = _history[i];
                if (group.Count > 0)
                {
                    return group[^1];
                }
            }
            return null;
        }

        public void AddNewGroup()
        {
            _history.Add(new List<HistoryEntry>(_groupCapacity));
        }

        public bool ClearActiveGroup()
        {
            if (_history.Count > 1)
            {
                Count -= _history[^1].Count;
                _history.RemoveAt(_history.Count - 1);
                return true;
            }
            return false;
        }

        public bool Clear()
        {
            if (Count > 0)
            {
                if (_history.Count > 1)
                {
                    _history.RemoveRange(1, _history.Count - 1);
                }
                _history[0].Clear();
                Count = 0;
                return true;
            }
            return false;
        }

        private bool FindEntry(Guid guid, out HistoryEntry entry, out int groupIndex, out int entryIndex)
        {
            for (int i = 0; i < _history.Count; i++)
            {
                List<HistoryEntry> group = _history[i];
                for (int j = 0; j < group.Count; j++)
                {
                    if (group[j].ID == guid)
                    {
                        entry = group[j];
                        groupIndex = i;
                        entryIndex = j;
                        return true;
                    }
                }
            }
            entry = null;
            groupIndex = -1;
            entryIndex = -1;
            return false;
        }
    }
}