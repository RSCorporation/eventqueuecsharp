using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynNetLib.Util
{
    internal static class EventQueue
    {
        private class EventQueueEvent : IComparable<EventQueueEvent>
        {
            public readonly TimerCallback Action;
            public readonly DateTime CallTime;
            public readonly object State;

            public EventQueueEvent(TimerCallback action, object state, DateTime callTime)
            {
                Action = action;
                CallTime = callTime;
                State = state;
            }

            public int CompareTo(EventQueueEvent other)
            {
                if (this == other)
                    return 0;
                return CallTime < other.CallTime ? -1 : 1;
            }
        }

        private static readonly Timer timer = new Timer(Callback);
        private static readonly SortedSet<EventQueueEvent> events = new SortedSet<EventQueueEvent>();
        private static readonly ConcurrentDictionary<Guid, EventQueueEvent> eventById = new ConcurrentDictionary<Guid, EventQueueEvent>();

        public static Guid AddEvent(TimerCallback action, object state, DateTime time)
        {
            return AddEvent(action, state, time, Guid.NewGuid());
        }

        public static Guid AddEvent(TimerCallback action, object state, DateTime callTime, Guid newId)
        {
            var newEvent = new EventQueueEvent(action, state, callTime);
            while (!eventById.TryAdd(newId, newEvent))
                Thread.Sleep(1);
            lock (events)
            {
                events.Add(newEvent);
                timer.Change(ClampTimeSpan(events.Min.CallTime - DateTime.Now), Timeout.InfiniteTimeSpan);
            }
            return newId;
        }

        public static void RemoveEvent(Guid id)
        {
            if (!eventById.ContainsKey(id))
                return;
            EventQueueEvent idEvent;
            while (!eventById.TryRemove(id, out idEvent)) Thread.Sleep(1);
            lock (events)
            {
                events.Remove(idEvent);
                timer.Change(events.Count > 0 ? ClampTimeSpan(events.Min.CallTime - DateTime.Now) : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        private class InvokeStateObject
        {
            public TimerCallback TimerCallback;
        }

        private static TimeSpan ClampTimeSpan(TimeSpan original)
        {
            return original.TotalMilliseconds < 0 ? TimeSpan.Zero : original;
        }

        private static void Callback(object state)
        {
            EventQueueEvent minEvent;

            lock (events)
            {
                minEvent = events.Min;
                events.Remove(minEvent);
                timer.Change(events.Count > 0 ? ClampTimeSpan(events.Min.CallTime - DateTime.Now) : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            minEvent.Action.BeginInvoke(minEvent.State, ActionInvokeCallback, new InvokeStateObject
            {
                TimerCallback = minEvent.Action
            });
        }

        private static void ActionInvokeCallback(IAsyncResult asyncResult)
        {
            ((InvokeStateObject)asyncResult.AsyncState).TimerCallback.EndInvoke(asyncResult);
        }
    }
}
