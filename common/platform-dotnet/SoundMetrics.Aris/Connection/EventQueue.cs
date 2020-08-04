using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.Aris.Connection
{
    // A queue of event to be handled. This queue knows of
    // "poison letter" events, which force the queue to be flushed
    // in order to speed error handling.
    internal sealed class EventQueue<Event>
    {
        public delegate bool PoisonEventPredicate(Event ev);

        public EventQueue(Func<Event, bool> isPoisonEvent)
        {
            this.isPoisonEvent = isPoisonEvent;
        }
        public void Enqueue(in Event ev)
        {
            lock (guard)
            {
                events.AddLast(ev);
            }
        }

        // Caller is responsible for checking whether a returned event
        // is a poison event.
        public bool TryDequeue(out Event ev)
        {
            lock (guard)
            {
                // Check queue for poison event, remove everyting up to
                // the poison event.
                while (events.Count > 0 && events.Any(isPoisonEvent))
                {
                    var foundPoison = false;
                    do
                    {
                        foundPoison = isPoisonEvent(events.First.Value);
                        if (!foundPoison)
                        {
                            events.RemoveFirst();
                        }
                    } while (!foundPoison);
                }

                if (events.Count > 0)
                {
                    ev = events.First.Value;
                    return true;
                }
                else
                {
                    ev = default;
                    return false;
                }
            }
        }

        private readonly Func<Event, bool> isPoisonEvent;
        private readonly LinkedList<Event> events = new LinkedList<Event>();
        private readonly object guard = new object();
    }
}
