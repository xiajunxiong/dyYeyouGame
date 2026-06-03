using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Events;

/// <summary>
/// 固定窗口宽度1080，仅可自由调整窗口高度（参考CSDN AspectRatioController改造）
/// 仅Windows打包生效，编辑器不挂载窗口钩子
/// </summary>
public class FixWidthOnlyHeightResize : MonoBehaviour
{
    public ResolutionChangedEvent resolutionChangedEvent;
    [Serializable]
    public class ResolutionChangedEvent : UnityEvent<int, int, bool> { }

    [Header("固定参数")]
    public int FixedClientWidth = 1080; //客户区固定宽度1080（不含窗口边框标题）
    public int InitClientHeight = 800;  //初始客户区高度800
    public int MinHeight = 200;         //窗口最小高度
    public int MaxHeight = 9999;        //窗口最大高度

    private int setWidth = -1;
    private int setHeight = -1;
    private bool wasFullscreenLastFrame;
    private bool started;
    private int pixelHeightOfCurrentScreen;
    private int pixelWidthOfCurrentScreen;
    private bool quitStarted;

    #region WinAPI常量与结构体
    private const int WM_SIZING = 0x214;
    private const int WMSZ_LEFT = 1;
    private const int WMSZ_RIGHT = 2;
    private const int WMSZ_TOP = 3;
    private const int WMSZ_BOTTOM = 6;
    private const int GWLP_WNDPROC = -4;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate wndProcDelegate;

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, ref RECT lpRect);
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);
    [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const string UNITY_WND_CLASSNAME = "UnityWndClass";
    private IntPtr unityHWnd;
    private IntPtr oldWndProcPtr;
    private IntPtr newWndProcPtr;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    #endregion

    void Start()
    {
        //初始化窗口尺寸
        Screen.SetResolution(FixedClientWidth, InitClientHeight, false);
#if !UNITY_EDITOR
        Application.wantsToQuit += ApplicationWantsToQuit;
        //查找Unity主窗口
        EnumThreadWindows(GetCurrentThreadId(), (hWnd, lParam) =>
        {
            var classText = new StringBuilder(UNITY_WND_CLASSNAME.Length + 1);
            GetClassName(hWnd, classText, classText.Capacity);
            if (classText.ToString() == UNITY_WND_CLASSNAME)
            {
                unityHWnd = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        wasFullscreenLastFrame = Screen.fullScreen;
        //挂载自定义窗口回调
        wndProcDelegate = wndProc;
        newWndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
        oldWndProcPtr = SetWindowLong(unityHWnd, GWLP_WNDPROC, newWndProcPtr);
        started = true;
#endif
    }

    //核心窗口消息拦截：拖拽时固定宽度，仅高度可变
    IntPtr wndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_SIZING)
        {
            RECT rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
            //计算窗口边框（标题+左右边框）
            RECT windowRect = new RECT();
            GetWindowRect(unityHWnd, ref windowRect);
            RECT clientRect = new RECT();
            GetClientRect(unityHWnd, ref clientRect);
            int borderWidth = windowRect.Right - windowRect.Left - (clientRect.Right - clientRect.Left);
            int borderHeight = windowRect.Bottom - windowRect.Top - (clientRect.Bottom - clientRect.Top);

            //客户区固定宽度=FixedClientWidth
            int targetW = FixedClientWidth;
            int dragType = wParam.ToInt32();

            switch (dragType)
            {
                //拖拽左右侧/左右下角：强制宽度固定1080
                case WMSZ_LEFT:
                    rc.Left = rc.Right - (targetW + borderWidth);
                    break;
                case WMSZ_RIGHT:
                    rc.Right = rc.Left + (targetW + borderWidth);
                    break;
                case WMSZ_LEFT + WMSZ_TOP:
                case WMSZ_LEFT + WMSZ_BOTTOM:
                    rc.Left = rc.Right - (targetW + borderWidth);
                    break;
                case WMSZ_RIGHT + WMSZ_TOP:
                case WMSZ_RIGHT + WMSZ_BOTTOM:
                    rc.Right = rc.Left + (targetW + borderWidth);
                    break;
                //拖拽上下：不限制宽度，仅限制高度范围
                case WMSZ_TOP:
                case WMSZ_BOTTOM:
                default:
                    int curClientH = rc.Bottom - rc.Top - borderHeight;
                    curClientH = Mathf.Clamp(curClientH, MinHeight, MaxHeight);
                    rc.Bottom = rc.Top + curClientH + borderHeight;
                    break;
            }

            //记录最终客户区尺寸
            setWidth = FixedClientWidth;
            int finalClientH = rc.Bottom - rc.Top - borderHeight;
            setHeight = Mathf.Clamp(finalClientH, MinHeight, MaxHeight);
            resolutionChangedEvent.Invoke(setWidth, setHeight, Screen.fullScreen);
            //回写窗口矩形
            Marshal.StructureToPtr(rc, lParam, true);
        }
        return CallWindowProc(oldWndProcPtr, hWnd, msg, wParam, lParam);
    }

    void Update()
    {
        //禁止全屏（按需可注释开启全屏）
        if (Screen.fullScreen)
            Screen.fullScreen = false;

        //防止Aero吸附、系统自动改分辨率
        if (!Screen.fullScreen && setWidth != -1 && setHeight != -1)
        {
            if (Screen.width != setWidth || Screen.height != setHeight)
                Screen.SetResolution(setWidth, setHeight, false);
        }
        wasFullscreenLastFrame = Screen.fullScreen;
    }

    //兼容32/64位SetWindowLong
    private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 4 ? SetWindowLong32(hWnd, nIndex, dwNewLong) : SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
    }

    //退出时还原原窗口过程，防止内存泄漏
    private bool ApplicationWantsToQuit()
    {
        if (!started) return false;
        if (!quitStarted)
        {
            StartCoroutine(DelayedQuit());
            return false;
        }
        return true;
    }
    IEnumerator DelayedQuit()
    {
        SetWindowLong(unityHWnd, GWLP_WNDPROC, oldWndProcPtr);
        yield return new WaitForEndOfFrame();
        quitStarted = true;
        Application.Quit();
    }
}