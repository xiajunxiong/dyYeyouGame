using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// 弹幕
/// </summary>
public class Danmu : MonoBehaviour
{
	// 1. 定义静态唯一实例（核心：全局可访问）
	private static Danmu _instance;

	// 对外提供的实例访问属性（加锁保证线程安全）
	public static Danmu Instance
	{
		get
		{
			// 如果实例为空，自动查找场景中的对象
			if (_instance == null)
			{
				_instance = FindObjectOfType<Danmu>();

				// 如果场景中没有，创建一个新的GameObject挂载
				if (_instance == null)
				{
					GameObject singletonObj = new GameObject("DanmuManager_Singleton");
					_instance = singletonObj.AddComponent<Danmu>();
					// 标记为 DontDestroyOnLoad，切换场景不销毁
					DontDestroyOnLoad(singletonObj);
				}
			}
			return _instance;
		}
	}
	private void Awake()
	{
		// 确保全局只有一个实例
		if (_instance == null)
		{
			_instance = this;
			DontDestroyOnLoad(gameObject); // 可选：根据需求决定是否跨场景保留
		}
		else if (_instance != this)
		{
			// 如果已有实例，销毁当前重复的对象
			Destroy(gameObject);
		}
	}






	public LiveRoomMessage Messages;//弹幕数据





	public void ParseWithNewtonsoft(string sampleJson)
	{
		try
		{
			// 将JSON字符串转换为LiveRoomMessage对象
			LiveRoomMessage message = JsonConvert.DeserializeObject<LiveRoomMessage>(sampleJson);

			弹幕类型对接(message);
			//Debug.Log($"消息类型：{message.msg_type}");
			//Debug.Log($"用户：{message.user_name} 赠送了 {message.gift_count} 个 {message.gift_name}");
			////Debug.Log($"时间：{message.GetDateTime()}");
			//Debug.Log($"直播间当前人数：{message.total_user}");

		}
		catch (Exception e)
		{
			Debug.LogError($"JSON解析失败：{e.Message}");
		}
	}



	public void 弹幕类型对接(LiveRoomMessage message)
	{ 
	switch (message.msg_type)
		{
			case "comment"://发言
				发言与游戏对接(message);

				break;
			case "follow"://关注
				break;
			case "like"://点赞
				点赞回血(message);

				break;
			case "gift"://礼物
				礼物分配(message);
				break;
			

		}
	
	}






	public void 发言与游戏对接(LiveRoomMessage message)
	{
		switch (message.content)
		{
			case "加入":
				PersonManageer.Instance.ProcessUserJoin(message.user_name);//加入游戏
				break;
            default:
				
				break;
		}
	}



	public void 礼物分配(LiveRoomMessage message)
	{//首先查找消息人的房间，然后查找礼物的回血量

		int roomID=PersonManageer.Instance.查找玩家房间(message.user_name);

		//GiftData gift = DY_JsonDataManager.Instance.FindGiftDataFromDict(message.gift_name);

		//GameManager.Instance.Liverooms[roomID].door.RecoverHealth(gift.replyVolume);
	}


	public void 点赞回血(LiveRoomMessage message)
	{
		int roomID = PersonManageer.Instance.查找玩家房间(message.user_name);
		int i= UnityEngine.Random.Range(0, 100);
		//if (i > 40)
		//{
		//	GameManager.Instance.Liverooms[roomID].door.RecoverHealth(2);
		//}
	
	}

}



// 直播间消息数据模型
[Serializable] 
public class LiveRoomMessage
{
	// 消息类型：comment（发言）/gift（礼物）/like（点赞）/follow（关注）/member（进入）/status（直播间状态）/stat（直播间统计）
	public string msg_type;
	// 时间戳
	public long timestamp;
	// 用户名
	public string user_name;
	// 弹幕内容/状态描述
	public string content;
	// 礼物名称
	public string gift_name;
	// 礼物数量/点赞数
	public int gift_count;
	// 直播间状态码 == 3 直播间关闭
	public int status_code;
	// 用户数
	public int total_user;
	// 总数
	public int total;

	//// 可选：将时间戳转换为可读时间的辅助方法
	//public DateTime GetDateTime()
	//{
	//	return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
	//}
}