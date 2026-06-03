using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
public class BarrageData
{
    [JsonProperty("msg_type")]
    public string MsgType { get; set; } // comment/gift/like/follow/member/status/stat

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; } // 时间戳（毫秒）

    [JsonProperty("user_name")]
    public string UserName { get; set; } = "";

    [JsonProperty("content")]
    public string Content { get; set; } = "";

    [JsonProperty("gift_name")]
    public string GiftName { get; set; } = "";

    [JsonProperty("gift_count")]
    public int GiftCount { get; set; } = 0;

    [JsonProperty("status_code")]
    public int StatusCode { get; set; } = 0;

    [JsonProperty("total_user")]
    public int TotalUser { get; set; } = 0;

    [JsonProperty("total")]
    public int Total { get; set; } = 0;
}

public class UrlClientManager : MonoBehaviour
{
    private const string ForwardTargetUrl = "http://127.0.0.1:5001/url/"; // 游戏端地址
    private const string ServerUrl = "http://localhost:8080"; // 服务端地址（收URL的地址）
    private const string ClientListenPrefix = "http://localhost:5000/receive/"; // 客户端监听地址（5000端口）
    private CancellationTokenSource _cts;
    private HttpListener _listener;
    private bool _isListenerRunning = false;

    public static UrlClientManager Instance;
    public Text text;

    public class UrpData
    {
        public string urpUrl;
        public int senderProcessId;
        public string timestamp;
        public string clientListenUrl;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cts = new CancellationTokenSource();

        if (text != null)
        {
            text.text = "=== URL客户端已初始化 ===\n";
        }
        else
        {
            Debug.LogError("URLClientManager：请在Inspector关联Text组件！");
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        Task.Delay(100).Wait();
        StopListener();
        _cts?.Dispose();
        _cts = null;
        Instance = null;
    }

    public async void StartUrlClient(UrpData urpDataToSend)
    {
        if (text != null) text.text = "";
        int[] checkPorts = { 8080, 5000 };

        foreach (var port in checkPorts)
        {
            if (IsPortInUse(port))
            {
                string errMsg = $"端口{port}已被占用！请关闭占用程序或更换端口";
                AppendLog(errMsg);
                Debug.LogWarning(errMsg);
                return;
            }
        }

        try
        {
            bool urpSentSuccess = await SendUrpToServer(urpDataToSend);
            if (!urpSentSuccess)
            {
                Debug.LogWarning("URL发送失败，无法启动监听（退出流程）");
                return;
            }

            await StartClientListener();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"\n客户端全局异常：{ex.Message}");
            Debug.LogWarning($"异常详情：{ex.StackTrace}");
        }
        finally
        {
            Debug.LogWarning("\n客户端流程结束");
        }
    }

    private bool IsPortInUse(int port)
    {
        try
        {
            using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                                                             System.Net.Sockets.SocketType.Stream,
                                                             System.Net.Sockets.ProtocolType.Tcp))
            {
                socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
                socket.Close();
                return false; // 端口未被占用
            }
        }
        catch
        {
            return true; // 端口已被占用
        }
    }

    private async Task<bool> SendUrpToServer(UrpData urpData)
    {
        try
        {
            using (var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ServerUrl),
                Timeout = TimeSpan.FromSeconds(15)
            })
            {
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                string jsonData = JsonConvert.SerializeObject(urpData, Formatting.Indented);
                //AppendLog(jsonData);
                Debug.Log(jsonData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync("/url", content, _cts.Token);

                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log(responseContent);

                return true;
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.LogWarning($"\nURL发送失败：服务端未响应（可能服务端未启动或地址错误）");
            Debug.LogWarning($"错误详情：{ex.Message}");
            return false;
        }
        catch (TaskCanceledException)
        {
            Debug.LogWarning($"\nURL发送超时：请求超过15秒未响应");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"\nURL发送异常：{ex.Message}");
            Debug.LogWarning($"异常详情：{ex.StackTrace}");
            return false;
        }
    }

    private async Task StartClientListener()
    {
        if (_isListenerRunning) return;

        _listener = new HttpListener();
        _listener.Prefixes.Add(ClientListenPrefix);

        try
        {
            _listener.Start();
            _isListenerRunning = true;
            AppendLog($"客户端5000端口监听已启动（{ClientListenPrefix}），等待服务端回传消息...");

            while (!_cts.Token.IsCancellationRequested)
            {
                HttpListenerContext context = await _listener.GetContextAsync().WithCancellation(_cts.Token);
                _ = ProcessServerMessageAsync(context, _cts.Token);
            }
        }
        catch (HttpListenerException ex)
        {
            Debug.LogWarning($"\n监听启动失败：{ex.Message}（建议更换端口，如5001）");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("\n客户端监听已主动停止");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"\n监听异常：{ex.Message}");
            Debug.LogWarning($"异常详情：{ex.StackTrace}");
        }
        finally
        {
            StopListener();
            _isListenerRunning = false;
        }
    }

    private void StopListener()
    {
        if (_listener != null)
        {
            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                    AppendLog("\n客户端监听已停止（端口已释放）");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"停止监听时异常：{ex.Message}");
            }
            finally
            {
                _listener = null;
                _isListenerRunning = false;
            }
        }
    }

    private async Task ProcessServerMessageAsync(HttpListenerContext context, CancellationToken token)
    {
        try
        {
            string messageContent;
            using (var reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                messageContent = await reader.ReadToEndAsync();
            }

            if (context.Request.HttpMethod != "POST")
            {
                Debug.LogWarning($"\n收到无效请求：{context.Request.HttpMethod}（只支持POST）");
                SendListenerResponse(context, 405, "只支持POST请求");
                return;
            }


            BarrageData barrageData = ParseRawMessageToBarrageData(messageContent);
            string jsonData = JsonConvert.SerializeObject(barrageData, Formatting.Indented);

            //UIConfiguration.ins.HandleBulletScreenButton(jsonData);//处理按键



            AppendLog(jsonData); //这里是接受弹幕
            //Danmu.Instance.ParseWithNewtonsoft(jsonData);



			await ForwardMessageTo5001(jsonData);

            SendListenerResponse(context, 200, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"\n处理服务端消息异常：{ex.Message}");
            Debug.LogWarning($"异常详情：{ex.StackTrace}");
            SendListenerResponse(context, 500, $"处理失败：{ex.Message}");
        }
        finally
        {
            context.Response.OutputStream?.Close();
        }
    }

    private BarrageData ParseRawMessageToBarrageData(string rawMessage)
    {
        BarrageData data = new BarrageData
        {
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() // 设置当前时间戳（毫秒）
        };

        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            Debug.LogWarning("原始消息为空，返回空的标准化数据");
            return data;
        }

        try
        {
            string[] typeSplit = rawMessage.Split(new[] { ':' }, 2);
            if (typeSplit.Length < 2)
            {
                Debug.LogWarning($"消息格式错误，无法解析：{rawMessage}");
                return data;
            }

            string msgType = typeSplit[0].Trim().Replace("D", "");
            string content = typeSplit[1].Trim();
            Debug.Log("解析消息类型：" + msgType);
            switch (msgType)
            {
                case "弹幕":
                    data.MsgType = "comment";
                    string[] commentParts = content.Split(new[] { ',' }, 2);
                    //if (commentParts.Length >= 2)
                    //{
                    //    data.UserName = commentParts[0].Trim();
                    //    data.Content = commentParts[1].Trim();
                    //    string msg = data.Content.Trim();

                    //    if (msg.StartsWith("加入"))
                    //    {
                    //        string numStr = msg.Replace("加入", "").Trim();

                    //        if (int.TryParse(numStr, out int roomId) && roomId >= 1 && roomId <= 4)
                    //        {
                    //            RoomManager.ins.AddPlayer(data.UserName, roomId);
                    //        }
                    //    }
                    //}
                    break;

                case "礼物":
                    data.MsgType = "gift";
                    string[] giftParts = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (giftParts.Length >= 3)
                    {
                        // 【正确解析】用户名、数量、礼物名
                        string userName = giftParts[0].Trim();          // 用户名：保留原样，不去掉空格
                        string giftCountStr = giftParts[1].Trim();
                        string giftName = giftParts[2].Trim();          // 礼物名

                        int.TryParse(giftCountStr, out int giftCount);

                        data.UserName = userName;
                        data.GiftCount = giftCount;
                        data.GiftName = giftName;

                        Debug.Log($"送礼人：{userName} | 数量：{giftCount} | 礼物：{giftName}");

                        //RoomManager.ins.AddHpByGiftForPlayer(data.UserName, data.GiftName);
                    }
                    break;

                case "点赞":
                    data.MsgType = "like";
                    string[] likeParts = content.Split(new[] { ',' }, 2);
                    if (likeParts.Length >= 2)
                    {
                        data.UserName = likeParts[0].Trim().Replace(" ", "");
                        int.TryParse(likeParts[1].Trim().Replace(" ", ""), out int likeCount);
                        data.GiftCount = likeCount;
                        RoomManager.ins.AddHpByLikeReplyForPlayer(data.UserName);
                    }
                    break;

                case "关注":
                    data.MsgType = "follow";
                    data.UserName = content.Trim().Replace(" ", "");
                    break;

                case "进入":
                    data.MsgType = "member";
                    data.UserName = content.Trim().Replace(" ", "");
                    RoomManager.ins.AddPlayer(data.UserName);
                    break;

                case "状态":
                    data.MsgType = "status";
                    data.Content = content.Trim();
                    data.StatusCode = content.Contains("停止") ? 3 : 0; // 停止直播则状态码为3
                    break;

                case "统计":
                    data.MsgType = "stat";
                    string[] statParts = content.Split(new[] { ',' }, 2);
                    if (statParts.Length >= 2)
                    {
                        int.TryParse(statParts[0].Trim(), out int total);
                        int.TryParse(statParts[1].Trim(), out int totalUser);
                        data.Total = total;
                        data.TotalUser = totalUser;
                    }
                    break;

                default:
                    Debug.LogWarning($"未知消息类型：{msgType}");
                    data.MsgType = "unknown";
                    data.Content = rawMessage;
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"解析消息失败：{ex.Message} | 原始消息：{rawMessage}");
        }

        return data;
    }

    private static void SendListenerResponse(HttpListenerContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain";
        byte[] responseBytes = Encoding.UTF8.GetBytes(message);
        context.Response.ContentLength64 = responseBytes.Length;
        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
    }

    private void AppendLog(string log)
    {
        if (text == null)
        {
            Debug.Log(log);
            return;
        }

        const int maxLines = 50;

        var lines = text.text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        lines.Add($"{DateTime.Now:HH:mm:ss}: " + log);

        if (lines.Count > maxLines)
        {
            lines.RemoveRange(0, lines.Count - maxLines);
        }

        text.text = string.Join("\n", lines) + "\n";

        var scrollRect = text.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    private async Task ForwardMessageTo5001(string jsonMessage)
    {
        if (string.IsNullOrEmpty(jsonMessage))
        {
            Debug.LogWarning("转发失败：JSON消息为空");
            return;
        }

        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                Debug.Log(jsonMessage);

                //var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                //var response = await httpClient.PostAsync(ForwardTargetUrl, content);
                HttpResponseMessage response = null;
                try
                {
                    response = await httpClient.PostAsync(ForwardTargetUrl, content);
                    Debug.Log("转发成功");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"PostAsync执行异常：{ex.Message}\n{ex.StackTrace}");
                    return;
                }
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"转发成功（状态码：{response.StatusCode}）");
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        Debug.Log($"5001端口响应：{responseContent}");
                    }
                }
                else
                {
                    Debug.LogWarning($"转发失败（状态码：{response.StatusCode}）");
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogWarning($"错误详情：{errorContent}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.LogWarning($"转发到5001端口异常（网络错误）：{ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Debug.LogWarning($"转发到5001端口超时（5秒未响应）");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"转发到5001端口异常：{ex.Message}");
            Debug.LogWarning($"异常详情：{ex.StackTrace}");
        }
    }
}

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var cancellationTask = Task.Delay(-1, cancellationToken);
        var completedTask = await Task.WhenAny(task, cancellationTask);

        if (completedTask == cancellationTask)
            throw new OperationCanceledException(cancellationToken);

        return await task;
    }
}