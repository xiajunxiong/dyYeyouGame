using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class 音乐 : MonoBehaviour
{
    [Header("背景音乐文件夹")]
    public string 白天音乐路径 = "音乐/白天音乐";
    public string 夜晚音乐路径 = "音乐/夜晚音乐";

    [Header("切换音效（固定路径）")]
    public string 天亮叫声路径 = "音乐/天亮鸡叫.mp3";
    public string 天黑叫声路径 = "音乐/天黑狼叫.mp3";

    [Header("播放器")]
    public AudioSource 背景音乐播放器;
    public AudioSource 切换音效播放器;

    // 音乐库
    private List<AudioClip> dayClips = new List<AudioClip>();
    private List<AudioClip> nightClips = new List<AudioClip>();
    private AudioClip 天亮叫声;
    private AudioClip 天黑叫声;

    // 统一加载状态管理
    private int totalLoadingTasks = 0;
    private int finishedLoadingTasks = 0;
    private bool isAllLoaded => finishedLoadingTasks >= totalLoadingTasks;

    void Awake()
    {
        if (背景音乐播放器 == null) 背景音乐播放器 = gameObject.AddComponent<AudioSource>();
        if (切换音效播放器 == null) 切换音效播放器 = gameObject.AddComponent<AudioSource>();

        背景音乐播放器.loop = true;
        背景音乐播放器.spatialBlend = 0;
        切换音效播放器.spatialBlend = 0;

        RegisterLoadTask(LoadSingleAudio(天亮叫声路径, clip => 天亮叫声 = clip));
        RegisterLoadTask(LoadSingleAudio(天黑叫声路径, clip => 天黑叫声 = clip));
        RegisterLoadTask(LoadFolderAudio(白天音乐路径, dayClips));
        RegisterLoadTask(LoadFolderAudio(夜晚音乐路径, nightClips));
    }

    void RegisterLoadTask(IEnumerator coroutine)
    {
        totalLoadingTasks++;
        StartCoroutine(StartTask(coroutine));
    }

    IEnumerator StartTask(IEnumerator coroutine)
    {
        yield return coroutine;
        finishedLoadingTasks++;
        CheckAllLoaded();
    }

    void CheckAllLoaded()
    {
        if (isAllLoaded)
        {
            OnTimeChanged(GameTime.ins.timeState);
        }
    }

    IEnumerator LoadSingleAudio(string relativePath, Action<AudioClip> onLoaded)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
        string url = $"file:///{fullPath}";

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
                onLoaded?.Invoke(DownloadHandlerAudioClip.GetContent(uwr));
            else
                Debug.LogError("加载失败：" + relativePath + " → " + uwr.error);
        }
    }

    IEnumerator LoadFolderAudio(string relativeFolder, List<AudioClip> list)
    {
        list.Clear();
        string fullFolder = Path.Combine(Application.streamingAssetsPath, relativeFolder);
        string[] exts = { ".mp3", ".wav", ".ogg" };
        string[] files = Directory.GetFiles(fullFolder);

        foreach (var f in files)
        {
            if (!Array.Exists(exts, e => e == Path.GetExtension(f).ToLower())) continue;
            string url = $"file:///{f}";

            using (var uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(uwr);
                    clip.name = Path.GetFileNameWithoutExtension(f);
                    list.Add(clip);
                }
            }
        }
    }

    private void OnTimeChanged(TimeState state)
    {
        if (!isAllLoaded) return;

        if (state == TimeState.Day)
        {
            切换音效播放器.PlayOneShot(天亮叫声);
            PlayRandomBGM(dayClips);
        }
        else if (state == TimeState.Night)
        {
            切换音效播放器.PlayOneShot(天黑叫声);
            PlayRandomBGM(nightClips);
        }
        else
        {
            背景音乐播放器.Stop();
        }
    }

    void PlayRandomBGM(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return;
        var clip = clips[UnityEngine.Random.Range(0, clips.Count)];
        背景音乐播放器.Stop();
        背景音乐播放器.clip = clip;
        背景音乐播放器.Play();
    }

    void OnEnable() => GameTime.ins.OnTimeStateChanged += OnTimeChanged;
    void OnDisable() => GameTime.ins.OnTimeStateChanged -= OnTimeChanged;
}