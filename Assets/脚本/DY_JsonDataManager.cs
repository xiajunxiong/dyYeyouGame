using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 获取弹幕游戏的配置以及弹幕json数据
/// </summary>
public class DY_JsonDataManager : MonoBehaviour
{




	#region 单例模式
	public static DY_JsonDataManager Instance;
    public List<string> playerNameList = new List<string>();
    private void Awake()
	{

		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
#if UNITY_EDITOR
        jsonDataPath = GetExeDirectory() + "/GameInitData.json";
        playerNameListPath = GetExeDirectory() + "/玩家名称.oldxia";
#else
		jsonDataPath = GetExeDirectory() + "/GameInitData.json";
        playerNameListPath = GetExeDirectory() + "/玩家名称.oldxia";
#endif
        GetLocalJsonData();
        LoadPlayerNameList();
    }
	#endregion

	public GameInitData localGameInitData;
    private string playerNameListPath;

    private string jsonDataPath;

    private void Start()
    {

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
            Debug.LogError("获取exe目录失败，返回默认Data路径");
            return dataPath;
        }
    }

    public void GetLocalJsonData()
    {
        try
        {
            // 1. 检查文件是否存在
            if (!File.Exists(jsonDataPath))
            {
                Debug.LogError($"本地JSON文件不存在！路径：{jsonDataPath}");
                return;
            }

            string jsonContent = File.ReadAllText(jsonDataPath, System.Text.Encoding.UTF8);
            localGameInitData = JsonConvert.DeserializeObject<GameInitData>(jsonContent);

            if (localGameInitData == null)
            {
                Debug.LogError("解析后GameInitData为空！");
                return;
            }
            if (localGameInitData.giftData == null)
            {
                Debug.LogError("giftData数组为空！");
                return;
            }
            if (localGameInitData.gameConfiguration == null)
            {
                Debug.LogError("gameConfiguration为空！");
                return;
            }

            Debug.Log("解析成功 - 礼物数据条数：" + localGameInitData.giftData.Length);
            Debug.Log("解析成功 - 门初始血量：" + localGameInitData.gameConfiguration.doorInitialHealth);

        }
        catch (Exception e)
        {
            Debug.LogError("读取/解析本地JSON失败：" + e.Message + "\n异常详情：" + e.StackTrace);
        }
    }
    public void LoadPlayerNameList()
    {
        try
        {
            if (!File.Exists(playerNameListPath))
            {
                Debug.LogError("玩家列表文件不存在：" + playerNameListPath);
                return;
            }

            // 读取所有行
            string[] lines = File.ReadAllLines(playerNameListPath, System.Text.Encoding.UTF8);

            playerNameList.Clear();

            foreach (string line in lines)
            {
                string trimLine = line.Trim();
                if (!string.IsNullOrEmpty(trimLine))
                {
                    playerNameList.Add(trimLine);
                }
            }

            Debug.Log("玩家列表加载成功！共 " + playerNameList.Count + " 人");
        }
        catch (Exception e)
        {
            Debug.LogError("读取玩家列表失败：" + e.Message);
        }
    }

}





// JSON数据类

[Serializable]
public class HttpResponseData
{
	public int msgType;          // 消息类型
	public GameInitData gameData;// 游戏数据主体
}


[Serializable]
public class GameInitData
{
	public GiftData[] giftData;               // 礼物数据列表
	public ChanceOfHealing chanceOfHealing;   // 回血概率配置
	public GameConfiguration gameConfiguration;// 游戏核心配置
}





[Serializable]
public class GiftData
{
	public string name;         // 礼物名称
	public int replyVolume;     // 回血数值
}

[Serializable]
public class ChanceOfHealing
{
	public int likeReplyProbability;   // 点赞回血概率
	public int likeReplyValue;         // 点赞回血值
	public int bulletReplyProbability; // 弹幕回血概率
	public int bulletReplyValue;       // 弹幕回血值
}

[Serializable]
public class GameConfiguration
{
	public int ghostAttackCount;                // 鬼攻击次数
	public int ghostMinAttack;                  // 鬼最小攻击
	public int ghostMaxAttack;                  // 鬼最大攻击
	public int doorInitialHealth;               // 门初始血量
	public int zombieRageAttack1;               // 僵尸狂暴1攻击
	public int zombieRageAttack2;               // 僵尸狂暴2攻击
	public int zombieRageAttack3;               // 僵尸狂暴3攻击
	public int lobbyWaitCountdown;              // 大厅等待倒计时
	public int extraHealthPerPersonForDoor;     // 每人给门的额外血量
	public int baseNightEndCountdown;           // 黑夜结束基础倒计时
	public int extraNightDurationPerPerson;     // 每人额外黑夜时长
    public int miniNpcCount;                    // 最少NPC数量
    public int maxNpcCount;                     // 最多NPC数量
}
