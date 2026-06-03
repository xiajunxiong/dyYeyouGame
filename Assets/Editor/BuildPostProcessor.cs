using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        // 根据平台找到 Managed 目录
        string buildPath = report.summary.outputPath;
        string managedPath;

        if (report.summary.platform == BuildTarget.StandaloneWindows64)
        {
            managedPath = Path.Combine(buildPath, $"{Path.GetFileNameWithoutExtension(buildPath)}_Data", "Managed");
        }
        else if (report.summary.platform == BuildTarget.StandaloneOSX)
        {
            managedPath = Path.Combine(buildPath, "Contents", "Resources", "Data", "Managed");
        }
        else
        {
            // 其他平台按需扩展
            return;
        }

        if (Directory.Exists(managedPath))
        {
            Directory.Delete(managedPath, true);
            Debug.Log($"自动清理了 Managed 目录: {managedPath}");
        }
    }
}