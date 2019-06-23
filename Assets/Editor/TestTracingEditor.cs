using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

[CustomEditor(typeof(TestTracing))]
public class TestTracingEditor : Editor
{

    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, true);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }
    public static string DeletePathLastComponent(string path)
    {
        if (path == null)
        {
            return path;
        }
        var idx = path.LastIndexOf('/');
        if (idx > 0)
            return path.Substring(0, idx + 1);
        else
            return path;
    }
    string root
    {
        get
        {
            var t = (target as TestTracing);
            return t.rootPath;
        }
    }
    string dllPath
    {
        get
        {
            var t = (target as TestTracing);
            return t.rootPath + t.dllPath;
        }
    }
    string projectName
    {
        get
        {
            var t = (target as TestTracing);
            return t.projectName;
        }
    }
    static readonly string fileDir = "Tracing";
    void Compile()
    {
        var p1 = AssetDatabase.GetAssetPath(target);
        p1 = DeletePathLastComponent(p1);
        DirectoryCopy(p1 + fileDir, root + "/" + fileDir,true);
        System.Diagnostics.Process x = new System.Diagnostics.Process();
        x.StartInfo.FileName = "/usr/local/share/dotnet/dotnet";
        x.StartInfo.Arguments = string.Format("build {0}{1} --configuration Release", root,projectName);
        x.StartInfo.StandardOutputEncoding = System.Text.Encoding.ASCII;
        x.StartInfo.RedirectStandardOutput = true;
        x.StartInfo.UseShellExecute = false;
        x.Start();     
        var outMsg = x.StandardOutput.ReadToEnd();
        x.WaitForExit();
        x.Close();
        byte[] buffer = Encoding.ASCII.GetBytes(outMsg);
        outMsg = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        Debug.Log(outMsg);
    }
    void Run()
    {
        var t = (target as TestTracing);
        System.Diagnostics.Process x = new System.Diagnostics.Process();
        x.StartInfo.FileName = "/usr/local/share/dotnet/dotnet";
        var outPath = AssetDatabase.GetAssetPath(target) + t.fileName;
        x.StartInfo.Arguments = string.Format("{0} {1} {2} {3} {4} {5}", dllPath, t.SampleCount, t.Width, t.Height, outPath,t.useBVH);
        var t1 = Time.realtimeSinceStartup;
        x.Start();
        x.WaitForExit();
        var dt = Time.realtimeSinceStartup - t1;
        Debug.LogFormat("sample: {0}  BVH {1} time:{2} ",t.SampleCount,t.useBVH,dt);
        AssetDatabase.ImportAsset(outPath);
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Compile&Run"))
        {
            Compile();
            Run();
        }

        if (GUILayout.Button("Run"))
        {
            Run();
        }
        if(GUILayout.Button("RunInUnity"))
        {
            var t = (target as TestTracing);
            var p1 = AssetDatabase.GetAssetPath(target);
            RT1.Program.Main(new string[] { t.SampleCount.ToString(), t.Width.ToString(), t.Height.ToString(), p1 + t.fileName });
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("Compile"))
        {
            Compile();
        }
    }
}
