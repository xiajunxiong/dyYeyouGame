using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//游戏管理器
public class GameManager : MonoBehaviour
{
	#region 单例模式
	public static GameManager Instance;

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

	}
	#endregion
	//当前已经有的房间
	public List<Room> rooms;
	//依旧存活的房间
	public List<Room> Liverooms;




}
