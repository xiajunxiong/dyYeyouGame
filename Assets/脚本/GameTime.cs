using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TimeState
{
    // 等待
    Waiting,
    Day,
    Night
}

public class GameTime : MonoBehaviour
{
    public event Action<TimeState> OnTimeStateChanged;
    public float dayTime = 0f;
    public float nightTime= 0f;
    public float nowTime = 0f;
    public Text dayTimeText;
    public Text nightTimeText;
    public int GameDays = 0;
    // 是否循环计时
    public bool isLooping = true;
    public Enemy enemy;
    public TimeState timeState = TimeState.Waiting;
    public bool isTimePaused = false;
    public static GameTime ins;
    private Coroutine dayCoroutine;
    private Coroutine nightCoroutine;
    private void Awake()
    {
        ins = this;
        dayTime = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.lobbyWaitCountdown;
        nightTime = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.baseNightEndCountdown;
        //StartDayTime();
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 开始白天计时
    public void StartDayTime()
    {
        dayCoroutine = StartCoroutine(DayTimeCountdown());
    }

    // 白天倒计时协程
    public IEnumerator DayTimeCountdown()
    {
        dayTimeText.gameObject.SetActive(true);
        nowTime = dayTime;
        timeState = TimeState.Day;
        OnTimeStateChanged?.Invoke(timeState);
        while (nowTime > 0)
        {
            if (isTimePaused)
            {
                yield return null;
                continue;
            }

            dayTimeText.text = $"白天时间：{nowTime}秒";
            yield return new WaitForSeconds(1f);
            nowTime -= 1f;
        }
        dayTimeText.gameObject.SetActive(false);
        StartNightTime();
    }

    public void StartNightTime()
    {
        nightCoroutine = StartCoroutine(NightTimeCountdown());
    }
    public IEnumerator NightTimeCountdown()
    {
        nightTimeText.gameObject.SetActive(true);
        enemy.startAttack();
        ResetNightTime();
        nowTime = nightTime;
        timeState = TimeState.Night;
        OnTimeStateChanged?.Invoke(timeState);
        while (nowTime > 0)
        {
            if (isTimePaused)
            {
                yield return null;
                continue;
            }

            nightTimeText.text = $"黑夜时间：{nowTime}秒";
            yield return new WaitForSeconds(1f);
            nowTime -= 1f;
        }
        nightTimeText.gameObject.SetActive(false);
        
        GameDays++;
        if (isLooping)
        {
            StartDayTime();
        }
        enemy.StopAttack();
    }

    // 重置黑夜时间
    private void ResetNightTime()
    {
        nightTime = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.baseNightEndCountdown;

        foreach(var r in RoomManager.ins.room)
        {
            foreach(var p in r.player)
            {
                nightTime++;
            }
        }
    }
    // 重置游戏
    public void ResetGame()
    {
        if(dayCoroutine != null)
            StopCoroutine(dayCoroutine);
        if(nightCoroutine != null)
            StopCoroutine(nightCoroutine);
        GameDays = 0;
        timeState = TimeState.Waiting;
        OnTimeStateChanged?.Invoke(timeState);
        isTimePaused = false;
        dayTimeText.gameObject.SetActive(false);
        nightTimeText.gameObject.SetActive(false);
        生存天数排行._instance.重置();
        //礼物排行榜.ins.重置礼物排行榜数据();
        生存排行榜.ins.重置排行榜();
        RoomManager.ins.ResetRooms();
        enemy.ResetEnemy();
    }




    public void AddNightTime()
    {
        //if (timeState == TimeState.Day)
        //{
        //    nightTime += DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.extraNightDurationPerPerson;
        //}
        //else
        //{
        //    nowTime += DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.extraNightDurationPerPerson;
        //    nightTimeText.text = $"黑夜时间：{nowTime}秒";
        //}
        
    }
}
