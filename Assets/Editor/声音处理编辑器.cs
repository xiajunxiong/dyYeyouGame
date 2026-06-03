// 必须引用UnityEditor命名空间
using UnityEditor;
using UnityEngine;

// 明确指定要扩展的目标类
[CustomEditor(typeof(声音处理))]
// 正确继承Editor类（注意是UnityEditor.Editor，不是自定义的Editor）
public class 声音处理编辑器 : UnityEditor.Editor
{
    // 重写Inspector绘制方法
    public override void OnInspectorGUI()
    {
        // 1. 绘制独立的组件介绍面板（带边框+醒目提示）
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("Box");
        {
            GUILayout.Label("📢 组件介绍", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "该组件为全局音量组件，非空间音量组件！\n\n" +
                "核心功能：\n" +
                "1. 自动扩容音频组件，支持多音效同时播放\n" +
                "2. 音量值强制限制为0-1范围，超出自动修正\n" +
                "3. 冗余组件超时自动销毁，仅保留核心组件\n" +
                "4. 支持全局音量倍率统一调节所有音效音量",
                MessageType.Info
            );
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // 2. 绘制原有组件参数（解决“DrawDefaultInspector不存在”报错）
        base.DrawDefaultInspector();

        // 3. 手动清理按钮（解决“target不存在”报错）
        EditorGUILayout.Space(5);
        if (GUILayout.Button("🗑️ 手动清理冗余音频组件"))
        {
            // 正确转换target类型
            声音处理 音效组件 = (声音处理)this.target;
            音效组件.手动清理冗余组件();
            EditorUtility.DisplayDialog("提示", "冗余音频组件已清理完成！", "确定");
        }
    }
}