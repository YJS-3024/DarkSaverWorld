#define PRINT_EVENT_LOG

using System;
using System.Collections.Generic;
using TS;
using TS.Utility;
using UnityEngine;

namespace TS
{
    public interface IEvent {}
    public interface IEventHandler {}
    
    public class EventHandler<T> : IEventHandler where T : IEvent
    {
        public delegate void Delegate(T arg);
        private Delegate _handler;

        public void AddListener(Delegate callback)
        {
            if (callback != null)
            {
                _handler += callback;
            }
        }

        public void RemoveListener(Delegate callback)
        {
            if (callback != null && _handler != null)
            {
                _handler -= callback;
            }
        }

        public void Invoke(T arg)
        {
            _handler?.Invoke(arg);
        }

        public void Clear()
        {
            _handler = null;
        }
    }
    
    public class EventManager : Singleton<EventManager>
    {
        private Dictionary<Type, IEventHandler> _handlers = new Dictionary<Type, IEventHandler>();
        
        public void AddListener<T>(EventHandler<T>.Delegate callback, object receiver = null) where T : IEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var h) == false)
            {
                h = new EventHandler<T>();
                _handlers.Add(type, h);
            }

            var handler = (EventHandler<T>) h;
            handler.AddListener(callback);
        }

        public void RemoveListener<T>(EventHandler<T>.Delegate callback, object receiver = null) where T : IEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var h))
            {
                var handler = (EventHandler<T>) h;
                handler.RemoveListener(callback);
            }
        }

        public void Invoke<T>(T arg, object sender = null) where T : IEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var h) == false)
            {
                h = new EventHandler<T>();
                _handlers.Add(type, h);
            }

            var handler = (EventHandler<T>) h;
            handler.Invoke(arg);
        }

        public void Clear()
        {
            _handlers.Clear(); 
        }
    }
}
