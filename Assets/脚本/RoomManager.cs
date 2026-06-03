using Newtonsoft.Json.Linq;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

struct DaytimeGiftStorageInformation
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string playerName;
    /// <summary>
    /// 礼物名称
    /// </summary>
    public string giftName;
    /// <summary>
    /// 礼物回复值
    /// </summary>
    public int giftResponseValue;
    /// <summary>
    /// 礼物价值
    /// </summary>
    public int giftValue;
}


[System.Serializable]
public class PlayerSaveData
{
    public List<Player> ranks = new List<Player>();
}
public class RoomManager : MonoBehaviour
{
    public List<Room> room = new List<Room>();
    // 所有房间初始状态用来重置房间时使用
    public List<Room> initialRoomState = new List<Room>();
    public static RoomManager ins;
    public Image[] hpImage;
    public Text[] hpText;
    public GameObject[] doorObj;
    public KeyCode addPlayerKey = KeyCode.P;
    public float hpMoveSpeed = 5f;
    public float playerMoveSpeed = 3f;
    public int playerTextNameSize = 32;
    public List<MoveGameObjectParameter> moveGameObjects = new List<MoveGameObjectParameter>();
    public List<GameObject> listPlayerStartPos = new List<GameObject>();
    private float fixedSpawnInterval; // 下一次刷人的随机时间
    private float spawnPlayerTimer;
    private int totalDayPlayerCount;
    public int dayPlayerCount = 0;

    public GameObject danmakuPrefab;
    public PlayerSaveData LeaderboardData;
    // 未分配房间的玩家对象列表
    public List<GameObject> unassignedPlayers = new List<GameObject>();

    // 玩家发送进入房间缓存房间id
    //public List<UnassignedPlayer> playerLastJoinRoomCache = new List<UnassignedPlayer>();

    public List<GameObject> playerPrefab = new List<GameObject>();

    public List<Vector4> playerRandomRoomArea = new List<Vector4>();
    public GameObject playerParent;
    public GameObject heartGameObjectParent;
    public List<GameObject> honorGameObjectParent = new List<GameObject>();
    public GameObject heartParent;
    // 设置玩家图层避免闪烁 int 顺序递增
    private int playerSortingOrder = 0;
    public List<string> debugplayername = new List<string>();
    public Vector4 playerWaitArea = Vector4.zero;
    private bool hasMovedInLast5Seconds = false;
    private List<DaytimeGiftStorageInformation> daytimeGiftStorageInformation = new List<DaytimeGiftStorageInformation>();
    // 称号图片合集
    public List<Sprite> honorSprites = new List<Sprite>();
    private void Start()
    {
        ins = this;
        room.Clear();
        for (int i = 0; i < 4; i++)
        {
            Room newRoom = new Room();
            newRoom.id = i;
            newRoom.hp = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.doorInitialHealth;
            newRoom.maxHp = newRoom.hp;
            newRoom.hpImage = hpImage[i];
            newRoom.hpText = hpText[i];
            newRoom.hpText.text = newRoom.hp.ToString();
            newRoom.hpImage.fillAmount = newRoom.hp / newRoom.maxHp;
            newRoom.doorObj = doorObj[i];
            room.Add(newRoom);
        }
        foreach(var r in room)
        {
            initialRoomState.Add(CopyRoom(r));
        }
        GameTime.ins.OnTimeStateChanged += OnTimeStateChanged;
        string path;
#if UNITY_EDITOR
        string root = Directory.GetParent(Application.dataPath).FullName;
        path = Path.Combine(root, "RankList.json");
#else
    path = Path.Combine(Application.persistentDataPath, "RankList.json");
#endif

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            LeaderboardData = JsonUtility.FromJson<PlayerSaveData>(json);
            Debug.Log("读取成功：" + path);
        }
        else
        {
            LeaderboardData = new PlayerSaveData();
            Debug.Log("无存档，新建数据");
        }

        // 动态加载/Resources/称号背景/所有图片资源到称号图片合集
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("称号背景");
        honorSprites.AddRange(loadedSprites);
    }

    Room CopyRoom(Room original)
    {
        Room newRoom = new Room();
        newRoom.id = original.id;
        newRoom.hp = original.hp;
        newRoom.maxHp = original.maxHp;
        newRoom.hpImage = original.hpImage;
        newRoom.hpText = original.hpText;
        newRoom.doorObj = original.doorObj;
        return newRoom;
    }

    private void OnTimeStateChanged(TimeState state)
    {
        //if (state == TimeState.Night && unassignedPlayers.Count > 0)// 给玩家添加随机房间
        //{
        //    foreach (var p in unassignedPlayers)
        //    {
        //        AddPlayerRandomToRoom(p);
        //    }
        //    unassignedPlayers.Clear();
        //}

        if (state == TimeState.Night && daytimeGiftStorageInformation.Count > 0)
        {
            foreach(var g in daytimeGiftStorageInformation)
            {
                //礼物排行榜.ins.玩家赠送礼物(g.playerName, g.giftName);
                AddHpByGiftForPlayer(g.playerName,g.giftName,g.giftResponseValue);
            }
            // 清空
            daytimeGiftStorageInformation.Clear();
        }
        else if(state == TimeState.Day)
        {
            hasMovedInLast5Seconds = false;
            totalDayPlayerCount = UnityEngine.Random.Range(DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.miniNpcCount, DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.maxNpcCount + 1);
            // 剩余数量 = 总数
            dayPlayerCount = totalDayPlayerCount;

            // 有效时间 = 白天总时间 - 最后5秒（不刷）
            float validTime = GameTime.ins.dayTime - 5f;

            // 线性固定间隔 = 有效时间 / 总数
            if (totalDayPlayerCount > 0)
                fixedSpawnInterval = validTime / totalDayPlayerCount;
            else
                fixedSpawnInterval = 9999f;

            // 初始化计时器
            spawnPlayerTimer = 0f;
            MovePlayerToWaitArea();
        }



        if (state == TimeState.Day)// 发放称号
        {
            foreach (var r in room)
            {
                foreach (var p in r.player)
                {
                    // 查找是否已经发放了称号预制体，如果没有则发放称号预制体
                    GameObject gameObject = p.playerObj.transform.Find("称号")?.gameObject;
                    if (gameObject == null)
                    {
                        AwardAHonor(p.playerObj);
                    }



                    p.playerSurvivalDays++;
                }
            }
            生存排行榜.ins.刷新排行榜(room);
        }
    }

    // 玩家发送弹幕显示
    public void ShowDanmaku(string name, string content)
    {
        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    if(p.playerText == null)
                    {
                        GameObject danmakuObj = Instantiate(danmakuPrefab);
                        danmakuObj.transform.SetParent(p.playerObj.transform);
                        danmakuObj.transform.localPosition = new Vector3(0, 1.3f, 0);
                        danmakuObj.transform.localRotation = Quaternion.identity;
                        danmakuObj.transform.localScale = Vector3.one;
                        p.playerText = danmakuObj.transform.Find("Canvas/弹幕").GetComponentInChildren<Text>();
                    }
                    else
                    {
                        p.playerText.text = content;
                    }



                    StartCoroutine(ClearPlayerDanmaku(p.playerText));
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 发言协程 1s后清除玩家发言
    /// </summary>
    private IEnumerator ClearPlayerDanmaku(Text text)
    {
        yield return new WaitForSeconds(1f);
        text.text = "";
    }

    public void ResetRooms()
    {
        // 销毁所有玩家对象
        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                Destroy(p.playerObj);
            }
            r.player.Clear();
        }
        room.Clear();
        daytimeGiftStorageInformation.Clear();
        foreach (var r in initialRoomState)
        {
            room.Add(CopyRoom(r));
        }
        foreach (var r in room)
        {
            r.hpText.text = r.hp.ToString();
            r.hpImage.fillAmount = r.hp / r.maxHp;
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(addPlayerKey))
        {
            string name = "Player" + UnityEngine.Random.Range(0, 10000);
            debugplayername.Add(name);
            AddPlayer(name);
        }
        if( Input.GetKeyDown(KeyCode.Space))
        {
            for(int i = 0; i < 10; i++)
            {
                AddHpByLikeReplyForPlayer(debugplayername[UnityEngine.Random.Range(0, debugplayername.Count)]);
            }
                
        }

        if (GameTime.ins.timeState == TimeState.Day && GameTime.ins.nowTime <= 5f)
        {
            if (!hasMovedInLast5Seconds)
            {
                foreach (var p in unassignedPlayers)// 给玩家添加随机房间
                {
                    AddPlayerRandomToRoom(p);
                }
                unassignedPlayers.Clear();

                hasMovedInLast5Seconds = true;
                TransferPlayerToRoomArea();
            }
        }

        if (GameTime.ins.timeState == TimeState.Day
            && dayPlayerCount > 0
            && GameTime.ins.nowTime > 5f)
        {
            spawnPlayerTimer += Time.deltaTime;

            if (spawnPlayerTimer >= fixedSpawnInterval)
            {
                spawnPlayerTimer = 0f;

                var nameList = DY_JsonDataManager.Instance.playerNameList;
                string randomName = nameList[UnityEngine.Random.Range(0, nameList.Count)];

                if (AddPlayer(randomName))
                {
                    dayPlayerCount--;
                }
            }
        }

        MoveGameObjectParameter();
    }

    private void OnDestroy()
    {
        GameTime.ins.OnTimeStateChanged -= OnTimeStateChanged;
    }

    private void MovePlayerToWaitArea()
    {
        // 遍历所有房间
        foreach (var r in room)
        {
            // 处理单个房间
            foreach (var p in r.player)
            {
                if(p.playerObj == null)
                {
                    continue;
                }
                var playerObj = p.playerObj;
                playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "run";
                MoveGameObjectParameter parameter = new MoveGameObjectParameter
                {
                    speed = playerMoveSpeed,
                    gameObject = playerObj,
                    target = r.doorObj,
                    rot = playerObj.transform.Find("player"),
                    action = () =>
                    {
                        var tagetPos = GenerateRandomPosInRoomArea(playerWaitArea);

                        MoveGameObjectParameter parameter2 = new MoveGameObjectParameter
                        {
                            speed = playerMoveSpeed,
                            gameObject = playerObj,
                            v3Target = tagetPos,
                            rot = playerObj.transform.Find("player"),
                            action = () =>
                            {
                                playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "idle";
                            }
                        };


                        // 朝向
                        playerObj.transform.Find("player").rotation = Quaternion.Euler(0,
                            playerObj.transform.position.x > tagetPos.x ? 180 : 0, 0);
                        AddGameObjectMove(parameter2);
                    }
                };

                AddGameObjectMove(parameter);
            }
        }

    }

    // 单个玩家从等待区移动到房间门口，再从房间门口移动到房间区域内随机位置
    public void PlayerMoveRoomRan(int roomId,GameObject playerObj)
    {
        playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "run";
        MoveGameObjectParameter parameter = new MoveGameObjectParameter
        {
            speed = playerMoveSpeed,
            gameObject = playerObj,
            target = room[roomId].doorObj,
            rot = playerObj.transform.Find("player"),
            action = () =>
            {
                var tagetPos = GenerateRandomPosInRoomArea(playerRandomRoomArea[roomId]);
                MoveGameObjectParameter parameter2 = new MoveGameObjectParameter
                {
                    speed = playerMoveSpeed,
                    gameObject = playerObj,
                    v3Target = tagetPos,
                    rot = playerObj.transform.Find("player"),
                    action = () =>
                    {
                        playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "idle";
                    }
                };
                // 朝向
                playerObj.transform.Find("player").rotation = Quaternion.Euler(0,
                    playerObj.transform.position.x > tagetPos.x ? 180 : 0, 0);
                AddGameObjectMove(parameter2);
            }
        };
        AddGameObjectMove(parameter);
    }

    private void TransferPlayerToRoomArea()
    {

        // 遍历所有房间
        foreach (var r in room)
        {
            // 处理单个房间
            foreach (var p in r.player)
            {
                if(p.playerObj == null || p.playerArea == PlayerArea.Room)
                {

                    continue;
                }
                var playerObj = p.playerObj;
                moveGameObjects.RemoveAll(m => m != null && m.gameObject == playerObj);
                playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "run";
                var rtager = r.doorObj;
                //Debug.Log($"玩家 {p.name} 从等待区移动到房间 {r.id} 门口 位置: {rtager.transform.position}");
                MoveGameObjectParameter parameter = new MoveGameObjectParameter
                {
                    speed = playerMoveSpeed,
                    gameObject = playerObj,
                    target = rtager,
                    rot = playerObj.transform.Find("player"),
                    action = () =>
                    {
                        var targe = GenerateRandomPosInRoomArea(playerRandomRoomArea[r.id]);

                        MoveGameObjectParameter parameter2 = new MoveGameObjectParameter
                        {
                            speed = playerMoveSpeed,
                            gameObject = playerObj,
                            v3Target = targe,
                            rot = playerObj.transform.Find("player"),
                            action = () =>
                            {
                                //Debug.Log($"玩家 {p.name} 进入房间 {r.id} 区域 位置: {targe}");

                                playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "idle";
                            }
                        };

                        // 朝向
                        playerObj.transform.Find("player").rotation = Quaternion.Euler(0,
                            playerObj.transform.position.x > r.doorObj.transform.position.x ? 180 : 0, 0);

                        //Debug.Log("玩家所在房间" + p.name + "_" + playerRandomRoomArea[r.id]);
                        // 加血逻辑
                        int addHp = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.extraHealthPerPersonForDoor;
                        GameObject go = HpUIPond.Instance.GetAddHpObj();
                        go.GetComponent<Text>().text = "+" + addHp;
                        go.GetComponent<RectTransform>().position = r.hpText.GetComponent<RectTransform>().position;
                        go.SetActive(true);

                        r.hp += addHp;
                        if (r.hp > r.maxHp)
                            r.maxHp = r.hp;

                        r.hpText.text = r.hp.ToString();
                        r.hpImage.fillAmount = (float)r.hp / r.maxHp;

                        HpUIPond.Instance.RecycleObjWithDelay(go, 1f, 0);
                        AddGameObjectMove(parameter2);
                    }
                };

                AddGameObjectMove(parameter);
            }
        }
        
    }

    // 单个玩家移动到房间随机区域
    private void TransferPlayerToRoomArea(Player playerdata, Room r)
    {
        if (playerdata.playerObj == null || playerdata.playerArea == PlayerArea.Room)
        {
            return;
        }
        var playerObj = playerdata.playerObj;
        moveGameObjects.RemoveAll(m => m != null && m.gameObject == playerObj);
        playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "run";

        // ===================== 【新增：上一个房间过渡逻辑】 =====================
        // 读取玩家上一个所在房间
        Room lastRoom = null;
        if (playerdata.lastEnterRoomId >= 0)
        {
            lastRoom = room.FirstOrDefault(roomItem => roomItem.id == playerdata.lastEnterRoomId);
        }

        // 如果存在上一个房间 → 先移动到上一个房间门口，再执行你原来的逻辑
        if (lastRoom != null)
        {
            MoveGameObjectParameter paramToLastRoom = new MoveGameObjectParameter
            {
                speed = playerMoveSpeed,
                gameObject = playerObj,
                target = lastRoom.doorObj,
                rot = playerObj.transform.Find("player"),
                action = () =>
                {
                    // 走完上一个房间门口 → 继续执行你【原本完整的移动逻辑】
                    OriginalMoveLogic(playerdata, r);
                }
            };
            AddGameObjectMove(paramToLastRoom);
            return; // 必须return，不执行下面原有代码
        }
        // ======================================================================

        // ===================== 【你原来的所有逻辑，完全没动】 =====================
        OriginalMoveLogic(playerdata, r);
    }

    // ===================== 【你原来的完整逻辑，原封不动提取】 =====================
    private void OriginalMoveLogic(Player playerdata, Room r)
    {
        var playerObj = playerdata.playerObj;
        var rtager = r.doorObj;
        Debug.Log($"玩家 {playerdata.name} 从等待区移动到房间 {r.id} 门口 位置: {rtager.transform.position}");
        MoveGameObjectParameter parameter = new MoveGameObjectParameter
        {
            speed = playerMoveSpeed,
            gameObject = playerObj,
            target = rtager,
            rot = playerObj.transform.Find("player"),
            action = () =>
            {
                var targe = GenerateRandomPosInRoomArea(playerRandomRoomArea[r.id]);

                MoveGameObjectParameter parameter2 = new MoveGameObjectParameter
                {
                    speed = playerMoveSpeed,
                    gameObject = playerObj,
                    v3Target = targe,
                    rot = playerObj.transform.Find("player"),
                    action = () =>
                    {
                        Debug.Log($"玩家 {playerdata.name} 进入房间 {r.id} 区域 位置: {targe}");

                        playerObj.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "idle";
                    }
                };

                // 朝向
                playerObj.transform.Find("player").rotation = Quaternion.Euler(0,
                    playerObj.transform.position.x > r.doorObj.transform.position.x ? 180 : 0, 0);

                // 加血逻辑
                int addHp = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.extraHealthPerPersonForDoor;
                GameObject go = HpUIPond.Instance.GetAddHpObj();
                go.GetComponent<Text>().text = "+" + addHp;
                go.GetComponent<RectTransform>().position = r.hpText.GetComponent<RectTransform>().position;
                go.SetActive(true);

                r.hp += addHp;
                if (r.hp > r.maxHp)
                    r.maxHp = r.hp;

                r.hpText.text = r.hp.ToString();
                r.hpImage.fillAmount = (float)r.hp / r.maxHp;

                HpUIPond.Instance.RecycleObjWithDelay(go, 1f, 0);
                AddGameObjectMove(parameter2);
            }
        };

        AddGameObjectMove(parameter);
    }

    /// <summary>
    /// 房间被销毁
    /// </summary>
    /// <param name="room"></param>
    public void DestroyRoom(Room room)
    {
        Debug.Log($"===== 开始销毁房间，房间ID：{room.id} =====");

        // 销毁房间内所有玩家
        foreach (var player in room.player)
        {
            if (player != null && player.playerObj != null)
            {
                生存天数排行._instance.减少人数(1);
                Debug.Log($"房间 {room.id} 内玩家被移除，玩家对象：{player.playerObj.name}");
                Destroy(player.playerObj);
            }
        }

        Debug.Log($"房间 {room.id} 内所有玩家已销毁");

        // 清空房间玩家列表 & 移除房间
        room.player.Clear();
        this.room.Remove(room);
        Debug.Log($"房间 {room.id} 已从房间列表移除，当前剩余房间数量：{this.room.Count}");

        // 判断是否需要重置游戏时间
        bool isExitGame = false;
        Debug.Log("===== 开始检测是否存在有效玩家 =====");

        if (this.room.Count == 0)
        {
            isExitGame = false;
            Debug.Log("房间总数 = 0 → 无任何房间，判定：无有效玩家");
        }
        else
        {
            foreach (var r in this.room)
            {
                if (r.player != null && r.player.Count > 0)
                {
                    isExitGame = true;
                    Debug.Log($"检测到有效房间：{r.id}，内有玩家数量：{r.player.Count} → 存在有效玩家");
                }
            }
        }

        if (!isExitGame)
        {
            Debug.Log("===== 所有房间均无玩家，执行重置游戏 =====");
            GameTime.ins.ResetGame();
            GameTime.ins.StartDayTime();
            Debug.Log("游戏时间已重置并重新开始昼夜循环");
        }
        else
        {
            Debug.Log("===== 仍存在有效玩家，不重置游戏 =====");
        }

        Debug.Log($"===== 销毁房间 {room.id} 流程结束 =====\n");
    }
    /// <summary>
    /// 点赞回血
    /// </summary>
    /// <param name="name"></param>
    public void AddHpByLikeReplyForPlayer(string name)
    {
        int healingProbability = DY_JsonDataManager.Instance.localGameInitData.chanceOfHealing.likeReplyProbability;
        bool isAddhp = UnityEngine.Random.value < (healingProbability / 100f);
        //if (!isAddhp) return;
        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    var gameObject = Instantiate(heartGameObjectParent, heartParent.transform);
                    gameObject.transform.position = GetPlayerGameObject(name).transform.position;
                    var target = GetPlayerRoom(name);
                    MoveGameObjectParameter parameter = new MoveGameObjectParameter
                    {
                        speed = hpMoveSpeed,
                        gameObject = gameObject,
                        target = target,
                        action = () =>
                        {
                            int addhp = DY_JsonDataManager.Instance.localGameInitData.chanceOfHealing.likeReplyValue;
                            r.hp += addhp;
                            if (r.hp > r.maxHp)
                            {
                                r.maxHp = r.hp;
                            }
                            r.hpText.text = r.hp.ToString();
                            r.hpImage.fillAmount = (float)r.hp / r.maxHp;
                            AddHpByLikeReplyForPlayer(addhp, r.hpText.rectTransform);
                            Destroy(gameObject);
                        }
                    };
                    AddGameObjectMove(parameter);
                    return;
                }
            }
        }
    }

    public static List<string> ZombieSchoolTitleList = new List<string>()
{
    "铁门守护者","教室壁垒君","窗台御尸者","门板坚盾仔","走廊守夜人",
    "课桌防线师","壁橱挡尸客","阁楼坚城者","楼道镇尸徒","砖墙捍卫者",
    "储物柜哨兵","阳台御敌酱","木门死忠卫","阶梯防线主","密室守门灵",
    "悄声避尸客","暗影溜行者","静默求生姬","轻步躲僵仔","无痕潜伏者",
    "暗处静观人","迂回脱身者","隐匿避袭者","无声遁走客","幽影求生士",
    "低姿潜行酱","侧路突围员","静候危机过","轻身脱险者","暗角栖身客",
    "徒手挡僵侠","课桌驱尸郎","粉笔御敌士","扫把战尸王","黑板击退者",
    "教具狂战士","书卷镇僵人","直尺破敌君","圆规斩尸客","课本御僵神",
    "板凳冲锋手","水杯阻敌侠","文具悍勇者","课桌冲锋将","校园战僵徒",
    "幸运躲灾崽","天命空房间","霉运绕行者","随机安全宅","好运栖居者",
    "僵潮绝缘体","福地安居客","无袭幸运儿","偶遇安全屋","灾祸擦肩客",
    "天选避灾人","闲居无恙者","顺风求生君","祥瑞护宅仔","零袭幸运酱",
    "布局引僵者","巧设障眼师","声东避袭谋","智划逃生路","诱敌远离客",
    "密室布局手","预判尸潮者","巧改房门位","障物阻僵师","智取求生士",
    "预判破局人","巧藏藏身点","分流引僵君","聪慧守宅客","灵思避祸徒",
    "躺平待结局","摆烂苟命仔","随缘存活者","躺卧避尸人","摆姿求生酱",
    "佛系熬过去","闲躺渡危机","慵懒苟活客","无心求生存","散漫安居者",
    "躺平渡僵潮","慵懒守空房","随性存活君","放空避危机","静卧等平息",
    "全校最后幸存者","整栋楼独存者","通宵守楼战神","百房避袭宗师","长夜御僵至尊",
    "绝境独栖神明","尸潮环绕无伤","全楼层苟命王者","教室绝境活宝","校园末日独行者"
};

    /// 发放称号
    public void AwardAHonor(GameObject player)
    {
        int suoyin = UnityEngine.Random.Range(0, honorGameObjectParent.Count);
        var honor = Instantiate(honorGameObjectParent[suoyin], player.transform);
        //honor.transform.Find("Canvas/UI图片称号").GetComponent<Image>().sprite = honorSprites[UnityEngine.Random.Range(0, honorSprites.Count)];
        honor.transform.Find("Canvas/称号名称").GetComponent<Text>().text = ZombieSchoolTitleList[UnityEngine.Random.Range(0, ZombieSchoolTitleList.Count)];
        honor.name = honorGameObjectParent[suoyin].name;
    }


    /// <summary>
    /// 增加血量值，房间位置
    /// </summary>
    /// <param name="hpValue"></param>
    /// <param name="pos"></param>
    public void AddHpByLikeReplyForPlayer(int hpValue, RectTransform pos)
    {
        GameObject go = HpUIPond.Instance.GetAddHpObj();
        go.GetComponent<Text>().text = "+" + hpValue.ToString();
        go.GetComponent<RectTransform>().position = pos.position;
        go.SetActive(true);

        HpUIPond.Instance.RecycleObjWithDelay(go, 1f, 0);
    }

    /// <summary>
    /// 减少血量值，房间位置
    /// </summary>
    /// <param name="hpValue"></param>
    /// <param name="pos"></param>
    public void ReduceHpByLikeReplyForPlayer(int hpValue, RectTransform pos)
    {
        GameObject go = HpUIPond.Instance.GetReduceHpObj();
        go.GetComponent<Text>().text = "-" + hpValue.ToString();
        go.GetComponent<RectTransform>().position = pos.position;
        go.SetActive(true);
        HpUIPond.Instance.RecycleObjWithDelay(go, 1f, 1);
    }

    /// <summary>
    /// 弹幕回血
    /// </summary>
    /// <param name="name"></param>
    public void AddHpByDanmakuForPlayer(string name)
    {
        if (GameTime.ins.timeState == TimeState.Day) return;// 白天不加血
        int healingProbability = DY_JsonDataManager.Instance.localGameInitData.chanceOfHealing.bulletReplyProbability;
        bool isAddhp = UnityEngine.Random.value < (healingProbability / 100f);
        if (!isAddhp) return;
        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    var gameObject = Instantiate(heartGameObjectParent, heartParent.transform);
                    gameObject.transform.position = GetPlayerGameObject(name).transform.position;
                    var target = GetPlayerRoom(name);
                    MoveGameObjectParameter parameter = new MoveGameObjectParameter
                    {
                        speed = hpMoveSpeed,
                        gameObject = gameObject,
                        target = target,
                        action = () =>
                        {
                            int addhp = DY_JsonDataManager.Instance.localGameInitData.chanceOfHealing.bulletReplyValue;
                            r.hp += addhp;
                            if (r.hp > r.maxHp)
                            {
                                r.maxHp = r.hp;
                            }
                            r.hpImage.fillAmount = (float)r.hp / r.maxHp;
                            AddHpByLikeReplyForPlayer(addhp, r.hpText.rectTransform);
                            Destroy(gameObject);
                        }
                    };
                    AddGameObjectMove(parameter);
                    return;
                }
            }
        }
    }


    /// <summary>
    /// 弹幕名称 礼物名称
    /// </summary>
    /// <param name="name"></param>
    /// <param name="giftname"></param>
    public void AddHpByGiftForPlayer(string name, string giftname)
    {
        // 检查玩家是否已经在房间中
        //foreach (var r in room)
        //{
        //    if (r.player == null) break;
        //    foreach (var p in r.player)
        //    {
        //        if (p.name == name)
        //        {
        //            return;// 已经存在该玩家
        //        }
        //    }
        //}

        int giftHp = 0;
        bool isGift = false;
        foreach (var gift in DY_JsonDataManager.Instance.localGameInitData.giftData)
        {
            if (gift.name == giftname)
            {
                giftHp = gift.replyVolume;
                isGift = true;
                continue;
            }
        }

        if (GameTime.ins.timeState == TimeState.Day && giftHp != 0)
        {

            var data = new DaytimeGiftStorageInformation
            {
                playerName = name,
                giftName = giftname,
                giftResponseValue = giftHp,
            };
            string tipText = $"<color=green>{name}送出【 <b><color=#FF3333>{giftname}</color></b>】，夜晚自动加血</color>";
            全局UI管理.Instance.显示提示UI(tipText, Color.white);
            daytimeGiftStorageInformation.Add(data);
            return;
        }

        //礼物排行榜.ins.玩家赠送礼物(name, giftname);
        if (!isGift)
        {
            全局UI管理.Instance.显示提示UI($"管理员未配置该礼物 <b><color=yellow>【{giftname}】</color></b> 对应回血数值！", Color.red);
            return;
        }

        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    var gameObject = Instantiate(heartGameObjectParent, heartParent.transform);
                    gameObject.transform.position = GetPlayerGameObject(name).transform.position;
                    var target = GetPlayerRoom(name);
                    MoveGameObjectParameter parameter = new MoveGameObjectParameter
                    {
                        speed = hpMoveSpeed,
                        gameObject = gameObject,
                        target = target,
                        action = () =>
                        {
                            r.hp += giftHp;
                            if (r.hp > r.maxHp)
                            {
                                r.maxHp = r.hp;
                            }
                            r.hpText.text = r.hp.ToString();
                            r.hpImage.fillAmount = (float)r.hp / r.maxHp;
                            AddHpByLikeReplyForPlayer(giftHp, r.hpText.rectTransform);
                            Destroy(gameObject);
                        }
                    };
                    AddGameObjectMove(parameter);
                    string tipText = $"<color=green>{name}送出【 <b><color=#FF3333>{giftname}</color></b>】</color>";
                    全局UI管理.Instance.显示提示UI(tipText, Color.white);
                    return;
                }
            }
        }
    }

    public void AddHpByGiftForPlayer(string name, string giftname, int replyVolume)
    {

        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    var gameObject = Instantiate(heartGameObjectParent, heartParent.transform);
                    Debug.LogWarning("111:" + gameObject);
                    var ceshi = GetPlayerGameObject(name);
                    Debug.LogWarning("111:" + ceshi);
                    gameObject.transform.position = ceshi.transform.position;
                    var target = GetPlayerRoom(name);
                    MoveGameObjectParameter parameter = new MoveGameObjectParameter
                    {
                        speed = hpMoveSpeed,
                        gameObject = gameObject,
                        target = r.doorObj,
                        action = () =>
                        {
                            r.hp += replyVolume;
                            if (r.hp > r.maxHp)
                            {
                                r.maxHp = r.hp;
                            }
                            r.hpText.text = r.hp.ToString();
                            r.hpImage.fillAmount = (float)r.hp / r.maxHp;
                            AddHpByLikeReplyForPlayer(replyVolume, r.hpText.rectTransform);
                            Destroy(gameObject);
                        }
                    };
                    AddGameObjectMove(parameter);
                    return;
                }
            }
        }
    }

    // 扣血 传入一个Room
    public void ReduceHpForRoom(Room room, int damage)
    {
        room.hp -= damage;
        if (room.hp < 0) room.hp = 0;
        room.hpText.text = room.hp.ToString();
        room.hpImage.fillAmount = (float)room.hp / room.maxHp;
        ReduceHpByLikeReplyForPlayer(damage, room.hpText.rectTransform);
        if (room.hp <= 0)
        {
            DestroyRoom(room);
        }

        
    }

    // 随机选择一个存在的房间
    public Room GetRandomRoom()
    {
        if (room.Count == 0 ) return null;
        List<Room> roomsWithPlayers = room.Where(r => r.player != null && r.player.Count > 0).ToList();
        if (roomsWithPlayers.Count == 0) return null;
        int randomIndex = UnityEngine.Random.Range(0, roomsWithPlayers.Count);
        return roomsWithPlayers[randomIndex];
    }

    /// <summary>
    /// 查找玩家所在房间
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GameObject GetPlayerRoom(string name)
    {
        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    return r.doorObj;
                }
            }
        }
        return null;
    }
    /// <summary>
    /// 获取玩家对象
    /// </summary>
    public GameObject GetPlayerGameObject(string name)
    {
        foreach (var r in room)
        {
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    return p.playerObj;
                }
            }
        }
        return null;
    }

    //// 新玩家进入指定房间区域
    //public void NewPlayerAdd(string name, int roomId)
    //{
    //    AddPlayer(name, roomId);
    //}


    // 新玩家进入指定房间
    public bool AddPlayer(string name, int roomId)
    {
        if (GameTime.ins.timeState == TimeState.Night || GameTime.ins.timeState == TimeState.Waiting) return false;

        // 判断玩家上次是不是输入过房间号，如果输入直接进入上次的房间
        //var unassignedPlayer = unassignedPlayers.FirstOrDefault(p => p.name == name);
        //if (unassignedPlayer != null && unassignedPlayer.lastJoinRoomId == roomId - 1)
        //{
        //    room[roomId - 1].player.Add(new Player { name = name, playerObj = unassignedPlayer });
        //    全局UI管理.Instance.显示提示UI($"{name}进入{roomId}号房间", Color.green);
        //    unassignedPlayers.Remove(unassignedPlayer);
        //    PlayerMoveRoomRan(roomId - 1, unassignedPlayer);
        //    return true;
        //}

        // 检查玩家是否已经在房间中
        foreach (var r in room)
        {
            // 房间没玩家，跳过
            if (r.player == null || r.player.Count == 0)
                continue;

            // 必须用 ToList() 遍历副本，否则遍历中删元素会报错
            foreach (Player p in r.player.ToList())
            {
                // 匹配名字 + 目标房间是 roomId-1
                if (p.name == name && p.lastJoinRoomId == roomId - 1)
                {
                    // 如果已经在正确位置，直接返回
                    if (p.lastEnterRoomId == p.lastJoinRoomId)
                    {
                        return true;
                    }

                    // ==============================================
                    // 关键：从【当前所在房间】彻底移除玩家
                    // ==============================================
                    foreach (var oldRoom in room)
                    {
                        if (oldRoom.id == p.lastEnterRoomId)
                        {
                            oldRoom.player.Remove(p);
                            break;
                        }
                    }

                    // ==============================================
                    // 目标房间（roomId-1）
                    // ==============================================
                    var targetRoom = room.FirstOrDefault(ro => ro.id == roomId - 1);
                    if (targetRoom != null)
                    {
                        // 检查是否已存在 → 不存在才添加
                        bool isExist = targetRoom.player.Any(pl => pl.name == name);
                        if (!isExist)
                        {
                            targetRoom.player.Add(p); // 直接用原实例，不创建新对象！
                        }
                    }

                    // ==============================================
                    // 传送 + 更新状态
                    // ==============================================
                    var joinRoom = room.FirstOrDefault(ro => ro.id == p.lastJoinRoomId);
                    if (joinRoom != null)
                    {
                        TransferPlayerToRoomArea(p, joinRoom);
                    }

                    // 更新进入房间ID = 最终所在房间
                    p.lastEnterRoomId = p.lastJoinRoomId;

                    return true;
                }
                else
                {
                    // 不匹配的玩家，统一设置目标房间
                    p.lastJoinRoomId = roomId - 1;
                    return true;
                }
            }
        }




        GameObject newplayer = null;
        bool isPlayerNull = true;
        foreach (var p in unassignedPlayers)
        {
            if (p.name == name)
            {
                newplayer = p;
                isPlayerNull = false;
                continue;
            }
        }
        if (isPlayerNull)
        {
            newplayer = AddPlayer(name,false);
        }
        room[roomId - 1].player.Add(new Player { name = name, playerObj = newplayer, lastJoinRoomId = roomId - 1 });
        全局UI管理.Instance.显示提示UI($"{name}加入{roomId}号房间", Color.green);
        unassignedPlayers.Remove(newplayer);
        return true;
    }

    // 直接加入到对应房间，从移动列表中移除


    /// <summary>
    /// 新玩家随机进入一个房间
    /// </summary>
    /// <param name="name"></param>
    public GameObject AddPlayer(string name,bool isRandRoom = true)
    {
        if (GameTime.ins.timeState == TimeState.Waiting || GameTime.ins.timeState == TimeState.Night || GameTime.ins.timeState == TimeState.Day && GameTime.ins.nowTime <= 6f) return null;

        foreach (var r in room)
        {
            if (r.player == null) break;
            foreach (var p in r.player)
            {
                if (p.name == name)
                {
                    return null;// 已经存在该玩家
                }
            }
        }
        生存天数排行._instance.更新人数(1);

        // 给玩家随机分配一个房间
        //int randomRoomIndex = UnityEngine.Random.Range(0, room.Count);
        全局UI管理.Instance.显示提示UI($"{name}进入了等待区", Color.green);
        //GameTime.ins.AddNightTime();
        // 随机分配一个出生点
        int randomStartPosIndex = UnityEngine.Random.Range(0, listPlayerStartPos.Count);
        // 在玩家预制体中随机选择一个预制体
        int randomPlayerPrefabIndex = UnityEngine.Random.Range(0, playerPrefab.Count);
        GameObject playerObject = Instantiate(playerPrefab[randomPlayerPrefabIndex]);
        playerObject.transform.position = listPlayerStartPos[randomStartPosIndex].transform.position;
        playerObject.transform.Find("玩家UI/nameText").GetComponent<Text>().text = name;
        playerObject.transform.Find("玩家UI/nameText").GetComponent<Text>().fontSize = playerTextNameSize;
        
        //playerObject.transform.Find("玩家UI").GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
        //playerObject.transform.Find("玩家UI/nameText").GetComponent<RectTransform>().position = new Vector3(0f, 0.5f, 0f);
        playerObject.transform.SetParent(playerParent.transform);
        playerObject.name = name;
        playerObject.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "run";
        //playerObject.transform.Find("player").rotation = Quaternion.Euler(45f, 0f, 0f);
        //room[randomRoomIndex].player.Add(new Player { name = name, playerObj = playerObject });
        //playerObject.transform.Find("player").transform.rotation = Quaternion.Euler(0f,playerObject.transform.position.x > room[randomRoomIndex].doorObj.transform.position.x ? 180f : 0f,0f);
        //// 设置玩家图层
        playerObject.transform.Find("player").GetComponent<MeshRenderer>().sortingOrder = playerSortingOrder++;
        // 设置世界UI图层
        playerObject.transform.Find("玩家UI").GetComponent<Canvas>().sortingOrder = 9999;


        var tagetPos = GenerateRandomPosInRoomArea(playerWaitArea);
        playerObject.transform.Find("player").transform.rotation = Quaternion.Euler(0f, playerObject.transform.position.x > tagetPos.x ? 180f : 0f, 0f);
        MoveGameObjectParameter parameter = new MoveGameObjectParameter
        {
            speed = playerMoveSpeed,
            gameObject = playerObject,
            v3Target = tagetPos,
            rot = playerObject.transform.Find("player"),
            action = () =>
            {
                playerObject.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "idle";
                //playerObject.transform.Find("player").transform.rotation = Quaternion.Euler(0f, playerObject.transform.position.x > tagetPos.x ? 180f : 0f, 0f);
                //MoveGameObjectParameter parameter = new MoveGameObjectParameter
                //{
                //    speed = playerMoveSpeed,
                //    gameObject = playerObject,
                //    v3Target = GenerateRandomPosInRoomArea(playerRandomRoomArea[randomRoomIndex]),
                //    rot = playerObject.transform.Find("player"),
                //    action = () =>
                //    {
                //        playerObject.transform.Find("player").GetComponent<SkeletonAnimation>().AnimationName = "idle";
                //        //playerObject.transform.Find("player").rotation = Quaternion.Euler(-45f, 180f, 0f);
                //    }
                //};

                //// 设置玩家图片朝向，根据目标位置设置图片x翻转
                //playerObject.transform.Find("player").transform.rotation = Quaternion.Euler(0f, playerObject.transform.position.x > room[randomRoomIndex].doorObj.transform.position.x ? 180f : 0f, 0f);

                ////给对应房间加血
                //int addHp = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.extraHealthPerPersonForDoor;
                //GameObject go = HpUIPond.Instance.GetAddHpObj();
                //go.GetComponent<Text>().text = "+" + addHp.ToString();
                //go.GetComponent<RectTransform>().position = room[randomRoomIndex].hpText.GetComponent<RectTransform>().position;
                //go.SetActive(true);
                //// 更新对应UI
                //room[randomRoomIndex].hp += addHp;
                //if (room[randomRoomIndex].hp > room[randomRoomIndex].maxHp)
                //{
                //    room[randomRoomIndex].maxHp = room[randomRoomIndex].hp;
                //}
                //room[randomRoomIndex].hpText.text = room[randomRoomIndex].hp.ToString();
                //room[randomRoomIndex].hpImage.fillAmount = (float)room[randomRoomIndex].hp / room[randomRoomIndex].maxHp;

                //HpUIPond.Instance.RecycleObjWithDelay(go, 1f, 0);

                //AddGameObjectMove(parameter);
            }
        };
        unassignedPlayers.Add(playerObject);
        AddGameObjectMove(parameter);
        return playerObject;
    }


    // 给玩家随机分配一个房间
    public void AddPlayerRandomToRoom(GameObject player)
    {
        int randomRoomIndex = UnityEngine.Random.Range(0, room.Count);
        //player.lastJoinRoomId = randomRoomIndex;
        room[randomRoomIndex].player.Add(new Player { name = player.name, playerObj = player });
        全局UI管理.Instance.显示提示UI($"{player.name}加入{randomRoomIndex + 1}号房间", Color.green);
        //unassignedPlayers.Remove(playerObj);
    }

    /// <summary>
    /// 添加移动对象
    /// </summary>
    /// <param name="hpGameObject"></param>
    /// <param name="targetGameObject"></param>
    public void AddGameObjectMove(MoveGameObjectParameter moveparameter)
    {
        moveGameObjects.Add(moveparameter);
    }

    /// <summary>
    /// 批量移动对象
    /// </summary>
    public void MoveGameObjectParameter()
    {
        
        if (moveGameObjects == null || moveGameObjects.Count <= 0)
        {
            //Debug.LogWarning("玩家-血条映射字典为空，跳过玩家移动逻辑");
            return;
        }
        foreach (var move in moveGameObjects.ToList())
        {
            if (move == null)
            {
                moveGameObjects.Remove(move);
                Debug.LogWarning("移动参数对象为空，已移除");
                continue;
            }
            GameObject playerObj = move.gameObject;
            GameObject targetHpObj = move.target;
            Vector3 targetPos = Vector3.zero;
            if (playerObj == null)
            {
                Debug.LogWarning($"玩家对象或目标血条对象为空，移除键：{move.gameObject}");
                moveGameObjects.Remove(move);
                continue;
            }

            if (targetHpObj == null)
            {
                targetPos = move.v3Target;
            }
            else
            {
                targetPos = targetHpObj.transform.position;
            }
            Vector3 currentPos = playerObj.transform.position;
            float moveStep = move.speed * Time.deltaTime;
            Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, moveStep);
            float distance = Vector3.Distance(currentPos, targetPos);
            //if(move.rot != null)
            //{
            //    Vector3 direction = (targetPos - currentPos).normalized;
            //    if (direction != Vector3.zero)
            //    {
            //        Quaternion targetRotation = Quaternion.LookRotation(direction);
            //        move.rot.rotation = targetRotation;
            //    }
            //}
            if (distance <= moveStep)
            {
                playerObj.transform.position = targetPos;
                moveGameObjects.Remove(move);
                move.action?.Invoke();
            }
            else
            {
                playerObj.transform.position = newPos;
            }
        }
    }

    public Vector3 GenerateRandomPosInRoomArea(Vector4 selectedArea)
    {

        if (playerRandomRoomArea == null || playerRandomRoomArea.Count == 0)
        {
            Debug.LogError("playerRandomRoomArea 列表为空，无法生成随机位置！");
            return Vector3.zero;
        }

        // ------------- 核心修改：2D坐标映射 -------------
        // 原3D的X/Z轴 → 2D的X/Y轴
        float left = selectedArea.x;    // 左边界（X轴最小值）
        float bottom = selectedArea.y;  // 下边界（Y轴最小值）
        float right = selectedArea.z;   // 右边界（X轴最大值）
        float top = selectedArea.w;     // 上边界（Y轴最大值）

        // 计算X/Y轴的最小/最大值（防止区域坐标写反）
        float minX = Mathf.Min(left, right);
        float maxX = Mathf.Max(left, right);
        float minY = Mathf.Min(bottom, top);
        float maxY = Mathf.Max(bottom, top);

        // 在2D区域内生成随机X/Y值
        float randomX = UnityEngine.Random.Range(minX, maxX);
        float randomY = UnityEngine.Random.Range(minY, maxY);

        // 2D场景下Z轴固定为0（如果你的2D物体有固定Z轴偏移，可改成比如-10）
        Vector3 randomPos = new Vector3(randomX, randomY, 0);
        return randomPos;
    }

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.green;
        DrawAreaGizmo(playerWaitArea);

        Gizmos.color = Color.blue;
        foreach (Vector4 area in playerRandomRoomArea)
        {
            DrawAreaGizmo(area);
        }
    }

    private void DrawAreaGizmo(Vector4 area)
    {
        float left = area.x;
        float bottom = area.y;
        float right = area.z;
        float top = area.w;

        Vector2 bl = new Vector2(left, bottom);
        Vector2 tl = new Vector2(left, top);
        Vector2 tr = new Vector2(right, top);
        Vector2 br = new Vector2(right, bottom);

        Gizmos.DrawLine(bl, tl);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
    }

    private Vector3 ToVector3(Vector2 vec2)
    {
        return new Vector3(vec2.x, vec2.y, 0);
    }
}
[System.Serializable]
public class MoveGameObjectParameter
{
    /// <summary>
    /// 速度
    /// </summary>
    public float speed;
    /// <summary>
    /// 需要移动的目标
    /// </summary>
    public GameObject gameObject;
    /// <summary>
    /// 目标位置 对象类型
    /// </summary>
    public GameObject target;
    /// <summary>
    /// 目标位置 v3类型
    /// </summary>
    public Vector3 v3Target;
    /// <summary>
    /// 需要控制的角色朝向
    /// </summary>
    public Transform rot;
    /// <summary>
    /// 到达目标点后的回调事件
    /// </summary>
    public Action action;
}