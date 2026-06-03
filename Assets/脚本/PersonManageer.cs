using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//游戏内部人物进出行为管理器
//存储所有游戏内部人员的数据

public class PersonManageer : MonoBehaviour
{
	#region 单例模式
	public static PersonManageer Instance;

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

	public HashSet<Person> allpeople = new HashSet<Person>();
	Person[] people = null;
	private HashSet<string> _joinedUsers = new HashSet<string>();//加入的用户名称队列

	public List<GameObject> personPres;

	//处理玩家加入
	public void ProcessUserJoin(string userName)
	{
		// 1. 预处理用户名
		string cleanUserName = userName.Trim().ToLower();

		// 2. 查重判断
		if (_joinedUsers.Contains(cleanUserName))
		{
			Debug.Log($"用户【{userName}】已加入游戏，无需重复添加");
			return;
		}

		// 3. 未加入过 → 执行加入游戏逻辑
		Debug.Log($"用户【{userName}】首次加入，开始添加到游戏...");

		Person person=new Person();
		person.name = cleanUserName;
		// ① 存入已加入集合
		_joinedUsers.Add(cleanUserName);
	

		// ③ 执行实际的“加入游戏”逻辑
		AddUserToGame(person);
	}



	//玩家加入后续实现（玩家随机再一个房间内）
	private void AddUserToGame(Person person)
	{
		////随机加入一个房间（进而执行生成玩家。给门加血操作）
		//int a = Random.Range(0, GameManager.Instance.Liverooms.Count);
		//person.roomID = a;
		//GameManager.Instance.Liverooms[a].加入该房间(person);
		//allpeople.Add(person);

		//Debug.Log($"✅成功将【{person.name}】加入游戏");
	}





	//处理玩家行为{发言/回血}
	public void 玩家发言()
	{ 
	
	}


	public int 查找玩家房间(string name)
	{
		var person = allpeople.FirstOrDefault(p => p.name == name);
		return person.roomID;
	}

}


public class Person 
{
	public string name;
	public int roomID;//该玩家所在的房间
	
}


