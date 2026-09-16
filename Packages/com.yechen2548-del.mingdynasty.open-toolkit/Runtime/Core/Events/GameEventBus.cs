using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IGameEvent
    {
    }

    public interface IGameEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent;
        void Publish<TEvent>(TEvent gameEvent) where TEvent : IGameEvent;
        void Clear();
    }

    /// <summary>
    /// Instance-scoped event bus. It is intentionally not static so tests and game sessions are isolated.
    /// </summary>
    public sealed class GameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(TEvent);
            List<Delegate> eventHandlers;
            if (!handlers.TryGetValue(eventType, out eventHandlers))
            {
                eventHandlers = new List<Delegate>();
                handlers.Add(eventType, eventHandlers);
            }

            eventHandlers.Add(handler);
            return new Subscription<TEvent>(this, handler);
        }

        public void Publish<TEvent>(TEvent gameEvent) where TEvent : IGameEvent
        {
            if (ReferenceEquals(gameEvent, null))
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            List<Delegate> eventHandlers;
            if (!handlers.TryGetValue(typeof(TEvent), out eventHandlers))
            {
                return;
            }

            Delegate[] snapshot = eventHandlers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                ((Action<TEvent>)snapshot[i])(gameEvent);
            }
        }

        public void Clear()
        {
            handlers.Clear();
        }

        private void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            List<Delegate> eventHandlers;
            if (!handlers.TryGetValue(typeof(TEvent), out eventHandlers))
            {
                return;
            }

            eventHandlers.Remove(handler);
            if (eventHandlers.Count == 0)
            {
                handlers.Remove(typeof(TEvent));
            }
        }

        private sealed class Subscription<TEvent> : IDisposable where TEvent : IGameEvent
        {
            private readonly GameEventBus owner;
            private Action<TEvent> handler;

            public Subscription(GameEventBus owner, Action<TEvent> handler)
            {
                this.owner = owner;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (handler == null)
                {
                    return;
                }

                owner.Unsubscribe(handler);
                handler = null;
            }
        }
    }
}
