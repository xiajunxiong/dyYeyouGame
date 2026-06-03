using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleLoginMessages : MonoBehaviour
{
    public GameObject 验证UI模块;
    public Enemy enemy;
    public GameObject 排行榜;
    public Text 提示;
    // 处理被管理员踢下线消息
    public void HandleKickedByAdmin(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理被管理员踢下线消息: " + message);
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            提示.text = message;
            验证UI模块.SetActive(true);
            Time.timeScale = 0f;
        });
        // 被管理员踢下线，不需要重连
    }
    
    // 处理卡密到期踢下线消息
    public void HandleKickedByExpired(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理卡密到期踢下线消息: " + message);
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            提示.text = message;
            验证UI模块.SetActive(true);
            Time.timeScale = 0f;
        });

        // 卡密到期，不需要重连
    }
    
    // 处理卡密被封禁踢下线消息
    public void HandleKickedByBanned(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理卡密被封禁踢下线消息: " + message);
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            提示.text = message;
            验证UI模块.SetActive(true);
            Time.timeScale = 0f;
        });
        // 卡密被封禁，不需要重连
    }
    
    // 处理登录成功消息
    public void HandleLoginSuccess(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理登录成功消息: " + message);
        // 登录成功，不需要重连
    }
    
    // 处理登录失败消息
    public void HandleLoginFail(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理登录失败消息: " + message);
        // 登录失败，不需要重连
    }
    
    // 处理加入房间成功消息
    public void HandleRoomJoinSuccess(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理加入房间成功消息: " + message);

        PlatformValidator.ins.Platform();
        验证UI模块.SetActive(false);
        //排行榜.SetActive(true);
        Time.timeScale = 1f;
        GameTime.ins.StartDayTime();
    }
    
    // 处理心跳响应消息
    public void HandleHeartbeatAck(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理心跳响应消息: " + message);
        // 心跳响应，不需要重连
    }
    
    // 处理服务端错误消息
    public void HandleServerError(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理服务端错误消息: " + message);
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            提示.text = message;
            验证UI模块.SetActive(true);
            Time.timeScale = 0f;
        });
        // 服务端错误，可能需要重连
    }
    
    // 处理未知类型消息
    public void HandleUnknownMessage(string message)
    {
        Debug.Log("[HandleLoginMessages] 处理未知类型消息: " + message);
        // 未知消息，不需要重连
    }
    
    // 检查是否需要重连
    public bool ShouldReconnect(string messageType, string message)
    {
        Debug.Log("[HandleLoginMessages] 检查是否需要重连: " + messageType);
        switch (messageType)
        {
            case "kicked":
                // 被踢下线，不需要重连
                return false;
            case "login_fail":
                // 登录失败，不需要重连
                return false;
            case "error":
                // 服务端错误，可能需要重连
                return true;
            case "heartbeat_ack":
                // 心跳响应，不需要重连
                return false;
            case "login_success":
                // 登录成功，不需要重连
                return false;
            case "room_join_success":
                // 加入房间成功，不需要重连
                return false;
            default:
                // 未知消息，不需要重连
                return false;
        }
    }
}
