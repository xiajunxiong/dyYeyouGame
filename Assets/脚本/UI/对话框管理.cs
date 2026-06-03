using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class 对话框管理 : MonoBehaviour
{
    private GameObject 控制显示节点;
    private Button 背景;
    private Button 点击继续;
    private Image 立绘;
    private Text 标题;
    private Text 内容;
    public static 对话框管理 Instance;
    private int 对话索引 = 0;
    private List<对话框> 当前对话框数据;
    private UnityEvent 回调方法;
    public bool 是否开启对话冻结时间 = true;
    private void Awake()
    {
        Instance = this;
        控制显示节点 = transform.Find("控制显示").gameObject;
        控制显示节点.SetActive(false);
        背景 = transform.Find("控制显示/背景").GetComponent<Button>();
        点击继续 = transform.Find("控制显示/点击继续").GetComponent<Button>();
        标题 = transform.Find("控制显示/标题").GetComponent<Text>();
        内容 = transform.Find("控制显示/内容").GetComponent<Text>();
        立绘 = transform.Find("控制显示/立绘").GetComponent<Image>();
        点击继续.onClick.AddListener(点击继续函数);
        背景.onClick.AddListener(点击继续函数);
    }
    /// <summary>
    /// 对话数据、完成对话回调方法
    /// </summary>
    /// <param name="对话框数据"></param>
    /// <param name="回调方法"></param>
    public void 开始对话(List<对话框> 对话框数据, UnityEvent 回调方法)
    {
        this.回调方法 = 回调方法 ?? null;
        控制显示节点.SetActive(true);
        当前对话框数据 = 对话框数据;
        int 索引 = 对话索引;
        string 标题文本 = 当前对话框数据[索引].标题;
        string 内容文本 = 当前对话框数据[索引].内容;
        Sprite 对话框数据立绘 = 当前对话框数据[索引].立绘;
        对话索引++;
        立绘.color = 对话框数据立绘 == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        立绘.sprite = 对话框数据立绘;
        标题.text = 标题文本 ?? "";
        内容.text = 内容文本 ?? "";
        声音处理.Instance.每次播放一条(当前对话框数据[索引].音频);
        if (!是否开启对话冻结时间) return;
        Time.timeScale = 0;
    }

    public void 开始对话(对话框数据合集 合集)
    {
        控制显示节点.SetActive(true);
        当前对话框数据 = 合集.合集.Select(d => new 对话框()
        {
            标题 = d.标题,
            内容 = d.内容,
            立绘 = d.立绘,
            音频 = d.音频
        }).ToList();

        int 索引 = 对话索引;
        string 标题文本 = 当前对话框数据[索引].标题;
        string 内容文本 = 当前对话框数据[索引].内容;
        Sprite 对话框数据立绘 = 当前对话框数据[索引].立绘;
        对话索引++;
        立绘.color = 对话框数据立绘 == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        立绘.sprite = 对话框数据立绘;
        标题.text = 标题文本 ?? "";
        内容.text = 内容文本 ?? "";
        声音处理.Instance.每次播放一条(当前对话框数据[索引].音频);
        if (!是否开启对话冻结时间) return;
        Time.timeScale = 0;
    }

    public void 开始对话(对话框数据合集 合集,UnityEvent 回调方法)
    {
        this.回调方法 = 回调方法 ?? null;
        控制显示节点.SetActive(true);
        当前对话框数据 = 合集.合集.Select(d => new 对话框()
        {
            标题 = d.标题,
            内容 = d.内容,
            立绘 = d.立绘,
            音频 = d.音频
        }).ToList();

        int 索引 = 对话索引;
        string 标题文本 = 当前对话框数据[索引].标题;
        string 内容文本 = 当前对话框数据[索引].内容;
        Sprite 对话框数据立绘 = 当前对话框数据[索引].立绘;
        对话索引++;
        立绘.color = 对话框数据立绘 == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        立绘.sprite = 对话框数据立绘;
        标题.text = 标题文本 ?? "";
        内容.text = 内容文本 ?? "";
        声音处理.Instance.每次播放一条(当前对话框数据[索引].音频);
        if (!是否开启对话冻结时间) return;
        Time.timeScale = 0;
    }

    public void 开始对话(对话框 合集)
    {
        控制显示节点.SetActive(true);

        Sprite 对话框数据立绘 = 合集.立绘;
        立绘.color = 对话框数据立绘 == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        立绘.sprite = 对话框数据立绘;
        标题.text = 合集.标题 ?? "";
        内容.text = 合集.内容 ?? "";
        声音处理.Instance.每次播放一条(合集.音频);
        if (!是否开启对话冻结时间) return;
        Time.timeScale = 0;
    }

    public void 开始对话(对话框 合集,UnityEvent 回调方法)
    {
        this.回调方法 = 回调方法 ?? null;
        控制显示节点.SetActive(true);

        Sprite 对话框数据立绘 = 合集.立绘;
        立绘.color = 对话框数据立绘 == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        立绘.sprite = 对话框数据立绘;
        标题.text = 合集.标题 ?? "";
        内容.text = 合集.内容 ?? "";
        声音处理.Instance.每次播放一条(合集.音频);
        if (!是否开启对话冻结时间) return;
        Time.timeScale = 0;
    }

    public void 点击继续函数()
    {
        if (当前对话框数据 == null || 当前对话框数据?.Count <= 0 || 对话索引 >= 当前对话框数据?.Count)
        {
            结束对话();
            return;
        }
        int 索引 = 对话索引;
        string 标题文本 = 当前对话框数据[索引].标题;
        string 内容文本 = 当前对话框数据[索引].内容;
        对话索引++;
        立绘.sprite = 当前对话框数据[索引].立绘;
        标题.text = 标题文本;
        内容.text = 内容文本;
        声音处理.Instance.每次播放一条(当前对话框数据[索引].音频);
    }

    private void 结束对话()
    {
        当前对话框数据?.Clear();
        对话索引 = 0;
        回调方法?.Invoke();
        回调方法 = null;
        控制显示节点?.SetActive(false);
        if (!是否开启对话冻结时间) return;
        Time.timeScale = 1;
    }
}

[System.Serializable]
public class 对话框
{
    public string 标题;
    public string 内容;
    public Sprite 立绘;
    public AudioClip 音频;
}