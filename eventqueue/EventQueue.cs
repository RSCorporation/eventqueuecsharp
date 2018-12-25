using System;
using System.Threading;
using System.Collections.Generic;
namespace eventqueue
{
    internal class EventTimePair : IComparable, IComparable<EventTimePair>
    {
        internal EventTimePair(TimerCallback action, object state, DateTime time)
        {
            this.Action = action ?? throw new ArgumentNullException(nameof(action));
            this.Time = time;
            this.State = state;
        }

        internal TimerCallback Action { get; }
        internal DateTime Time { get; }
        internal object State { get; }

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
        public void AddEvent(TimerCallback action, object state, DateTime time)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (time < DateTime.Now) throw new ArgumentException("time can not be before now", nameof(time));
            lock (set) set.Add(new EventTimePair(action, state, time));
            if (time < set.Min.Time)
            {
                timer.Change((time - DateTime.Now).Milliseconds, Timeout.Infinite);
            }
        }
        private void Callback(object state)
        {
            SortedSet<EventTimePair> curr = (SortedSet<EventTimePair>)state;
            curr.Min.Action.Invoke(curr.Min.State);
            lock(curr) curr.Remove(curr.Min);

            timer.Change((curr.Min.Time - DateTime.Now).Milliseconds, Timeout.Infinite);
        }
    }
}
