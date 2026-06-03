﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public class LoginModule : MonoBehaviour
{
    // UI组件
    public InputField deviceCodeInput;
    public InputField cdkInput;
    public InputField roomIdInput;
    public Dropdown platformDropdown;
    public Button submitButton;
    public Text statusText; // 新增状态显示文本

    // 配置参数
    public string baseUrl = "http://localhost:3000";
    public string wsUrl = "ws://localhost:3000";
    public string deviceId;
    public string deviceName;

    // 认证信息
    private readonly string _appKey = "client_app_key";
    private readonly string _appSecret = "client_app_secret123";

    // WebSocket客户端
    private WebSocketClient _wsClient;
    private bool _isWsConnected = false;
    
    // 消息处理器
    private HandleLoginMessages _messageHandler;
    private UnityMainThreadDispatcher _mainThreadDispatcher;
    private void Awake()
    {
        // 初始化设备信息
        deviceId = GetDeviceCode();
        deviceCodeInput.text = deviceId;
        deviceName = Environment.MachineName;

        string cdk = PlayerPrefs.GetString("loaded_cdk", "");
        if(!string.IsNullOrEmpty(cdk))
        {
            cdkInput.text = cdk;
        }

        // 注册按钮事件
        submitButton.onClick.AddListener(OnSubmit);

        // 初始化消息处理器
        _messageHandler = GetComponent<HandleLoginMessages>();
        if (_messageHandler == null)
        {
            _messageHandler = gameObject.AddComponent<HandleLoginMessages>();
        }

        // 初始化WebSocket客户端
        if (_wsClient == null)
        {
            _wsClient = new WebSocketClient(wsUrl, deviceId, _appKey, _appSecret);
        }
        _mainThreadDispatcher = UnityMainThreadDispatcher.GetInstance();
        if (statusText != null)
            statusText.text = "就绪";
    }

    // 提交校验逻辑
    public void OnSubmit()
    {
        string cdk = cdkInput.text.Trim();
        string roomId = roomIdInput.text.Trim();
        int platformIndex = platformDropdown.value;
        string platform = platformDropdown.options[platformIndex].text;

        // 输入验证
        if (string.IsNullOrEmpty(cdk))
        {
            ShowStatus("CDK不能为空", Color.red);
            return;
        }
        if (string.IsNullOrEmpty(roomId))
        {
            ShowStatus("房间号不能为空", Color.red);
            return;
        }
        PlayerPrefs.SetString("loaded_cdk", cdk);
        // 开始登录流程（使用协程）
        StartCoroutine(LoginCoroutine(cdk, roomId, platform));
    }
    private bool _isCdkActivatedSuccess = false;
    // 登录流程协程
    private IEnumerator LoginCoroutine(string cdk, string roomId, string platform)
    {
        // 显示加载状态
        SetLoading(true);
        ShowStatus("开始验证CDK...", Color.white);
        _isCdkActivatedSuccess = false;
        // 1. 激活CDK
        yield return StartCoroutine(ActivateCdkCoroutine(cdk));

        if (!_isCdkActivatedSuccess)
        {
            ShowStatus("CDK 不存在|已被使用", Color.red);
            SetLoading(false); // 隐藏加载状态
            yield break; // 终止协程，不再执行后续步骤
        }


        // 2. 验证设备绑定
        yield return StartCoroutine(VerifyDeviceCoroutine());

        // 3. 获取设备信息
        yield return StartCoroutine(GetDeviceInfoCoroutine());

        // 4. 获取CDK状态
        yield return StartCoroutine(GetCdkStatusCoroutine(cdk));

        // 5. 连接WebSocket
        ShowStatus("连接游戏服务器...", Color.white);
        yield return StartCoroutine(ConnectWebSocketCoroutine(cdk));

        // 6. 发送房间信息（自定义消息）
        if (_isWsConnected)
        {
            yield return StartCoroutine(SendRoomInfoCoroutine(cdk, roomId, platform));
            ShowStatus("初始化完成！房间id:" + roomId, Color.green);
        }

        // 隐藏加载状态
        SetLoading(false);
    }

    #region API请求协程封装
    // 激活CDK
    private IEnumerator ActivateCdkCoroutine(string cdk)
    {
        ShowStatus("激活CDK中...", Color.white);

        var apiClient = new ApiClient(baseUrl, _appKey, _appSecret);
        Task<ApiResponse<ActivateCdkResponse>> task = apiClient.ActivateCdk(cdk, deviceId, deviceName);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            
            ShowStatus("CDK激活失败：" + task.Exception.InnerException?.Message, Color.red);
            Debug.LogError(task.Exception);
            _isCdkActivatedSuccess = false;
            yield return null; // 确保在主线程更新UI
        }
        else
        {
            var response = task.Result;
            if (response.code == 200)
            {
                ShowStatus("CDK激活成功", Color.green);
                _isCdkActivatedSuccess = true;
            }
            else
            {
                ShowStatus("CDK激活失败：" + response.message, Color.red);
                _isCdkActivatedSuccess = false;
            }
        }
    }

    // 验证设备绑定
    private IEnumerator VerifyDeviceCoroutine()
    {
        ShowStatus("验证设备绑定状态...", Color.white);

        var apiClient = new ApiClient(baseUrl, _appKey, _appSecret);
        Task<ApiResponse<bool>> task = apiClient.VerifyDevice(deviceId);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            ShowStatus("设备验证失败：" + task.Exception.InnerException?.Message, Color.red);
            Debug.LogError(task.Exception);
        }
        else
        {
            var response = task.Result;
            if (response.code == 200)
            {
                ShowStatus("设备验证成功，绑定状态：" + (response.data ? "已绑定" : "未绑定"), Color.green);
            }
            else
            {
                ShowStatus("设备验证失败：" + response.message, Color.red);
            }
        }
    }

    // 获取设备信息
    private IEnumerator GetDeviceInfoCoroutine()
    {
        ShowStatus("获取设备信息...", Color.white);

        var apiClient = new ApiClient(baseUrl, _appKey, _appSecret);
        Task<ApiResponse<DeviceInfo>> task = apiClient.GetDeviceInfo(deviceId);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            ShowStatus("获取设备信息失败：" + task.Exception.InnerException?.Message, Color.red);
            Debug.LogError(task.Exception);
        }
        else
        {
            var response = task.Result;
            if (response.code == 200)
            {
                ShowStatus("设备信息获取成功：" + response.data.deviceName, Color.green);
                Debug.Log($"设备名称：{response.data.deviceName}, 绑定CDK：{response.data.cdk}");
            }
            else
            {
                ShowStatus("获取设备信息失败：" + response.message, Color.red);
            }
        }
    }

    // 获取CDK状态
    private IEnumerator GetCdkStatusCoroutine(string cdk)
    {
        ShowStatus("检查CDK状态...", Color.white);

        var apiClient = new ApiClient(baseUrl, _appKey, _appSecret);
        Task<ApiResponse<CdkStatus>> task = apiClient.GetCdkStatus(cdk);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            ShowStatus("检查CDK状态失败：" + task.Exception.InnerException?.Message, Color.red);
            Debug.LogError(task.Exception);
        }
        else
        {
            var response = task.Result;
            if (response.code == 200)
            {
                ShowStatus("CDK状态：" + response.data.status, Color.green);
            }
            else
            {
                ShowStatus("检查CDK状态失败：" + response.message, Color.red);
            }
        }
    }

    // 连接WebSocket
    // 连接WebSocket
    private IEnumerator ConnectWebSocketCoroutine(string cdk)
    {
        _isWsConnected = false;
        
        // 确保消息处理器已设置
        if (_wsClient != null && _messageHandler != null)
        {
            _wsClient.SetMessageHandler(_messageHandler);
        }
        
        Task connectTask = _wsClient.ConnectAsync(cdk);

        // 等待连接完成（最多等待10秒）
        float timeout = 10f;
        float timer = 0f;

        while (!connectTask.IsCompleted && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (timer >= timeout)
        {
            ShowStatus("WebSocket连接超时", Color.red);
            _isWsConnected = false; // 强制标记为未连接
        }
        else if (connectTask.Exception != null)
        {
            ShowStatus("WebSocket连接失败：" + connectTask.Exception.InnerException?.Message, Color.red);
            Debug.LogError(connectTask.Exception);
            _isWsConnected = false; // 强制标记为未连接
        }
        else
        {
            // 【核心修复】等待连接后，额外校验底层 IsConnected 状态
            yield return new WaitForSeconds(0.1f); // 短暂等待事件触发
            _isWsConnected = _wsClient.IsConnected;
            if (!_isWsConnected)
            {
                ShowStatus("WebSocket连接任务完成，但实际未连接", Color.red);
            }
            else
            {
                ShowStatus("WebSocket连接成功", Color.green);
            }
        }
    }

    // 发送房间信息
    private IEnumerator SendRoomInfoCoroutine(string cdk, string roomId, string platform)
    {
        ShowStatus("发送房间信息...", Color.white);

        var roomData = new
        {
            cdk = cdk,
            deviceId = deviceId,
            roomId = roomId,
            platform = platform,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        Task sendTask = _wsClient.SendMessageAsync("join_room", roomData);

        yield return new WaitUntil(() => sendTask.IsCompleted);

        if (sendTask.Exception != null)
        {
            ShowStatus("发送房间信息失败：" + sendTask.Exception.InnerException?.Message, Color.red);
            Debug.LogError(sendTask.Exception);
        }
        else
        {
            ShowStatus("已加入房间：" + roomId, Color.green);
        }
    }
    #endregion


    #region UI辅助方法
    private void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log(message);
    }

    private void SetLoading(bool isLoading)
    {
        submitButton.interactable = !isLoading;
    }
    #endregion

    #region 唯一设备码生成逻辑
    public string GetDeviceCode()
    {
        try
        {
            string deviceId = GetFixedDeviceIdentifier();

            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = PlayerPrefs.GetString("UniqueDeviceCode", GenerateFallbackFixedId());
            }

            string rawData = $"{deviceId}_{_appKey}_{_appSecret}";
            string encryptedDeviceId = EncryptWithMD5(rawData);

            return encryptedDeviceId;
        }
        catch (Exception ex)
        {
            Debug.LogError($"获取设备码失败：{ex.Message}");
            string fallbackId = PlayerPrefs.GetString("UniqueDeviceCode", GenerateFallbackFixedId());
            string fallbackData = $"{fallbackId}_{_appKey}_{_appSecret}";
            return EncryptWithMD5(fallbackData);
        }
    }

    private string GetFixedDeviceIdentifier()
    {
        string deviceId = string.Empty;

        if (!string.IsNullOrEmpty(SystemInfo.deviceUniqueIdentifier))
        {
            deviceId = SystemInfo.deviceUniqueIdentifier;
        }
        else
        {
            deviceId = $"{SystemInfo.graphicsDeviceName}_{SystemInfo.graphicsDeviceVendor}_{SystemInfo.processorType}_{SystemInfo.systemMemorySize}";
        }

        deviceId = deviceId.Replace(" ", "").Replace("/", "").Replace("\\", "");
        return deviceId;
    }

    private string GenerateFallbackFixedId()
    {
        string fallbackId = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString("UniqueDeviceCode", fallbackId);
        PlayerPrefs.Save();
        return fallbackId;
    }

    private string EncryptWithMD5(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using (MD5 md5Hash = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5Hash.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
    #endregion

    #region 生命周期管理
    private void OnDestroy()
    {
        if (_wsClient != null && _isWsConnected)
        {
            _ = CleanupWebSocketAsync();
        }
    }
    private async Task CleanupWebSocketAsync()
    {
        try
        {
            if (_wsClient != null)
            {
                await _wsClient.DisconnectAsync();
                _wsClient.Dispose();
                _wsClient = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"清理WebSocket失败: {ex.Message}");
        }
    }
    #endregion
}

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new Queue<Action>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }

    // 【你原来的方法】
    public static UnityMainThreadDispatcher GetInstance()
    {
        if (_instance == null)
        {
            GameObject obj = new GameObject("UnityMainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
        }
        return _instance;
    }

    // 【我加的】兼容 Instance 属性（解决你报错！）
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            return GetInstance();
        }
    }
}

// ========== 以下是需要放在同一个文件或单独文件中的辅助类 ==========
#region API客户端类
public class ApiClient
{
    private readonly string _baseUrl;
    private readonly string _appKey;
    private readonly string _appSecret;
    private readonly HttpClient _httpClient;

    public ApiClient(string baseUrl, string appKey, string appSecret)
    {
        _baseUrl = baseUrl;
        _appKey = appKey;
        _appSecret = appSecret;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    private string GenerateSignature(Dictionary<string, string> parameters)
    {
        var sortedParams = new SortedDictionary<string, string>(parameters);
        var sb = new StringBuilder();

        foreach (var param in sortedParams)
        {
            if (!string.IsNullOrEmpty(param.Value))
            {
                sb.Append($"{param.Key}={param.Value}&");
            }
        }

        sb.Append($"appSecret={_appSecret}");

        using (var md5 = MD5.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = md5.ComputeHash(bytes);
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return signature;
        }
    }

    private async Task<T> SendRequest<T>(string endpoint, Dictionary<string, string> parameters, HttpMethod method = null)
    {
        if (method == null)
        {
            method = HttpMethod.Post;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString().Substring(0, 8);

        var requestParams = new Dictionary<string, string>(parameters)
        {
            { "appKey", _appKey },
            { "timestamp", timestamp },
            { "nonce", nonce }
        };

        var signature = GenerateSignature(requestParams);
        requestParams["signature"] = signature;

        var content = new FormUrlEncodedContent(requestParams);
        var request = new HttpRequestMessage(method, $"{_baseUrl}/{endpoint}")
        {
            Content = content
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(responseContent);
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"API请求错误: {ex.Message}");
            throw;
        }
    }

    // CDK激活
    public async Task<ApiResponse<ActivateCdkResponse>> ActivateCdk(string cdk, string deviceId, string deviceName)
    {
        var parameters = new Dictionary<string, string>
        {
            { "cdk", cdk },
            { "deviceId", deviceId },
            { "deviceName", deviceName }
        };

        return await SendRequest<ApiResponse<ActivateCdkResponse>>("api/activate", parameters);
    }

    // 验证设备绑定
    public async Task<ApiResponse<bool>> VerifyDevice(string deviceId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "deviceId", deviceId }
        };

        return await SendRequest<ApiResponse<bool>>("api/verify", parameters);
    }

    // 获取设备绑定信息
    public async Task<ApiResponse<DeviceInfo>> GetDeviceInfo(string deviceId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "deviceId", deviceId }
        };

        return await SendRequest<ApiResponse<DeviceInfo>>("api/device/info", parameters);
    }

    // 解除设备绑定
    public async Task<ApiResponse<bool>> UnbindDevice(string deviceId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "deviceId", deviceId }
        };

        return await SendRequest<ApiResponse<bool>>("api/device/unbind", parameters);
    }

    // 批量生成CDK
    public async Task<ApiResponse<List<string>>> GenerateCdk(int count, int validityDays)
    {
        var parameters = new Dictionary<string, string>
        {
            { "count", count.ToString() },
            { "validityDays", validityDays.ToString() }
        };

        return await SendRequest<ApiResponse<List<string>>>("api/cdk/generate", parameters);
    }

    // 获取CDK状态
    public async Task<ApiResponse<CdkStatus>> GetCdkStatus(string cdk)
    {
        var parameters = new Dictionary<string, string>
        {
            { "cdk", cdk }
        };

        return await SendRequest<ApiResponse<CdkStatus>>("api/cdk/status", parameters);
    }
}

// API响应类
public class ApiResponse<T>
{
    public int code { get; set; }
    public string message { get; set; }
    public T data { get; set; }
}

// 设备信息类
public class DeviceInfo
{
    public string deviceId { get; set; }
    public string deviceName { get; set; }
    public string cdk { get; set; }
    public DateTime bindTime { get; set; }
    public bool isBound { get; set; }
}

// CDK状态类
public class CdkStatus
{
    public string cdk { get; set; }
    public string status { get; set; }
    public string deviceId { get; set; }
    public DateTime? bindTime { get; set; }
    public DateTime expireTime { get; set; }
}

// 激活CDK响应类
public class ActivateCdkResponse
{
    public int cdk_id { get; set; }
    public int binding_id { get; set; }
    public string device_id { get; set; }
    public string activated_at { get; set; }
}
#endregion

#region WebSocket客户端类
public class WebSocketClient : IDisposable
{
    private ClientWebSocket _webSocket;
    private readonly string _wsUrl;
    private readonly string _deviceId;
    private readonly string _appKey;
    private readonly string _appSecret;
    private CancellationTokenSource _cts;
    private Task _receiveTask;
    private Task _heartbeatTask;
    private bool _isConnected;
    private bool _isDisposed;
    private string _cdk;
    private int _reconnectAttempts;
    private const int MaxReconnectAttempts = 5;
    private const int ReconnectDelayMs = 5000;
    
    // 消息处理器
    private HandleLoginMessages _messageHandler;

    public event EventHandler<WebSocketMessageEventArgs> OnMessageReceived;
    public event EventHandler<EventArgs> OnConnected;
    public event EventHandler<EventArgs> OnDisconnected;
    public event EventHandler<ExceptionEventArgs> OnError;

    public bool IsConnected
    {
        get
        {
            // 【核心修复】优先判断底层 WebSocket 状态，再判断标记
            if (_webSocket == null) return false;
            return _webSocket.State == WebSocketState.Open && _isConnected;
        }
    }
    
    // 设置消息处理器
    public void SetMessageHandler(HandleLoginMessages handler)
    {
        _messageHandler = handler;
        Debug.Log("WebSocketClient消息处理器已设置");
    }

    public WebSocketClient(string wsUrl, string deviceId, string appKey, string appSecret)
    {
        _wsUrl = wsUrl;
        _deviceId = deviceId;
        _appKey = appKey;
        _appSecret = appSecret;
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        
        // 消息处理器将通过外部注入
        _messageHandler = null;
    }
    

    public async Task ConnectAsync(string cdk = null)
    {
        _cdk = cdk;
        // 重置Disposed标志，允许重新连接
        _isDisposed = false;
        await InternalConnectAsync();
    }

    private async Task InternalConnectAsync()
    {
        try
        {
            _reconnectAttempts = 0;
            // 确保重置Disposed标志
            _isDisposed = false;

            // 清理旧的WebSocket实例
            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "重新连接", CancellationToken.None);
                    }
                }
                catch { }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }

            // 创建新的WebSocket实例
            _webSocket = new ClientWebSocket();

            // 创建新的CancellationTokenSource实例
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = GenerateWebSocketSignature(timestamp);

            var connectUrl = $"{_wsUrl}?deviceId={_deviceId}&appKey={_appKey}&timestamp={timestamp}&signature={signature}";

            Debug.Log($"正在连接WebSocket服务器: {connectUrl}");
            await _webSocket.ConnectAsync(new Uri(connectUrl), _cts.Token);
            _isConnected = true;

            Debug.Log("WebSocket连接成功，启动消息接收和心跳任务");
            // 启动消息接收任务
            _receiveTask = Task.Run(async () => {
                Debug.Log("消息接收任务已启动");
                await ReceiveMessagesAsync();
                Debug.Log("消息接收任务已结束");
            }, _cts.Token);
            // 启动心跳任务
            _heartbeatTask = Task.Run(async () => {
                Debug.Log("心跳任务已启动");
                await SendHeartbeatAsync();
                Debug.Log("心跳任务已结束");
            }, _cts.Token);

            if (!string.IsNullOrEmpty(_cdk))
            {
                Debug.Log($"发送登录消息: CDK={_cdk}, DeviceId={_deviceId}");
                await SendLoginMessage(_cdk);
            }

            OnConnected?.Invoke(this, EventArgs.Empty);
            Debug.Log("WebSocket连接成功，事件已触发");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new ExceptionEventArgs(ex));
            Debug.LogError($"WebSocket连接失败: {ex.Message}");
            Debug.LogError($"错误堆栈: {ex.StackTrace}");

            if (!_isDisposed && _reconnectAttempts < MaxReconnectAttempts)
            {
                await AttemptReconnect();
            }
            else
            {
                throw;
            }
        }
    }

    private async Task AttemptReconnect()
    {
        // 退出时直接终止重连
        if (_isDisposed) return;

        _reconnectAttempts++;
        Debug.Log($"尝试重连 ({_reconnectAttempts}/{MaxReconnectAttempts})...");

        await Task.Delay(ReconnectDelayMs);

        if (!_isDisposed)
        {
            await InternalConnectAsync();
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            Debug.Log("开始断开WebSocket连接");
            _isDisposed = true;
            _isConnected = false;

            // 优先取消所有异步任务
            _cts.Cancel();
            Debug.Log("已取消CancellationTokenSource");

            // 等待心跳/接收任务结束（超时保护，避免无限等待）
            if (_heartbeatTask != null && !_heartbeatTask.IsCompleted)
            {
                Debug.Log("等待心跳任务结束");
                await Task.WhenAny(_heartbeatTask, Task.Delay(3000));
                Debug.Log("心跳任务已结束或超时");
            }
            if (_receiveTask != null && !_receiveTask.IsCompleted)
            {
                Debug.Log("等待接收任务结束");
                await Task.WhenAny(_receiveTask, Task.Delay(3000));
                Debug.Log("接收任务已结束或超时");
            }

            // 安全关闭WebSocket（判断状态+非阻塞）
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                try
                {
                    Debug.Log("关闭WebSocket连接");
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "客户端主动断开",
                        CancellationToken.None
                    );
                    Debug.Log("WebSocket连接已关闭");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"关闭WebSocket连接失败: {ex.Message}");
                    // 忽略关闭异常（如网络已断开）
                }
            }

            // 强制清理WebSocket实例
            if (_webSocket != null)
            {
                Debug.Log("清理WebSocket实例");
                _webSocket.Dispose();
                _webSocket = null;
                Debug.Log("WebSocket实例已清理");
            }

            // 清理任务引用
            _heartbeatTask = null;
            _receiveTask = null;
            Debug.Log("任务引用已清理");

            OnDisconnected?.Invoke(this, EventArgs.Empty);
            Debug.Log("WebSocket连接已断开");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new ExceptionEventArgs(ex));
            Debug.LogError($"WebSocket断开失败: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(string type, object data)
    {
        if (!_isConnected || _webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket未连接");
        }

        try
        {
            var message = new
            {
                type = type,
                data = data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var jsonMessage = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(jsonMessage);

            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new ExceptionEventArgs(ex));
            Debug.LogError($"发送消息失败: {ex.Message}");

            // 【核心修复】只在网络连接真正断开时才标记为未连接
            // 发送消息失败可能是因为网络延迟，不应该立即断开连接
            if (!_isDisposed && _isConnected && (_webSocket == null || _webSocket.State != WebSocketState.Open))
            {
                _isConnected = false;
                OnDisconnected?.Invoke(this, EventArgs.Empty);
                // 被踢下线后不再尝试重新连接
                if (!_isDisposed && _reconnectAttempts < MaxReconnectAttempts)
                {
                    await AttemptReconnect();
                }
            }

            throw;
        }
    }
    // 异步处理socket消息
    private async Task ReceiveMessagesAsync()
    {
        Debug.Log("ReceiveMessagesAsync方法开始执行");
        var buffer = new byte[4096];
        // 定义主线程调度器实例（提前缓存，避免多次获取失效）
        UnityMainThreadDispatcher mainThreadDispatcher = null;
        try
        {
            Debug.Log("初始化主线程调度器");
            // 尝试获取调度器实例，但即使失败也继续执行
            try
            {
                mainThreadDispatcher = UnityMainThreadDispatcher.Instance;
                Debug.Log("主线程调度器初始化成功");
            }
            catch (Exception ex)
            {
                // 调度器初始化失败，只记录日志，不影响WebSocket连接
                Debug.LogError($"[WebSocket 警告] 主线程调度器初始化失败：{ex.Message}");
                Console.WriteLine($"[WebSocket 警告] 主线程调度器初始化失败：{ex.Message}");
            }

            Debug.Log("进入消息接收循环");
            while (!_cts.Token.IsCancellationRequested && !_isDisposed)
            {
                // 1. 调试日志：确保循环在运行（增加线程ID，方便定位）
                Debug.Log($"[WebSocket 接收线程 {Thread.CurrentThread.ManagedThreadId}] 正在异步接收消息！");
                SafeLogToMainThread(mainThreadDispatcher,
                    $"[WebSocket 接收线程 {Thread.CurrentThread.ManagedThreadId}] 正在异步接收消息！",
                    LogType.Log);

                if (_webSocket == null)
                {
                    Debug.LogError("WebSocket实例为null，终止接收循环");
                    var stateMsg = "WebSocket实例为null，终止接收循环";
                    SafeLogToMainThread(mainThreadDispatcher, stateMsg, LogType.Error);

                    // 线程安全修改连接状态（加锁）
                    lock (this)
                    {
                        _isConnected = false;
                    }
                    // 触发断开事件（确保在主线程）
                    SafeInvokeDisconnected(mainThreadDispatcher);
                    break;
                }

                var webSocketState = _webSocket.State;
                Debug.Log($"WebSocket状态：{webSocketState}");
                if (webSocketState != WebSocketState.Open)
                {
                    // 2. 状态异常：确保日志和事件都执行
                    var stateMsg = $"WebSocket状态异常：{webSocketState}，终止接收循环";
                    Debug.LogError(stateMsg);
                    SafeLogToMainThread(mainThreadDispatcher, stateMsg, LogType.Error);

                    // 线程安全修改连接状态（加锁）
                    lock (this)
                    {
                        _isConnected = false;
                    }
                    // 触发断开事件（确保在主线程）
                    SafeInvokeDisconnected(mainThreadDispatcher);
                    break;
                }

                Task<WebSocketReceiveResult> receiveTask = _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                Task timeoutTask = Task.Delay(70000, _cts.Token);
                Debug.Log("等待消息接收或超时...");
                Task completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // 3. 接收超时：兜底日志+事件
                    Debug.LogError("WebSocket接收超时（70秒），判定连接已断开");
                    SafeLogToMainThread(mainThreadDispatcher,
                        "WebSocket接收超时（70秒），判定连接已断开",
                        LogType.Error);

                    lock (this)
                    {
                        _isConnected = false;
                    }
                    SafeInvokeDisconnected(mainThreadDispatcher);

                    // 重连逻辑（确保异步执行不阻塞）
                    if (!_isDisposed && _reconnectAttempts < MaxReconnectAttempts)
                    {
                        // 使用Fire-and-Forget模式，避免重连阻塞接收循环
                        Debug.Log("尝试重新连接...");
                        _ = AttemptReconnect();
                    }
                    break;
                }

                Debug.Log("消息接收完成，处理结果...");
                var result = receiveTask.Result;
                Debug.Log($"消息接收结果：MessageType={result.MessageType}, Count={result.Count}, CloseStatus={result.CloseStatus}");
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var closeMsg = $"服务端主动关闭WebSocket连接！关闭码：{result.CloseStatus}，原因：{result.CloseStatusDescription}";
                    Debug.LogError(closeMsg);
                    SafeLogToMainThread(mainThreadDispatcher, closeMsg, LogType.Error);



                    try
                    {
                        Debug.Log("关闭WebSocket连接...");
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                            new CancellationTokenSource(1000).Token);
                        Debug.Log("WebSocket连接已关闭");
                    }
                    catch (Exception closeEx)
                    {
                        Debug.LogError($"关闭WebSocket失败：{closeEx.Message}");
                        SafeLogToMainThread(mainThreadDispatcher,
                            $"关闭WebSocket失败：{closeEx.Message}",
                            LogType.Error);
                    }

                    lock (this)
                    {
                        _isConnected = false;
                    }
                    SafeInvokeDisconnected(mainThreadDispatcher);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log($"客户端实际收到的原始消息：{message}");
                    SafeLogToMainThread(mainThreadDispatcher,
                        $"客户端实际收到的原始消息：{message}",
                        LogType.Log);
                    Debug.Log("处理收到的消息...");
                    
                    // 直接处理消息，不再调用ProcessMessage
                    try
                    {
                        // 消息修复逻辑
                        string fixedMessage = message.Trim();
                        fixedMessage = fixedMessage.Replace("'", "\"");
                        Debug.Log($"修复后消息: {fixedMessage}");
                        SafeLogToMainThread(mainThreadDispatcher,
                            $"修复后消息: {fixedMessage}",
                            LogType.Log);
                        
                        // 解析消息
                        var wsMessage = JsonConvert.DeserializeObject<WebSocketMessage>(fixedMessage);
                        Debug.Log($"解析出消息类型: {wsMessage.type}");
                        SafeLogToMainThread(mainThreadDispatcher,
                            $"解析出消息类型: {wsMessage.type}",
                            LogType.Log);
                        
                        // 处理消息
                        Console.WriteLine("[WebSocket] 准备处理消息，调用SafeLogToMainThread");
                        SafeLogToMainThread(mainThreadDispatcher, () => {
                            try
                            {
                                Console.WriteLine("[WebSocket] 委托开始执行");
                                if (wsMessage == null)
                                {
                                    Debug.LogError("WebSocket消息对象为null");
                                    Console.WriteLine("[WebSocket 错误] WebSocket消息对象为null");
                                    return;
                                }
                                
                                if (string.IsNullOrEmpty(wsMessage.type))
                                {
                                    Debug.LogError("WebSocket消息类型为null或空");
                                    Console.WriteLine("[WebSocket 错误] WebSocket消息类型为null或空");
                                    return;
                                }

                                switch (wsMessage.type)
                                {
                                    case "login_success":
                                        if (_messageHandler != null)
                                        {
                                            _messageHandler.HandleLoginSuccess(JsonConvert.SerializeObject(wsMessage.data));
                                        }
                                        break;
                                    case "login_fail":
                                        if (_messageHandler != null)
                                        {
                                            _messageHandler.HandleLoginFail(JsonConvert.SerializeObject(wsMessage.data));
                                        }
                                        // 登录失败，不需要重连
                                        break;
                                    case "room_join_success":
                                        if (_messageHandler != null)
                                        {
                                            mainThreadDispatcher.Enqueue(() =>
                                            {
                                                _messageHandler.HandleRoomJoinSuccess(JsonConvert.SerializeObject(wsMessage.data));
                                            });
                                        }
                                        break;
                                    case "kicked":
                                        string kickMessage = JsonConvert.SerializeObject(wsMessage.data);
                                        if (_messageHandler != null)
                                        {
                                            if (kickMessage.Contains("管理员"))
                                            {
                                                _messageHandler.HandleKickedByAdmin(kickMessage);
                                            }
                                            else if (kickMessage.Contains("过期"))
                                            {
                                                _messageHandler.HandleKickedByExpired(kickMessage);
                                            }
                                            else if (kickMessage.Contains("封禁"))
                                            {
                                                _messageHandler.HandleKickedByBanned(kickMessage);
                                            }
                                            else
                                            {
                                                _messageHandler.HandleKickedByAdmin(kickMessage);
                                            }
                                        }
                                        _ = DisconnectAsync();
                                        break;
                                    case "heartbeat_ack":
                                        // 心跳响应，无需特殊处理，仅用于调试
                                        Debug.Log("收到心跳响应：" + JsonConvert.SerializeObject(wsMessage.data));
                                        Console.WriteLine("[WebSocket] 收到心跳响应：" + JsonConvert.SerializeObject(wsMessage.data));
                                        Debug.Log("[WebSocketClient] 调用HandleHeartbeatAck");
                                        if (_messageHandler != null)
                                        {
                                            _messageHandler.HandleHeartbeatAck(JsonConvert.SerializeObject(wsMessage.data));
                                        }
                                        break;
                                    case "error":
                                        Debug.Log("服务器错误：" + wsMessage.data);
                                        Console.WriteLine("[WebSocket] 服务器错误：" + wsMessage.data);
                                        Debug.Log("[WebSocketClient] 调用HandleServerError");
                                        if (_messageHandler != null)
                                        {
                                            _messageHandler.HandleServerError(JsonConvert.SerializeObject(wsMessage.data));
                                            // 检查是否需要重连
                                            if (_messageHandler.ShouldReconnect("error", JsonConvert.SerializeObject(wsMessage.data)))
                                            {
                                                Debug.Log("[WebSocketClient] 服务端错误，需要重连");
                                                Console.WriteLine("[WebSocket] 服务端错误，需要重连");
                                                // 这里可以添加重连逻辑
                                            }
                                        }
                                        break;
                                    default:
                                        Debug.Log("收到未知类型消息：" + wsMessage.type);
                                        Console.WriteLine("[WebSocket] 收到未知类型消息：" + wsMessage.type);
                                        Debug.Log("[WebSocketClient] 调用HandleUnknownMessage");
                                        if (_messageHandler != null)
                                        {
                                            _messageHandler.HandleUnknownMessage(JsonConvert.SerializeObject(wsMessage));
                                        }
                                        break;
                                }
                                Console.WriteLine("[WebSocket] 委托执行完成");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("处理WebSocket消息失败：" + ex.Message);
                                Console.WriteLine("[WebSocket 错误] 处理WebSocket消息失败：" + ex.Message);
                                Console.WriteLine("[WebSocket 错误] 堆栈跟踪：" + ex.StackTrace);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"解析消息失败: {ex.Message} | 原始消息: {message}");
                        SafeLogToMainThread(mainThreadDispatcher,
                            $"解析消息失败: {ex.Message} | 原始消息: {message}",
                            LogType.Error);
                        OnError?.Invoke(this, new ExceptionEventArgs(ex));
                    }
                }
                else
                {
                    Debug.Log($"收到非文本消息：{result.MessageType}");
                }
            }
            Debug.Log("消息接收循环结束");
        }
        catch (OperationCanceledException)
        {
            // 6. 手动取消：友好日志
            Debug.Log("接收循环被手动取消（比如被踢下线）");
            SafeLogToMainThread(mainThreadDispatcher,
                "接收循环被手动取消（比如被踢下线）",
                LogType.Log);
        }
        catch (Exception ex)
        {
            // 7. 核心修复：异常捕获兜底（关键！）
            // 第一步：先打印控制台兜底日志（不依赖Unity主线程）
            Debug.LogError($"[WebSocket 致命异常 线程{Thread.CurrentThread.ManagedThreadId}] 接收消息异常：{ex.Message}\n堆栈：{ex.StackTrace}");
            Console.WriteLine($"[WebSocket 致命异常 线程{Thread.CurrentThread.ManagedThreadId}] 接收消息异常：{ex.Message}\n堆栈：{ex.StackTrace}");

            // 第二步：尝试主线程打印Unity日志
            SafeLogToMainThread(mainThreadDispatcher,
                $"接收消息异常：{ex.Message}\n堆栈：{ex.StackTrace}",
                LogType.Error);

            // 第三步：线程安全修改状态
            lock (this)
            {
                _isConnected = false;
            }

            // 第四步：触发错误事件（确保主线程）
            SafeInvokeError(mainThreadDispatcher, ex);

            // 第五步：触发断开事件
            SafeInvokeDisconnected(mainThreadDispatcher);
        }
        finally
        {
            Debug.Log("ReceiveMessagesAsync方法执行完毕");
        }
    }

    private void SafeLogToMainThread(UnityMainThreadDispatcher dispatcher, string message, LogType logType = LogType.Log)
    {
        try
        {
            if (dispatcher != null && !_isDisposed)
            {
                dispatcher.Enqueue(() =>
                {
                    switch (logType)
                    {
                        case LogType.Error:
                            Debug.LogError(message);
                            break;
                        case LogType.Warning:
                            Debug.LogWarning(message);
                            break;
                        default:
                            Debug.Log(message);
                            break;
                    }
                });
            }
            else
            {
                // 兜底：直接输出控制台（所有环境都能看到）
                Console.WriteLine($"[WebSocket 日志 调度器失效] {message}");
            }
        }
        catch (Exception logEx)
        {
            // 终极兜底：避免日志逻辑本身抛异常
            Console.WriteLine($"[WebSocket 日志异常] 打印日志失败：{logEx.Message} | 原消息：{message}");
        }
    }

    private void SafeLogToMainThread(UnityMainThreadDispatcher dispatcher, Action action)
    {
        try
        {
            if (dispatcher != null && !_isDisposed)
            {
                Console.WriteLine("[WebSocket] 主线程调度器有效，将委托加入队列");
                dispatcher.Enqueue(action);
            }
            else
            {
                Console.WriteLine("[WebSocket 警告] 主线程调度器失效，直接执行委托");
                // 主线程调度器失效，直接执行委托
                try
                {
                    action.Invoke();
                    Console.WriteLine("[WebSocket] 直接执行委托成功");
                }
                catch (Exception invokeEx)
                {
                    Console.WriteLine($"[WebSocket 错误] 直接执行委托失败：{invokeEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocket 错误] 执行主线程委托失败：{ex.Message}");
            // 再次尝试直接执行委托
            try
            {
                action.Invoke();
                Console.WriteLine("[WebSocket] 再次尝试直接执行委托成功");
            }
            catch (Exception invokeEx)
            {
                Console.WriteLine($"[WebSocket 错误] 再次尝试直接执行委托失败：{invokeEx.Message}");
            }
        }
    }
    private void SafeInvokeError(UnityMainThreadDispatcher dispatcher, Exception ex)
    {
        try
        {
            if (OnError != null)
            {
                if (dispatcher != null && !_isDisposed)
                {
                    dispatcher.Enqueue(() => OnError?.Invoke(this, new ExceptionEventArgs(ex)));
                }
                else
                {
                    // 降级：直接触发（可能在子线程，但保证事件执行）
                    OnError?.Invoke(this, new ExceptionEventArgs(ex));
                }
            }
        }
        catch (Exception invokeEx)
        {
            Console.WriteLine($"[WebSocket 错误] 触发OnError事件失败：{invokeEx.Message}");
        }
    }

    private void SafeInvokeDisconnected(UnityMainThreadDispatcher dispatcher)
    {
        try
        {
            if (OnDisconnected != null)
            {
                if (dispatcher != null && !_isDisposed)
                {
                    dispatcher.Enqueue(() => OnDisconnected?.Invoke(this, EventArgs.Empty));
                }
                else
                {
                    // 降级：直接触发
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (Exception invokeEx)
        {
            Console.WriteLine($"[WebSocket 错误] 触发OnDisconnected事件失败：{invokeEx.Message}");
        }
    }

    private async Task SendLoginMessage(string cdk)
    {
        try
        {
            await SendMessageAsync("login", new { cdk = cdk, deviceId = _deviceId });
            Debug.Log($"发送登录消息: CDK={cdk}, DeviceId={_deviceId}");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new ExceptionEventArgs(ex));
            Debug.LogError($"发送登录消息失败: {ex.Message}");
        }
    }

    private async Task SendHeartbeatAsync() {
        try {
            Debug.Log("开始发送心跳消息");
            while (!_cts.Token.IsCancellationRequested && !_isDisposed) {
                // 更严格的连接状态检查
                if (_isConnected && _webSocket != null && _webSocket.State == WebSocketState.Open) {
                    try {
                        Debug.Log($"发送心跳消息: CDK={_cdk}, DeviceId={_deviceId}");
                        await SendMessageAsync("heartbeat", new { cdk = _cdk, deviceId = _deviceId });
                        Debug.Log("心跳消息发送成功");
                    } catch (Exception ex) {
                        // 在主线程中输出错误信息
                        try {
                            Debug.LogError($"发送心跳失败: {ex.Message}");
                        } catch (Exception dispatcherEx) {
                            // 调度器调用失败，只记录日志，不影响连接状态
                            Console.WriteLine($"[WebSocket 警告] 主线程调度器调用失败：{dispatcherEx.Message}");
                        }
                        if (_isConnected && (_webSocket == null || _webSocket.State != WebSocketState.Open)) {
                            _isConnected = false;
                            OnDisconnected?.Invoke(this, EventArgs.Empty);
                        }
                    }
                } else {
                    Debug.Log($"WebSocket未连接，跳过心跳发送: _isConnected={_isConnected}, State={_webSocket?.State}");
                }
                // 检查是否被取消或释放，避免在延迟后继续执行
                if (_cts.Token.IsCancellationRequested || _isDisposed) {
                    break;
                }
                Debug.Log("等待30秒后发送下一次心跳");
                await Task.Delay(30000, _cts.Token);
            }
            Debug.Log("心跳任务已停止");
        } catch (OperationCanceledException) {
            // 正常取消
            Debug.Log("心跳任务已取消");
        } catch (Exception ex) {
            // 在主线程中输出错误信息
            try {
                UnityMainThreadDispatcher.Instance.Enqueue(() => {
                    Debug.LogError($"心跳任务失败: {ex.Message}");
                });
            } catch (Exception dispatcherEx) {
                // 调度器调用失败，只记录日志，不影响连接状态
                Console.WriteLine($"[WebSocket 警告] 主线程调度器调用失败：{dispatcherEx.Message}");
            }
            OnError?.Invoke(this, new ExceptionEventArgs(ex));
        }
    }


    private string GenerateWebSocketSignature(string timestamp)
    {
        var sb = new StringBuilder();
        sb.Append($"deviceId={_deviceId}&appKey={_appKey}&timestamp={timestamp}&appSecret={_appSecret}");

        using (var md5 = MD5.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = md5.ComputeHash(bytes);
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return signature;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _cts.Cancel();

            // 修复4：Dispose时不阻塞等待CloseAsync
            try
            {
                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        // 异步关闭，不等待结果
                        _ = _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "客户端关闭",
                            CancellationToken.None
                        );
                    }
                    _webSocket.Dispose();
                }
            }
            catch { }

            _cts?.Dispose();
            _isConnected = false;
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }

}

// WebSocket消息类
public class WebSocketMessage
{
    public string type { get; set; }
    public object data { get; set; }
    public long timestamp { get; set; }
}

// WebSocket事件参数类
public class WebSocketMessageEventArgs : EventArgs
{
    public WebSocketMessage Message { get; }

    public WebSocketMessageEventArgs(WebSocketMessage message)
    {
        Message = message;
    }
}

// 异常事件参数类
public class ExceptionEventArgs : EventArgs
{
    public Exception Exception { get; }

    public ExceptionEventArgs(Exception exception)
    {
        Exception = exception;
    }
}
#endregion