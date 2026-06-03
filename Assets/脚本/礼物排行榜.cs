using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 用于Distinct去重
using UnityEngine;
using UnityEngine.UI;
public enum GiftType
{
    小心心,
    玫瑰,
    入团卡,
    点亮粉丝团,
    粉丝灯牌,
    啤酒,
    棒棒糖,
    墨镜,
    热气球,
    跑车,
    火箭,
    嘉年华,
    亲吻,
    比心,
    抱抱,
    抖音一号,
    飞机,
    穿云箭,
    纸鹤,
    爱的守护,
    保时捷,
    浪漫花火,
    万象烟花,
    星辰大海,
    真的爱你,
    私人飞机,
    梦幻城堡,
    璀璨舞台,
    告白气球,
    心动连线,
    为你打call,
    加油鸭,
    小皇冠,
    掌上明珠,
    招财猫,
    锦鲤,
    爱神之箭,
    流星雨,
    宇宙之心,
    浪漫马车,
    黄金跑车,
    天使之翼,
    心动外卖,
    甜蜜陪伴
}
public class 礼物排行榜 : MonoBehaviour
{
    public List<礼物排行榜数据> 礼物排行榜数据列表 = new List<礼物排行榜数据>();
    private List<礼物排行榜数据> 前5排行榜 = new List<礼物排行榜数据>(); // 前10缓存（核心维护）
    private 礼物排行榜数据 第5名;
    private List<Text> 显示排行Text = new List<Text>();
    [Header("测试配置-追加礼物价值范围")]
    public int 测试追加最小值 = 200;
    public int 测试追加最大值 = 800;
    public int 显示排名 = 10;
    public static 礼物排行榜 ins;

    private Dictionary<GiftType, int> GiftValueTable = new Dictionary<GiftType, int>()
{
    { GiftType.小心心, 1 },
    {GiftType.点亮粉丝团,1 },
    {GiftType.入团卡,1 },
    { GiftType.玫瑰, 2 },
    { GiftType.粉丝灯牌, 1 },
    { GiftType.啤酒, 10 },
    { GiftType.棒棒糖, 3 },
    { GiftType.墨镜, 20 },
    { GiftType.热气球, 50 },
    { GiftType.跑车, 120 },
    { GiftType.火箭, 2000 },
    { GiftType.嘉年华, 30000 },
    { GiftType.亲吻, 10 },
    { GiftType.比心, 20 },
    { GiftType.抱抱, 10 },
    { GiftType.抖音一号, 1000 },
    { GiftType.飞机, 600 },
    { GiftType.穿云箭, 2999 },
    { GiftType.纸鹤, 10 },
    { GiftType.爱的守护, 100 },
    { GiftType.保时捷, 200 },
    { GiftType.浪漫花火, 520 },
    { GiftType.万象烟花, 1314 },
    { GiftType.星辰大海, 5000 },
    { GiftType.真的爱你, 100 },
    { GiftType.私人飞机, 1000 },
    { GiftType.梦幻城堡, 100000 },
    { GiftType.璀璨舞台, 5000 },
    { GiftType.告白气球, 50 },
    { GiftType.心动连线, 200 },
    { GiftType.为你打call, 10 },
    { GiftType.加油鸭, 10 },
    { GiftType.小皇冠, 30 },
    { GiftType.掌上明珠, 500 },
    { GiftType.招财猫, 200 },
    { GiftType.锦鲤, 500 },
    { GiftType.爱神之箭, 100 },
    { GiftType.流星雨, 2000 },
    { GiftType.宇宙之心, 99999 },
    { GiftType.浪漫马车, 50000 },
    { GiftType.黄金跑车, 5000 },
    { GiftType.天使之翼, 2000 },
    { GiftType.心动外卖, 100 },
    { GiftType.甜蜜陪伴, 50 }
};

    private void Awake()
    {
        ins = this;

        // 获取UI排名文本组件
        if (transform.Find("排名") != null)
        {
            foreach (Transform t in transform.Find("排名"))
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

        //for (int i = 0; i < 20; i++)
        //{
        //    string 玩家名 = RoomManager.ins.debugplayername[UnityEngine.Random.Range(0, RoomManager.ins.debugplayername.Count)];
        //    Array allGifts = Enum.GetValues(typeof(GiftType));
        //    GiftType randomGift = (GiftType)allGifts.GetValue(UnityEngine.Random.Range(0, allGifts.Length));
        //    string 礼物名 = randomGift.ToString();

        //    玩家赠送礼物(玩家名, 礼物名);
        //}

        初始化前10排行榜();
        Debug.Log("排行榜初始化完成，总玩家数：" + 礼物排行榜数据列表.Count + "，按L键为随机玩家追加礼物价值测试！");
    }

    private void Update()
    {
        // 按L键触发测试：随机选已有玩家追加礼物价值
        if (Input.GetKeyDown(KeyCode.L))
        {
            测试_为随机玩家追加礼物价值();
        }

    }

    // 重置礼物排行榜数据
    public void 重置礼物排行榜数据()
    {
        礼物排行榜数据列表.Clear();
        前5排行榜.Clear();
        第5名 = null;
        更新排行榜UI();
    }

    /// <summary>
    /// 测试方法：随机选择一个已存在的玩家，追加随机礼物价值
    /// </summary>
    private void 测试_为随机玩家追加礼物价值()
    {
        //if (礼物排行榜数据列表.Count == 0)
        //{
        //    Debug.LogWarning("总榜无玩家数据，无法测试！");
        //    return;
        //}
        int 随机索引 = UnityEngine.Random.Range(0, 礼物排行榜数据列表.Count);
        //礼物排行榜数据 随机玩家 = 礼物排行榜数据列表[随机索引];

        string wanjia = RoomManager.ins.debugplayername[UnityEngine.Random.Range(0, RoomManager.ins.debugplayername.Count)];

        Array allGifts = Enum.GetValues(typeof(GiftType));
        GiftType randomGift = (GiftType)allGifts.GetValue(UnityEngine.Random.Range(0, allGifts.Length));
        string 礼物名 = UnityEngine.Random.value < 0.5f? randomGift.ToString() : "小心心";

        // 调用你真正的送礼方法（测试用）
        玩家赠送礼物(wanjia, 礼物名);

        //Debug.Log($"=====测试送礼===== 【{随机玩家.玩家名称}】 赠送礼物：{礼物名}");
    }

    public void 玩家赠送礼物(string 玩家名称, string 礼物名称)
    {
        if (System.Enum.TryParse(礼物名称, out GiftType 礼物) && GiftValueTable.ContainsKey(礼物))
        {
            int 价值 = GiftValueTable[礼物];
            添加礼物数据(玩家名称, 价值);
            //RoomManager.ins.AddHpByGiftForPlayer(玩家名称, 礼物名称);
            Debug.Log($"【送礼成功】{玩家名称} 赠送 {礼物名称}，增加 {价值} 积分");
        }
        else
        {
            Debug.LogWarning($"【礼物不存在】：{礼物名称}");
        }
    }

    /// <summary>
    /// 核心方法：添加/追加礼物数据，触发冲榜逻辑
    /// </summary>
    private void 添加礼物数据(string 玩家名称, int 礼物价值)
    {
        // 1. 查找玩家是否存在（总榜查找，总榜不排序、不修改）
        礼物排行榜数据 目标玩家 = 礼物排行榜数据列表.Find(data => data.玩家名称 == 玩家名称);
        if (目标玩家 != null)
        {
            目标玩家.礼物价值 += 礼物价值;
        }
        else
        {
            目标玩家 = new 礼物排行榜数据 { 玩家名称 = 玩家名称, 礼物价值 = 礼物价值 };
            礼物排行榜数据列表.Add(目标玩家);
        }

        // 2. 【修复核心1】先去重：保证缓存内没有重复玩家
        前5排行榜 = 前5排行榜.Distinct().ToList();

        // 3. 核心逻辑：处理前10缓存
        // 3.1 先判断玩家是否已经在缓存里
        bool 玩家已在缓存 = 前5排行榜.Contains(目标玩家);

        if (玩家已在缓存)
        {
            对前5排行榜排序();
            Debug.Log($"【{目标玩家.玩家名称}】已在榜内，加分后重新排序");
        }
        else
        {
            if (前5排行榜.Count < 显示排名)
            {
                前5排行榜.Add(目标玩家);
                对前5排行榜排序();
                Debug.Log($"【{目标玩家.玩家名称}】新入榜，当前榜内人数：{前5排行榜.Count}");
            }
            else
            {

                第5名 = 前5排行榜[显示排名 - 1];
                if (目标玩家.礼物价值 > 第5名.礼物价值)
                {
                    Debug.Log($"【{目标玩家.玩家名称}】冲榜成功，淘汰第5名【{第5名.玩家名称}】（价值：{第5名.礼物价值}）");
                    前5排行榜.RemoveAt(显示排名 - 1);
                    前5排行榜.Add(目标玩家);
                    对前5排行榜排序();
                }
                else
                {
                    Debug.Log($"【{目标玩家.玩家名称}】追加后价值不足，未进入前5（当前第5名价值：{第5名.礼物价值}）");
                }
            }
        }

        前5排行榜 = 前5排行榜.Distinct().ToList();

        更新排行榜UI();
    }

    private void 初始化前10排行榜()
    {
        前5排行榜.Clear();
        List<礼物排行榜数据> 临时总榜 = new List<礼物排行榜数据>(礼物排行榜数据列表);
        临时总榜.Sort((a, b) => b.礼物价值.CompareTo(a.礼物价值));
        临时总榜 = 临时总榜.Distinct().ToList();
        int 取数数量 = Mathf.Min(显示排名, 临时总榜.Count);
        for (int i = 0; i < 取数数量; i++)
        {
            前5排行榜.Add(临时总榜[i]);
        }
        //更新排行榜UI();
        Debug.Log("前10缓存初始化完成，初始顺序已按价值降序排列");
    }

    private void 对前5排行榜排序()
    {
        前5排行榜.Sort((a, b) => b.礼物价值.CompareTo(a.礼物价值));
        // 同步更新第10名引用
        if (前5排行榜.Count >= 显示排名)
        {
            第5名 = 前5排行榜[显示排名 - 1];
        }
        else
        {
            第5名 = null;
        }
    }

    private void 更新排行榜UI()
    {
        for (int i = 0; i < 显示排行Text.Count; i++)
        {
            if (i < 前5排行榜.Count)
            {
                var data = 前5排行榜[i];
                string name = data.玩家名称.Length > 6 ? data.玩家名称.Substring(0, 6) : data.玩家名称;
                显示排行Text[i].text = $"{name} - {data.礼物价值}";
            }
            else
            {
                显示排行Text[i].text = $"暂无";
            }
        }
    }
}

public class 礼物排行榜数据
{
    public int 排名;
    public string 玩家名称;
    public int 礼物价值;

    public override bool Equals(object obj)
    {
        if (obj is 礼物排行榜数据 other)
        {
            return 玩家名称 == other.玩家名称;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return 玩家名称.GetHashCode();
    }
}