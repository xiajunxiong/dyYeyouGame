using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFixAspect : MonoBehaviour
{
    public Vector2 baseRes = new Vector2(600, 800);
    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        FixAspectRatio();
    }

    void FixAspectRatio()
    {
        // 目标宽高比
        float targetAspect = baseRes.x / baseRes.y;

        // 当前屏幕宽高比
        float screenAspect = (float)Screen.width / Screen.height;

        // 缩放比例
        float scaleHeight = screenAspect / targetAspect;
        float scaleWidth = 1f / scaleHeight;

        Rect rect = _cam.rect;

        if (screenAspect > targetAspect)
        {
            // 屏幕太宽 → 左右黑边
            rect.width = scaleWidth;
            rect.x = (1f - scaleWidth) / 2f;
            rect.height = 1f;
            rect.y = 0f;
        }
        else
        {
            // 屏幕太高 → 上下黑边
            rect.height = scaleHeight;
            rect.y = (1f - scaleHeight) / 2f;
            rect.width = 1f;
            rect.x = 0f;
        }

        // 赋值一次就够了！
        _cam.rect = rect;
    }
}