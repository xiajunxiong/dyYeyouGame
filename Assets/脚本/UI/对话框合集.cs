using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "对话框数据合集",
    menuName = "OldXia/对话框/对话框数据合集"
)]
public class 对话框数据合集 : ScriptableObject
{
    public List<对话框> 合集 = new List<对话框>();
}