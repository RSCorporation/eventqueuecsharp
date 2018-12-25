using System;
using System.Threading;
using System.Collections.Generic;
namespace eventqueue
{
    internal class EventTimePair : IComparable, IComparable<EventTimePair>
    {
        internal EventTimePair(TimerCallback action, object state, DateTime time, Guid guid)
        {
            this.Action = action ?? throw new ArgumentNullException(nameof(action));
            this.Time = time;
            this.State = state;
            this.Guid = guid;
        }

        internal TimerCallback Action { get; }
        internal DateTime Time { get; }
        internal object State { get; }
        internal Guid Guid { get; }

        public int CompareTo(EventTimePair other)
        {
            return (other.Time - this.Time).Milliseconds;
        }

        int IComparable.CompareTo(object obj)
        {
            if (!(obj is EventTimePair)) throw new ArgumentException("Object can be only EventTimePair", nameof(obj));
            return this.CompareTo((EventTimePair)obj);
        }
    }
    public class EventQueue
    {
        public EventQueue()
        {
            timer = new Timer(Callback, set, Timeout.Infinite, Timeout.Infinite);
            set = new SortedSet<EventTimePair>();
        }

        private Timer timer;
        private SortedSet<EventTimePair> set;
        private SortedSet<Guid> removedEvents;
        public Guid AddEvent(TimerCallback action, object state, DateTime time)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (time < DateTime.Now) throw new ArgumentException("time can not be before now", nameof(time));
            Guid g = Guid.NewGuid();
            lock (set) set.Add(new EventTimePair(action, state, time, g));
            if (time < set.Min.Time)
            {
                timer.Change((time - DateTime.Now).Milliseconds, Timeout.Infinite);
            }
            return g;
        }
        public void RemoveEvent(Guid guid)
        {
            removedEvents.Add(guid);
        }
        private void Callback(object state)
        {
            SortedSet<EventTimePair> curr = (SortedSet<EventTimePair>)state;
            while (curr.Count > 0 && removedEvents.Contains(curr.Min.Guid)) curr.Remove(curr.Min);
            if (curr.Count == 0) return;
            curr.Min.Action.Invoke(curr.Min.State);
            lock(curr) curr.Remove(curr.Min);

            timer.Change((curr.Min.Time - DateTime.Now).Milliseconds, Timeout.Infinite);
        }
    }
}
