
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();
    private readonly object _lockObj = new object();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                // 查找场景中是否已有该组件
                _instance = FindObjectOfType<MainThreadDispatcher>();
                if (_instance == null)
                {
                    // 没有则创建新的GameObject并挂载
                    GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
                    _instance = dispatcherObj.AddComponent<MainThreadDispatcher>();
                    // 标记为切换场景不销毁
                    DontDestroyOnLoad(dispatcherObj);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // 防止重复创建
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 每帧检查并执行主线程任务
        lock (_lockObj)
        {
            while (_mainThreadActions.Count > 0)
            {
                Action action = _mainThreadActions.Dequeue();
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"主线程执行任务出错: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }

    /// <summary>
    /// 将任务投递到主线程执行
    /// </summary>
    /// <param name="action">需要在主线程执行的代码逻辑</param>
    public void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_lockObj)
        {
            _mainThreadActions.Enqueue(action);
        }
    }

    /// <summary>
    /// 静态方法：快速投递任务到主线程
    /// </summary>
    public static void RunOnMainThread(Action action)
    {
        if (Thread.CurrentThread.ManagedThreadId == 1)
        {
            // 已经是主线程，直接执行
            action.Invoke();
        }
        else
        {
            // 子线程，投递到主线程
            Instance.Enqueue(action);
        }
    }
}