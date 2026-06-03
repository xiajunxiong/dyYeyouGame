using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 全局UI管理 : MonoBehaviour
{
    public string UI加载主路径 = "UI";
    public string 提示UI路径 = "提示UI";
    private GameObject 按键提示;
    private GameObject 黑屏;
    private Text 内容;
    private Button 按钮;
    private Text 按钮文本;
    public static 全局UI管理 Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        按键提示 = transform.Find("按键提示").gameObject;
        黑屏 = transform.Find("黑屏").gameObject;
        内容 = transform.Find("黑屏/内容").GetComponent<Text>();
        按钮 = transform.Find("黑屏/按钮").GetComponent<Button>();
        按钮文本 = transform.Find("黑屏/按钮/按钮文本").GetComponent<Text>();
    }

    #region 黑屏UI
    public void 黑屏显示(string 显示内容, string 按钮文本内容, System.Action 按钮回调)
    {
        黑屏.SetActive(true);
        内容.text = 显示内容;
        按钮文本.text = 按钮文本内容;
        按钮.onClick.RemoveAllListeners();
        按钮.onClick.AddListener(() =>
        {
            按钮回调?.Invoke();
            黑屏.SetActive(false);
        });
    }

    #endregion

    #region 提示UI管理
    public void 显示按键提示(string 提示内容)
    {
        按键提示.SetActive(true);
        Text 文本 = 按键提示.transform.Find("Text (Legacy)").GetComponent<Text>();
        文本.text = 提示内容;
    }

    public void 隐藏按键提示()
    {
        按键提示.SetActive(false);
    }
    #endregion
    #region 提示UI队列管理
    private Queue<提示信息> 提示队列 = new Queue<提示信息>();
    private bool 正在处理队列 = false;
    public float 提示间隔时间 = 0.5f;
    private struct 提示信息
    {
        public string 内容;
        public Color 颜色;
        public 提示信息(string 内容, Color 颜色)
        {
            this.内容 = 内容;
            this.颜色 = 颜色;
        }
    }

    public void 显示提示UI(string 提示内容, Color 提示颜色 = default)
    {
        if (提示颜色 == default)
        {
            提示颜色 = Color.black;
        }
        提示队列.Enqueue(new 提示信息(提示内容, 提示颜色));
        if (!正在处理队列)
        {
            StartCoroutine(处理提示队列());
        }
    }
    private IEnumerator 处理提示队列()
    {
        正在处理队列 = true;

        while (提示队列.Count > 0)
        {
            提示信息 当前提示 = 提示队列.Dequeue();

            显示单个提示UI(当前提示.内容, 当前提示.颜色);

            yield return new WaitForSeconds(提示间隔时间);
        }
        正在处理队列 = false;
    }

    private void 显示单个提示UI(string 提示内容, Color 提示颜色)
    {
        try
        {
            GameObject 提示UI对象 = 获取对应对象(提示UI路径);
            if (提示UI对象 == null)
            {
                Debug.LogError("提示UI预制体未找到，请检查路径：" + 提示UI路径);
                return;
            }

            Text 文本 = 提示UI对象.transform.Find("Text (Legacy)").GetComponent<Text>();
            if (文本 == null)
            {
                Debug.LogError("提示UI中未找到Text (Legacy)组件");
                return;
            }
            文本.text = 提示内容;
            文本.color = 提示颜色;
            Destroy(提示UI对象, 2f);
        }
        catch (System.Exception e)
        {
            Debug.LogError("显示提示UI失败：" + e.Message);
        }
    }

    #endregion



    private GameObject 获取对应对象(string 路径)
    {
        GameObject prefab = Resources.Load<GameObject>($"{UI加载主路径}/{路径}");
        return Instantiate(prefab, transform);
    }
}
