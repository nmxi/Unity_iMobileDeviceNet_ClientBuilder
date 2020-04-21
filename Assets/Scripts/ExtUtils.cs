using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ExtUtils : MonoBehaviour
{
    public static string Command(string cmd)
    {
        var p = new Process();
        string output = "";

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                p.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
                p.StartInfo.Arguments = "/c start " + cmd;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();

                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();

                break;
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                p.StartInfo.FileName = "/bin/bash";
                p.StartInfo.Arguments = "-c \" " + "open " + cmd + " \"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();

                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();

                break;
            default:
                UnityEngine.Debug.LogError("Can not identify the OS on which the editor is running");
                break;
        }

        return output;
    }
}
