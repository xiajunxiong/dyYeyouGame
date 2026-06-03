using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(使用对话框))]
public class 使用对话框Editor : Editor
{
    private SerializedProperty 使用类型Prop;
    private SerializedProperty 对话框类型Prop;
    private SerializedProperty 对话Prop;
    private SerializedProperty 对话数据合集Prop;
    private SerializedProperty 结束回调Prop;

    private void OnEnable()
    {
        使用类型Prop = serializedObject.FindProperty("使用类型");
        对话框类型Prop = serializedObject.FindProperty("对话框类型");
        对话Prop = serializedObject.FindProperty("对话");
        对话数据合集Prop = serializedObject.FindProperty("对话数据合集");
        结束回调Prop = serializedObject.FindProperty("结束回调");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(使用类型Prop);

        if (使用类型Prop.enumValueIndex == (int)使用对话框类型.开局激活)
        {
            EditorGUILayout.PropertyField(对话框类型Prop);

            if (对话框类型Prop.enumValueIndex == (int)对话框类型.单条)
            {
                EditorGUILayout.PropertyField(对话Prop);
            }
            else if (对话框类型Prop.enumValueIndex == (int)对话框类型.合集)
            {
                EditorGUILayout.PropertyField(对话数据合集Prop);
            }

            EditorGUILayout.PropertyField(结束回调Prop);
        }else if (使用类型Prop.enumValueIndex == (int)使用对话框类型.被调用)
        {
            EditorGUILayout.PropertyField(结束回调Prop);
        }

        serializedObject.ApplyModifiedProperties();
    }
}