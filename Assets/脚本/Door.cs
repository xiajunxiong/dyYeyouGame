using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 僵尸要攻击的门
/// </summary>
public class Door : MonoBehaviour
{
	public float Maxhealth;
	public float Minhealth;
	public float CurrentHealth;
	public Room currentRoom;
	public Slider 血条;
	public GameObject 门前位置;

	private void Start()
	{
		CurrentHealth = Maxhealth;
	}

	//恢复血量函数

	public void RecoverHealth(int 恢复值)
	{
		CurrentHealth += 恢复值;
		if(CurrentHealth>Maxhealth)CurrentHealth = Maxhealth; ; 
		更新血条();
	}


	//受伤函数
	public void TakeDamage(int 伤害值)
	{
		CurrentHealth -= 伤害值;
		Debug.Log("门受到伤害");
		更新血条();
		if (CurrentHealth <= 0) {
			Debug.Log("门被销毁");
			//销毁该房间的gamebject，把该房间玩家玩家剔除
			//currentRoom.clear();
		}
	}


	public void 更新血条()
	{
		血条.value = CurrentHealth / Maxhealth;
	}


	//门被破坏函数
	public void Dead()
	{
		//currentRoom.clear();
	}
}
