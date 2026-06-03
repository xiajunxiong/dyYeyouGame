using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum 使用对话框类型
{
    开局激活,
    被调用
}

public enum 对话框类型
{
    单条,
    合集
}

public class 使用对话框 : MonoBehaviour
{
    public 使用对话框类型 使用类型;
    public 对话框类型 对话框类型;
    public 对话框 对话;
    public 对话框数据合集 对话数据合集;
    public UnityEvent 结束回调;
    void Start()
    {
        switch(使用类型)
        {
            case 使用对话框类型.开局激活:
                
                switch(对话框类型)
                {
                    case 对话框类型.单条:
                        开始对话(对话, 结束回调);
                        break;
                    case 对话框类型.合集:
                        开始对话(对话数据合集, 结束回调);
                        break;
                }
                break;
            case 使用对话框类型.被调用:
                
                break;
        }
    }

    public void 开始对话调用节点上结束事件(对话框数据合集 对话数据合集) => 对话框管理.Instance.开始对话(对话数据合集);
    public void 开始对话调用节点上结束事件(对话框 对话) => 对话框管理.Instance.开始对话(对话);
    public void 开始对话(对话框 对话) => 对话框管理.Instance.开始对话(对话);
    public void 开始对话(对话框数据合集 对话数据合集) => 对话框管理.Instance.开始对话(对话数据合集);
    public void 开始对话(对话框 对话, UnityEvent 结束回调) => 对话框管理.Instance.开始对话(对话, 结束回调);
    public void 开始对话(对话框数据合集 对话数据合集, UnityEvent 结束回调) => 对话框管理.Instance.开始对话(对话数据合集, 结束回调);
    public void 销毁自生防止重复触发事件() => Destroy(gameObject);
}
