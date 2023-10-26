using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimerData
{
    public string timerKey = string.Empty;
    public float curTime = 0;
    public float endTime = 0;
    public Action endCallback = null;
    public bool isEnd = false;
    public bool isRepeat = false;

    public TimerData(string timerKey, float endTime, Action endCallback, bool isRepeat = false)
    {
        curTime = 0;
        isEnd = false;

        this.timerKey = timerKey;
        this.endTime = endTime;
        this.endCallback = endCallback;
        this.isRepeat = isRepeat;
    }

    public bool IsTimeOver() => endTime <= curTime && isRepeat;
}

public class TimerManager : MonoSingleton<TimerManager>
{
    private readonly Dictionary<string, TimerData> _dicTimer = new Dictionary<string, TimerData>();


    protected override void Destroy()
    {
        _dicTimer.Clear();
    }

    public override bool Initialize()
    {
        return true;
    }

    private void Update()
    {
        foreach (var timer in _dicTimer.Values)
        {
            if(timer is null || timer.isEnd)
                continue;

            timer.curTime += Time.deltaTime;

            //  시간 오버
            if (timer.IsTimeOver())
            {
                timer.endCallback?.Invoke();
                timer.isEnd = true;
            }

            //  반복의 경우
            if (timer.isEnd && timer.isRepeat)
            {
                timer.curTime = 0;
                timer.isEnd = false;
            }
        }

        var list = _dicTimer.Values
            .Where(x=>x.isEnd)
            .Select(x=>x.timerKey);
        foreach (var timerKey in list)
        {
            _dicTimer.Remove(timerKey);
        }
    }

    /// <summary>
    /// 타이머 추가
    /// </summary>
    /// <param name="timerData"></param>
    public void InsertTimer(TimerData timerData)
    {
        if (_dicTimer.ContainsKey(timerData.timerKey) == false)
        {
            _dicTimer.Add(timerData.timerKey, timerData);;
        }
    }

    /// <summary>
    /// 타이머 삭제
    /// </summary>
    /// <param name="timerKey"></param>
    public void RemoveTimer(string timerKey)
    {
        if (_dicTimer.ContainsKey(timerKey))
        {
            _dicTimer.Remove(timerKey);
        }
    }
}
