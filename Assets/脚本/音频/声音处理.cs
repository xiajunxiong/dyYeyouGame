using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("音频管理/全局音效组件")]
public class 声音处理 : MonoBehaviour
{
    private List<AudioSource> 全局音效组件合集 = new List<AudioSource>();
    private Dictionary<AudioSource, float> 组件最后使用时间 = new Dictionary<AudioSource, float>();
    private Dictionary<AudioClip, List<AudioSource>> 音频对应播放组件 = new Dictionary<AudioClip, List<AudioSource>>();

    [Header("自动销毁设置")]
    public float 冗余组件超时时间 = 10f;
    public int 最小保留组件数 = 1;

    [Header("全局音量设置")]
    [Range(0f, 1f)] public float 全局音量 = 1f;

    public static 声音处理 Instance;
    private AudioSource 单条音频播放;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        初始化核心音频组件();
    }

    private void Update()
    {
        检查并销毁冗余组件();
    }

    private void 初始化核心音频组件()
    {
        while (全局音效组件合集.Count < 最小保留组件数)
        {
            AudioSource 核心组件 = 添加新音频组件();
            全局音效组件合集.Add(核心组件);
            组件最后使用时间[核心组件] = Time.time;
        }
        if (单条音频播放 != null) return;
        单条音频播放 = gameObject.AddComponent<AudioSource>();
    }

    public void 每次播放一条(AudioClip 音频源)
    {
        if (音频源 == null) return;
        单条音频播放.PlayOneShot(音频源, 全局音量);
    }

    public void 播放音乐(AudioClip 音频源)
    {
        if (音频源 == null)
        {
            Debug.LogWarning("播放音效失败：音频源为空！");
            return;
        }

        AudioSource 空闲组件 = 查找空闲音频组件();

        if (空闲组件 == null)
        {
            空闲组件 = 添加新音频组件();
            全局音效组件合集.Add(空闲组件);
        }

        组件最后使用时间[空闲组件] = Time.time;
        空闲组件.volume = 全局音量; 
        空闲组件.PlayOneShot(音频源);
        记录音频播放组件(音频源, 空闲组件);
    }

    public void 播放音乐(AudioClip 音频源, float 音量)
    {
        if (音频源 == null)
        {
            Debug.LogWarning("播放音效失败：音频源为空！");
            return;
        }

        AudioSource 空闲组件 = 查找空闲音频组件();

        if (空闲组件 == null)
        {
            空闲组件 = 添加新音频组件();
            全局音效组件合集.Add(空闲组件);
        }

        组件最后使用时间[空闲组件] = Time.time;
        空闲组件.volume = 全局音量 * 音量; 
        空闲组件.PlayOneShot(音频源, 音量);
        记录音频播放组件(音频源, 空闲组件);
    }

    public void 循环播放音乐(AudioClip 音频源, float 音量)
    {
        if (音频源 == null)
        {
            Debug.LogWarning("播放音效失败：音频源为空！");
            return;
        }

        AudioSource 空闲组件 = 查找空闲音频组件();

        if (空闲组件 == null)
        {
            空闲组件 = 添加新音频组件();
            全局音效组件合集.Add(空闲组件);
        }

        空闲组件.loop = true;
        空闲组件.clip = 音频源;
        空闲组件.volume = 全局音量 * 音量;
        组件最后使用时间[空闲组件] = Time.time;
        空闲组件.Play();
        记录音频播放组件(音频源, 空闲组件);
    }

    public void 循环播放音乐(AudioClip 音频源)
    {
        if (音频源 == null)
        {
            Debug.LogWarning("播放音效失败：音频源为空！");
            return;
        }

        AudioSource 空闲组件 = 查找空闲音频组件();

        if (空闲组件 == null)
        {
            空闲组件 = 添加新音频组件();
            全局音效组件合集.Add(空闲组件);
        }

        空闲组件.loop = true;
        空闲组件.clip = 音频源;
        空闲组件.volume = 全局音量;
        组件最后使用时间[空闲组件] = Time.time;
        空闲组件.Play();
        记录音频播放组件(音频源, 空闲组件);
    }

    public void 关闭包含音频源的所有组件(AudioClip 音频源)
    {
        if (音频源 == null)
        {
            Debug.LogWarning("停止音效失败：音频源为空！");
            return;
        }

        if (音频对应播放组件.TryGetValue(音频源, out List<AudioSource> 播放组件列表))
        {
            foreach (var 组件 in 播放组件列表)
            {
                if (组件 != null)
                {
                    组件.Stop();
                    组件.loop = false;
                    组件.clip = null;
                    组件最后使用时间[组件] = Time.time;
                }
            }
            播放组件列表.Clear();
        }
    }

    private AudioSource 查找空闲音频组件()
    {
        foreach (var 组件 in 全局音效组件合集)
        {
            if (组件 != null && !组件.isPlaying)
            {

                组件.loop = false;
                组件.clip = null;
                return 组件;
            }
        }
        return null;
    }

    private AudioSource 添加新音频组件()
    {
        AudioSource 新组件 = gameObject.AddComponent<AudioSource>();
        新组件.playOnAwake = false;
        新组件.loop = false;
        新组件.spatialBlend = 0f;
        新组件.volume = 全局音量;
        return 新组件;
    }
    private void 检查并销毁冗余组件()
    {
        List<AudioSource> 待销毁组件 = new List<AudioSource>();

        全局音效组件合集.RemoveAll(item => item == null);
        List<AudioSource> 空组件键 = new List<AudioSource>();
        foreach (var kvp in 组件最后使用时间)
        {
            if (kvp.Key == null)
            {
                空组件键.Add(kvp.Key);
            }
        }
        foreach (var 键 in 空组件键)
        {
            组件最后使用时间.Remove(键);
        }

        for (int i = 0; i < 全局音效组件合集.Count; i++)
        {
            var 组件 = 全局音效组件合集[i];

            if (组件 == null) continue;
            if (组件.isPlaying) continue;
            // 确保保留最小组件数
            if (全局音效组件合集.Count <= 最小保留组件数) break;

            if (Time.time - 组件最后使用时间[组件] > 冗余组件超时时间)
            {
                待销毁组件.Add(组件);
            }
        }

        foreach (var 组件 in 待销毁组件)
        {
            全局音效组件合集.Remove(组件);
            组件最后使用时间.Remove(组件);
            移除音频播放组件映射(组件);
            Destroy(组件);
        }
    }

    public void 手动清理冗余组件()
    {
        全局音效组件合集.RemoveAll(item => item == null);

        while (全局音效组件合集.Count > 最小保留组件数)
        {
            var 最后一个组件 = 全局音效组件合集[全局音效组件合集.Count - 1];
            if (最后一个组件 != null && !最后一个组件.isPlaying)
            {
                全局音效组件合集.Remove(最后一个组件);
                组件最后使用时间.Remove(最后一个组件);
                移除音频播放组件映射(最后一个组件);
                Destroy(最后一个组件);
            }
            else
            {
                break;
            }
        }
    }

    private void 记录音频播放组件(AudioClip 音频源, AudioSource 播放组件)
    {
        if (!音频对应播放组件.ContainsKey(音频源))
        {
            音频对应播放组件[音频源] = new List<AudioSource>();
        }
        // 避免重复添加
        if (!音频对应播放组件[音频源].Contains(播放组件))
        {
            音频对应播放组件[音频源].Add(播放组件);
        }
    }

    private void 移除音频播放组件映射(AudioSource 播放组件)
    {
        foreach (var kvp in 音频对应播放组件)
        {
            kvp.Value.Remove(播放组件);
        }
        List<AudioClip> 空键列表 = new List<AudioClip>();
        foreach (var kvp in 音频对应播放组件)
        {
            if (kvp.Value.Count == 0)
            {
                空键列表.Add(kvp.Key);
            }
        }
        foreach (var 键 in 空键列表)
        {
            音频对应播放组件.Remove(键);
        }
    }

    public void 更新全局音量(float 新音量)
    {
        全局音量 = Mathf.Clamp01(新音量); // 限制音量在0-1之间
        foreach (var 组件 in 全局音效组件合集)
        {
            if (组件 != null)
            {
                组件.volume = 全局音量;
            }
        }
    }

    public void 停止所有音效()
    {
        foreach (var 组件 in 全局音效组件合集)
        {
            if (组件 != null && 组件.isPlaying)
            {
                组件.Stop();
                组件.loop = false;
                组件.clip = null;
                组件最后使用时间[组件] = Time.time;
            }
        }
        音频对应播放组件.Clear();
    }
}