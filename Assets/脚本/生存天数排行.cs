using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class 生存天数排行 : MonoBehaviour
{

    private Text 人数;
    private Text 天数;
    private int 当前天数;
    private int 当前人数;
    public static 生存天数排行 _instance;
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        
    }



    private void OnTimeChanged(TimeState state)
    {
        当前天数 = GameTime.ins.GameDays;
        天数.text = 当前天数.ToString();
    }

    void Start()
    {
        人数 = transform.Find("存活玩家").GetComponent<Text>();
        天数 = transform.Find("生存天数").GetComponent<Text>();
        GameTime.ins.OnTimeStateChanged += OnTimeChanged;
        重置();
    }



    public void 更新人数(int num)
    {
        当前人数 += num;
        人数.text = 当前人数.ToString();
    }

    public void 减少人数(int num)
    {
        当前人数 -= num;
        人数.text = 当前人数.ToString();
    }

    //public void 更新天数(int num)
    //{
    //    当前天数 = num;
    //    天数.text = 当前天数.ToString();
    //}

    public void 重置()
    {
        当前人数 = 0;
        当前天数 = 0;
        人数.text = 当前人数.ToString();
        天数.text = 当前天数.ToString();
    }
}
