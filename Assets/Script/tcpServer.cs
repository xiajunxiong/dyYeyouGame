using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServer : MonoBehaviour
{
    private string ip = "127.0.0.1";
    private int port = 8888;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;

    private Thread listenThread;
    private Thread receiveThread;

    private bool isListening = true;

    void Start()
    {
        StartServer();
    }

    void StartServer()
    {
        listenThread = new Thread(ListenLoop);
        listenThread.IsBackground = true;
        listenThread.Start();
        Debug.Log("✅ TCP 服务端已启动: " + ip + ":" + port);
    }

    /// <summary>
    /// 循环监听（断开后自动重新监听）
    /// </summary>
    void ListenLoop()
    {
        try
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start();

            while (isListening)
            {
                client = server.AcceptTcpClient();
                Debug.Log("✅ 客户端已连接!");

                stream = client.GetStream();
                ReceiveLoop();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("服务端异常: " + e.Message);
        }
    }

    /// <summary>
    /// 接收消息循环
    /// </summary>
    void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                try
                {
                    int len = stream.Read(buffer, 0, buffer.Length);
                    if (len <= 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, len);
                    BarrageData data = JsonConvert.DeserializeObject<BarrageData>(json);

                    switch (data.MsgType)
                    {
                        // 1. 弹幕发言
                        case "comment":
                            string msg = data.Content.Trim();

                            //string numStr = msg.Replace("加入", "").Trim();

                            if (int.TryParse(msg, out int roomId) && roomId >= 1 && roomId <= 4) // 输入1-4加入对应房间
                            {
                                MainThreadDispatcher.RunOnMainThread(() =>
                                {
                                    RoomManager.ins.AddPlayer(data.UserName, roomId);
                                });
                            }

                            if (msg.StartsWith("加入")) // 加入大厅（不分房间）
                            {
                                MainThreadDispatcher.RunOnMainThread(() =>
                                {
                                    RoomManager.ins.AddPlayer(data.UserName);
                                });
                            }

                            if (msg.StartsWith("666") || msg.StartsWith("888")) // 发送666或888增加血量
                            {
                                MainThreadDispatcher.RunOnMainThread(() =>
                                {
                                    RoomManager.ins.AddHpByDanmakuForPlayer(data.UserName);
                                });
                            }
                            MainThreadDispatcher.RunOnMainThread(() =>
                            {
                                RoomManager.ins.ShowDanmaku(data.UserName, data.Content);
                            });
                            

                            Debug.Log($"💬 弹幕发言：\n用户名：{data.UserName}\n内容：{data.Content}\n");
                            break;

                        // 2. 礼物
                        case "gift":
                            MainThreadDispatcher.RunOnMainThread(() =>
                            {
                                RoomManager.ins.AddHpByGiftForPlayer(data.UserName, data.GiftName);
                            });

                            Debug.Log($"🎁 礼物赠送：\n用户名：{data.UserName}\n礼物：{data.GiftName} \n数量： {data.GiftCount}\n");
                            break;

                        // 3. 点赞
                        case "like":
                            MainThreadDispatcher.RunOnMainThread(() =>
                            {
                                RoomManager.ins.AddHpByLikeReplyForPlayer(data.UserName);
                            });
                            Debug.Log($"❤️ 点赞：\n用户名：{data.UserName}\n点赞数：{data.GiftCount}\n");
                            break;

                        // 4. 关注
                        case "follow":
                            MainThreadDispatcher.RunOnMainThread(() =>
                            {

                            });
                            Debug.Log($"⭐ 新关注：\n用户名：{data.UserName}\n");
                            break;

                        // 5. 进入直播间
                        case "member":
                            //MainThreadDispatcher.RunOnMainThread(() =>
                            //{
                            //    RoomManager.ins.AddPlayer(data.UserName);
                            //});
                            Debug.Log($"🚪 进入直播间：\n用户名：{data.UserName}\n");
                            break;

                        // 6. 直播间状态
                        case "status":
                            MainThreadDispatcher.RunOnMainThread(() =>
                            {

                            });
                            Debug.Log($"📶 直播间状态：\n状态：{data.Content}\n状态码：{data.StatusCode}\n");
                            break;

                        // 7. 直播间统计
                        case "stat":
                            MainThreadDispatcher.RunOnMainThread(() =>
                            {

                            });
                            Debug.Log($"📊 直播统计：\n在线人数：{data.TotalUser}\n累计人数：{data.Total}\n");
                            break;

                        default:
                            Debug.Log($"❓ 未知消息类型：{data.MsgType}\n");
                            break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }
        catch
        {
            Debug.LogWarning("⚠️ 客户端断开连接");
        }
        finally
        {
            CloseClient();
        }
    }

    /// <summary>
    /// 发送消息给客户端
    /// </summary>
    public void SendMsg(string msg)
    {
        if (stream == null || client == null || !client.Connected)
        {
            Debug.LogWarning("无客户端连接，无法发送");
            return;
        }

        try
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
            Debug.Log("📤 已发送: " + msg);
        }
        catch
        {
            Debug.LogError("发送失败");
        }
    }

    void CloseClient()
    {
        if (stream != null) { stream.Close(); stream = null; }
        if (client != null) { client.Close(); client = null; }
        Debug.Log("✅ 客户端已清理，等待重连...");
    }

    void OnDestroy()
    {
        isListening = false;

        if (server != null) server.Stop();
        CloseClient();

        if (listenThread != null) listenThread.Abort();
        if (receiveThread != null) receiveThread.Abort();

        Debug.Log("❌ 服务端已关闭");
    }
}