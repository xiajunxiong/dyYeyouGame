using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class 生存排行榜 : MonoBehaviour
{
    public static 生存排行榜 ins;
    public int 显示排名 = 10;
    private List<Text> 显示排行Text = new List<Text>();
    private void Awake()
    {
        ins = this;
        if (transform.Find("排名") != null)
        {
            foreach (Transform t in transform.Find("排名")) // 固定10个
            {
                Text txt = t.GetComponent<Text>();
                txt.text = "";
                if (txt != null)
                    显示排行Text.Add(txt);
            }
        }
        else
        {
            Debug.LogError("未找到名为【排名】的UI父物体，请检查层级命名！");
        }
        初始化排行榜();
    }

    public void 刷新排行榜(List<Room> 房间数据)
    {
        // 1. 取当前房间里的玩家（内存）
        List<Player> 内存玩家 = new List<Player>();
        if (房间数据 != null) // 房间数据也防一手
        {
            foreach (Room room in 房间数据)
            {
                if (room != null && room.player != null && room.player.Count > 0)
                    内存玩家.AddRange(room.player);
            }
        }

        // 2. 取存档里的玩家 + 判空（关键修复）
        List<Player> 存档玩家 = new List<Player>();
        if (RoomManager.ins != null && RoomManager.ins.LeaderboardData != null && RoomManager.ins.LeaderboardData.ranks != null)
        {
            存档玩家 = RoomManager.ins.LeaderboardData.ranks;
        }

        // 3. 合并去重：同名 → 内存覆盖存档
        Dictionary<string, Player> 最终玩家字典 = new Dictionary<string, Player>();

        foreach (var p in 存档玩家)
        {
            if (p != null && !最终玩家字典.ContainsKey(p.name))
                最终玩家字典.Add(p.name, p);
        }

        foreach (var p in 内存玩家)
        {
            if (p != null) // 防止内存里有null玩家
                最终玩家字典[p.name] = p;
        }

        // 4. 排序
        List<Player> 最终排行 = 最终玩家字典.Values
            .OrderByDescending(p => p.playerSurvivalDays)
            .ToList();

        // 5. 存回内存（保证LeaderboardData一定存在）
        if (RoomManager.ins.LeaderboardData == null)
            RoomManager.ins.LeaderboardData = new PlayerSaveData();

        RoomManager.ins.LeaderboardData.ranks = 最终排行;

        SaveRank(); // 这里也加个安全调用

        // 7. 更新UI
        重置排行榜();
        int maxShow = Mathf.Min(最终排行.Count, 显示排名);
        for (int i = 0; i < maxShow; i++)
        {
            if (i < 显示排行Text.Count && 显示排行Text[i] != null)
            {
                var info = 最终排行[i];

                // 前6名（0~5）：显示名字+天数
                if (i < 6)
                {
                    string shortName = info.name.Length > 6 ? info.name.Substring(0, 6) : info.name;
                    显示排行Text[i].text = $"{shortName} - {info.playerSurvivalDays}天";
                }
                // 第6名及以后：只显示天数，不显示名字
                else
                {
                    显示排行Text[i].text = $"{info.playerSurvivalDays}天";
                }
            }
        }
    }

    public void SaveRank()
    {
        string path;
#if UNITY_EDITOR
        //编辑器：Assets同级项目根目录
        string root = Directory.GetParent(Application.dataPath).FullName;
        path = Path.Combine(root, "RankList.json");
#else
    //打包：系统默认可读写目录，不会权限报错
    path = Path.Combine(Application.persistentDataPath, "RankList.json");
#endif

        string json = JsonUtility.ToJson(RoomManager.ins.LeaderboardData, true);
        //目录不存在自动创建
        if (!Directory.Exists(Path.GetDirectoryName(path)))
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        Debug.Log("保存成功路径：" + path);
    }
    public void 重置排行榜()
    {
        foreach (Text txt in 显示排行Text)
        {
            txt.text = "暂无";
        }
    }

    public void 初始化排行榜()
    {
               重置排行榜();
    }

    void Update()
    {
        
    }
}
