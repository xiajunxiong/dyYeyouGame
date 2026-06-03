using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlatformValidator : MonoBehaviour
{
    public InputField liveUrl;
    public Text btnText;
    /// <summary>
    /// 直播间url
    /// </summary>
    public string lowerUrl;
    private GameObject befoRerunUIEffects;
    private Process exeProcess;
    private string exePath = "";
    private bool isPlatformStarted = false; // 重命名变量，使其更通用
    public static PlatformValidator ins;

    void Start()
    {
        ins = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Platform();
        }
    }

    public void Platform()
    {

        isPlatformStarted = !isPlatformStarted;

        if (!isPlatformStarted)
        {
            CloseConnection();
            return;
        }

        if (befoRerunUIEffects != null)
            befoRerunUIEffects.SetActive(false);

        var str = CheckPlatform();
    }

    private void CloseConnection()
    {
        if (befoRerunUIEffects != null)
            befoRerunUIEffects.SetActive(false);

        if (exeProcess != null && !exeProcess.HasExited)
            exeProcess.Kill();

        UrlClientManager.Instance.text.text = "=== 已关闭链接 ===";
        btnText.text = "点击链接";
    }

    public string CheckPlatform()
    {
        btnText.text = "关闭链接";

        if (string.IsNullOrEmpty(liveUrl.text))
            return "Invalid URL";


        if (true)
        {
            try
            {
                string rootPath = GetExeDirectory();
                exePath = Path.Combine(rootPath, "douyin", "douyin.exe");

                string id = ExtractRoomId(liveUrl.text);
                string finalUrl = IsDigitsOnly(id) ? "https://live.douyin.com/" + id : liveUrl.text;

                UnityEngine.Debug.Log($"准备启动路径: {exePath}");
                UnityEngine.Debug.Log($"准备传入参数: {finalUrl}");

                exeProcess = Process.Start(exePath, finalUrl);

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"❌ 启动失败：{e.Message}");
                UnityEngine.Debug.LogError(e.StackTrace);
            }

            return "Douyin";
        }

        //return "Unknown Platform";
    }

    public static string GetExeDirectory()
    {
        string dataPath = Application.dataPath;
        DirectoryInfo parentDir = Directory.GetParent(dataPath);

        if (parentDir != null)
        {
            return parentDir.FullName;
        }
        else
        {
            UnityEngine.Debug.LogError("获取exe目录失败，返回默认Data路径");
            return dataPath;
        }
    }

    private string ExtractRoomId(string url)
    {
        if (url.Contains("live.douyin.com/"))
        {
            int index = url.LastIndexOf('/') + 1;
            return url.Substring(index).Trim();
        }
        return url.Trim();
    }

    private bool IsDigitsOnly(string str)
    {
        if (string.IsNullOrEmpty(str)) return false;
        foreach (char c in str)
        {
            if (!char.IsDigit(c)) return false;
        }
        return true;
    }

    // 协程：在后台启动外部进程
    private IEnumerator StartExeCoroutine()
    {
        // 等待一帧确保UI更新完成
        yield return null;

        // 在后台线程中启动进程
        yield return StartCoroutine(ExecuteInBackground(() =>
        {
            if (exeProcess != null && !exeProcess.HasExited)
            {
                exeProcess.Kill();
            }

            exeProcess = new Process();
            exeProcess.StartInfo.FileName = exePath;
            exeProcess.StartInfo.UseShellExecute = false;
            exeProcess.StartInfo.CreateNoWindow = true;
            exeProcess.Start();
        }));
    }

    // 通用后台执行协程
    private IEnumerator ExecuteInBackground(System.Action action)
    {
        bool isCompleted = false;

        // 在ThreadPool中执行耗时操作
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            action.Invoke();
            isCompleted = true;
        });

        // 等待操作完成
        while (!isCompleted)
        {
            yield return null;
        }
    }

    public void RestartExe()
    {
        if (!string.IsNullOrEmpty(exePath))
        {
            StartCoroutine(StartExeCoroutine());
        }
    }

    public void StopExe()
    {
        if (exeProcess != null && !exeProcess.HasExited)
        {
            exeProcess.Kill();
        }
    }

    private void OnDestroy()
    {
        if (exeProcess != null && !exeProcess.HasExited)
        {
            exeProcess.Kill();
        }
    }
}
