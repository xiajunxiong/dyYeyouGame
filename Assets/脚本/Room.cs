using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 房间
/// </summary>
[System.Serializable]
public class Room
{
    public int id;
    public int hp;
    public int maxHp;
    public Image hpImage;
    public Text hpText;
    public GameObject doorObj;
    public List<Player> player = new List<Player>();
}
[System.Serializable]
public class Player
{
    public string name;
    /// <summary>
    /// 玩家称号
    /// </summary>
    public GameObject playerTitle;
    /// <summary>
    /// 玩家预制体
    /// </summary>
    public GameObject playerObj;
    //玩家弹幕
    public Text playerText;
    /// <summary>
    /// 玩家生存天数
    /// </summary>
    public int playerSurvivalDays;
    /// <summary>
    /// 上一次输入的房间id
    /// </summary>
    public int lastJoinRoomId = -1;
    /// <summary>
    /// 上一次进入的房间id
    /// </summary>
    public int lastEnterRoomId = -1;
    /// <summary>
    /// 玩家所在区域
    /// </summary>
    public PlayerArea playerArea = PlayerArea.WaitingArea;
}
// 玩家所在区域枚举
public enum PlayerArea
{
    // 房间
    Room,
    // 等待区
    WaitingArea
}