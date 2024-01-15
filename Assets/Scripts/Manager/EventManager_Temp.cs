using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoSingleton<EventManager>
{
    private readonly Dictionary<string, Action> _dicEvent = new Dictionary<string, Action>();

    protected override void Destroy()
    {
        _dicEvent.Clear();
    }

    public override bool Initialize()
    {
        return true;
    }

    public void AddEvent(string eventName, Action registerAction)
    {
        if (string.IsNullOrEmpty(eventName))
            return;

        if (_dicEvent.TryGetValue(eventName, out var onCallback))
        {
            return;
        }

        _dicEvent.Add(eventName, registerAction);
    }

    public void RemoveEvent(string eventName)
    {
        if (_dicEvent.ContainsKey(eventName))
        {
            _dicEvent.Remove(eventName);
        }
    }

    public Action CallEvent(string eventName)
    {
        if (_dicEvent.TryGetValue(eventName, out var onCallback))
        {
            return onCallback;
        }

        return null;
    }
}