# 🎮 dyYeyouGame - 猛鬼游戏

一款基于 Unity 的多人鬼怪防守游戏，集成了直播互动功能的创新游戏体验。

## 📋 项目概述

**dyYeyouGame** 是一个融合了游戏与直播互动的创意项目。玩家需要协作防守门窗，抵挡来自鬼怪的攻击。游戏支持多人在线游玩，直播间观众可以通过礼物赠送和弹幕互动来影响游戏进程。

### 🎯 核心特性

- ✅ **多人协作玩法** - 支持多个玩家协作防守
- ✅ **鬼怪AI系统** - 动态的敌人行为和攻击模式
- ✅ **直播互动** - 观众可通过赠送礼物和弹幕参与游戏
- ✅ **排名系统** - 完整的玩家排行榜数据
- ✅ **可配置游戏参数** - 灵活的游戏难度调整

## 📁 项目结构

dyYeyouGame/ ├── Assets/ # Unity 资源目录 ├── Packages/ # 项目依赖包 ├── ProjectSettings/ # Unity 项目设置 ├── GameInitData.json # 游戏初始化配置文件 ├── RankList.json # 玩家排行榜数据 ├── main.py # 数据处理脚本 ├── Newtonsoft.Json.dll # JSON 序列化库 ├── 猛鬼游戏配置.exe # 游戏配置工具 ├── 玩家名称.oldxia # 玩家名单数据文件 ├── 下载须知.txt # 使用说明 └── .vsconfig # Visual Studio 配置

Code

## 🔧 游戏配置参数

游戏配置位于 `GameInitData.json` 中，主要参数说明：

### 礼物配置
```json
"giftData": [
  {
    "name": "小心心",           // 礼物名称
    "replyVolume": 10          // 礼物价值/数量
  }
]
治疗机制
JSON
"chanceOfHealing": {
  "likeReplyProbability": 90,      // 点赞回复触发概率
  "likeReplyValue": 1,             // 点赞回复回复数量
  "BulletReplyProbability": 90,    // 弹幕回复触发概率
  "BulletReplyValue": 1            // 弹幕回复数量
}
游戏难度参数
参数	含义	默认值
GhostAttackCount	每个鬼怪的攻击次数	5
GhostMinAttack	鬼怪最小伤害	10
GhostMaxAttack	鬼怪最大伤害	50
DoorInitialHealth	门的初始血量	1000
ZombieRageAttack1/2/3	僵尸不同阶段的愤怒攻击	1
LobbyWaitCountdown	大厅等待倒计时(秒)	30
BaseNightEndCountdown	夜晚基础时长(秒)	30
ExtraNightDurationPerPerson	每增加一人的额外夜晚时长	1
MiniNpcCount	最小NPC数量	5
MaxNpcCount	最大NPC数量	10
🚀 快速开始
前置要求
Unity 2020.3 LTS 或更高版本
.NET Framework 4.7.1+
Visual Studio（可选，用于代码编辑）
安装步骤
克隆或下载项目

bash
git clone https://github.com/xiajunxiong/dyYeyouGame.git
在 Unity 中打开项目

使用 Unity Hub 打开项目目录
等待项目加载完成（首次加载可能需要较长时间）
运行游戏

在 Unity Editor 中点击 Play 按钮运行
或构建成可执行文件进行部署
配置游戏参数
使用 猛鬼游戏配置.exe 工具可视化修改游戏参数：

bash
./猛鬼游戏配置.exe
或直接编辑 GameInitData.json 文件来调整游戏难度和机制。

📊 数据文件说明
GameInitData.json
游戏的核心配置文件，包含：

礼物数据定义
治疗和恢复机制
游戏难度参数
RankList.json
玩家排行榜数据，包含：

玩家名称和排名
游戏成绩
通关记录
玩家名称.oldxia
玩家昵称列表，格式为逗号分隔。

🐍 数据处理脚本
main.py 用于清理玩家名单数据：

bash
python main.py
功能：

读取 玩家名称.oldxia 文件
移除格式错误和空白行
清理名称前缀（如"、"分隔符）
输出清理后的结果
⚠️ 重要说明
📌 当前版本限制
这是一个 演示版本，具有以下限制：

配套服务端未上传 - 项目缺少后端服务器代码

多人在线功能需要连接到服务端
排行榜数据同步功能依赖服务端
直播互动集成需要配置的服务接口
可直接运行 - 本地模式下可以运行游戏

支持单机游戏测试
可测试游戏配置参数
适合开发和演示使用
需要完整部署

如需完整的在线多人功能，需要单独开发服务端
可参考项目配置文件的结构自行实现服务端
推荐服务端架构：Node.js + Socket.io / C# + Netcode
🔌 服务端集成建议
若要启用完整功能，建议配置以下内容：

WebSocket/Socket 连接 - 实现实时多人通信
用户认证系统 - 管理玩家身份
数据库 - 存储排行榜和游戏记录
直播平台 API - 集成礼物和弹幕系统
🌐 网络配置
根据项目说明，演示网站可访问：

Code
http://8.137.38.23:3000/
🛠️ 开发工具
引擎: Unity（C#）
序列化: Newtonsoft.Json (Json.NET)
数据处理: Python 3.x
配置管理: JSON 格式
📝 文件说明
文件名	说明
下载须知.txt	快速开始说明
.vsconfig	Visual Studio 推荐配置
.gitignore	Git 忽略规则
🎓 使用建议
用途一：游戏开发学习
学习 Unity 多人游戏架构
研究 AI 敌人行为设计
理解游戏参数平衡
用途二：直播互动整合
参考直播礼物和弹幕集成方案
学习观众参与游戏的设计思路
用途三：演示和展示
完整的游戏演示案例
配套的管理配置工具