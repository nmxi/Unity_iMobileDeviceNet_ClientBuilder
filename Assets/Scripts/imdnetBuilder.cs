using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;

public class imdnetBuilder : MonoBehaviour
{
    private static string STREAMINGASSETS;

    private static string IMDN_URL;
    private static string ZIP_PATH;
    private static string UNZIP_PATH;
    private static string MSBUILD_ZIP_PATH;
    private static string MSBUILD_PATH;

    private static string RESTORE_BAT_PATH;
    private static string BUILD_BAT_PATH;

    private static string MSBUILD_EXE_PATH;
    private static string IMDN_SLN_PATH;

    private static string EXPORTED_DLL_PATH;
    private static string FINAL_DLL_EXPORT_PATH;

    private Progress<ZipProgress> progress;

    private float progressPer;
    private int unzipNum;
    private int lastReportPer;

    [SerializeField] private bool isStartProcessOnAwake;

    [Space(15f), SerializeField] private RectTransform progressBarRT;
    [SerializeField] private TextMeshProUGUI progressParText;

    private void Awake()
    {
        STREAMINGASSETS = Application.dataPath + "/StreamingAssets";

        IMDN_URL = "https://github.com/libimobiledevice-win32/imobiledevice-net/archive/v1.2.186.zip";
        ZIP_PATH = STREAMINGASSETS + "/v1.2.186.zip";
        UNZIP_PATH = STREAMINGASSETS + "/imdn";
        MSBUILD_ZIP_PATH = STREAMINGASSETS + "/msbuild_net472.zip";
        MSBUILD_PATH = STREAMINGASSETS + "/msbuild/net472/MSBuild/Current/Bin";

        RESTORE_BAT_PATH = STREAMINGASSETS + "/restore.bat";
        BUILD_BAT_PATH = STREAMINGASSETS + "/build.bat";

        IMDN_SLN_PATH = UNZIP_PATH + "/imobiledevice-net-1.2.186/iMobileDevice.NET.sln";
        MSBUILD_EXE_PATH = MSBUILD_PATH + "/MSBuild.exe";

        EXPORTED_DLL_PATH = UNZIP_PATH + "/imobiledevice-net-1.2.186/iMobileDevice-net/bin/Release/netstandard2.0/iMobileDevice-net.dll";
        FINAL_DLL_EXPORT_PATH = STREAMINGASSETS + "/iMobileDevice-net.dll";

        unzipNum = 1;
        lastReportPer = 0;
    }

    public void Start()
    {
        if (isStartProcessOnAwake)
            StartProcess();
    }

    public void StartProcess()
    {
        SetProgressBarValue(0);

        if (File.Exists(FINAL_DLL_EXPORT_PATH))
            File.Delete(FINAL_DLL_EXPORT_PATH);

        progressPer = 0;

        StartCoroutine(downloadWithProgress(IMDN_URL
            , (progress) =>
            {
                var p = (progress * 100f).ToString("F0");
                SetProgressBarValue((int)(float.Parse(p) / 4f));

                if(p != "0")
                    Debug.Log("Downloading " + p + " %");
            }));
    }

    private IEnumerator downloadWithProgress(string url, Action<float> progress)
    {
        using (var request = UnityWebRequest.Get(url))
        {
            var async = request.SendWebRequest();

            while (true)
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogError(request.error);
                    yield break;
                }

                if (async.isDone)
                {
                    File.WriteAllBytes(ZIP_PATH, request.downloadHandler.data);

                    Extract();
                    yield break;
                }

                yield return null;

                progress(async.progress);
            }
        }
    }

    private async void Extract()
    {
        progress = new Progress<ZipProgress>();
        progress.ProgressChanged += Report;

        await Task.Run(() => UnZipTask(ZIP_PATH, UNZIP_PATH));

        if(File.Exists(MSBUILD_ZIP_PATH))
            await Task.Run(() => UnZipTask(MSBUILD_ZIP_PATH, STREAMINGASSETS));

        Debug.Log("Build");
        BuildImdn();
    }

    private void UnZipTask(string zipPath, string extratPath)
    {
        ZipArchive zip = ZipFile.OpenRead(zipPath);
        zip.ExtractToDirectory(extratPath, progress);
    }

    private void Report(object sender, ZipProgress zipProgress)
    {
        progressPer = ((float)zipProgress.Processed / (float)zipProgress.Total) * 100f;

        var p = progressPer.ToString("F0");
        Debug.Log("Extract " + unzipNum + "/2 : " + p + " %");

        lastReportPer = int.Parse(p);

        SetProgressBarValue((int)((float)lastReportPer / 4f) + (25 * unzipNum));

        if (zipProgress.Total == zipProgress.Processed)
        {
            unzipNum++;
            Debug.Log("Extract is done");
        }
    }

    private async void BuildImdn()
    {
        Debug.Log(RESTORE_BAT_PATH + " " + BUILD_BAT_PATH);

        await Task.Run(() => {
            string restoreBatText = "@echo off\n\n"
                + "if not \"%~0\"==\"%~dp0.\\%~nx0\" (\n"
                + "start /min cmd /c,\"%~dp0.\\%~nx0\"%* \n"
                + "exit\n"
                + ")\n\n"
                + "\"" + MSBUILD_EXE_PATH.Replace("/", "\\") + "\" "
                + "\"" + IMDN_SLN_PATH.Replace("/", "\\") + "\" "
                + "-t:restore";

            File.WriteAllText(RESTORE_BAT_PATH, restoreBatText);
        });

        await Task.Run(() => {
            string buildBatText = "@echo off\n\n"
                + "if not \"%~0\"==\"%~dp0.\\%~nx0\" (\n"
                + "start /min cmd /c,\"%~dp0.\\%~nx0\"%* \n"
                + "exit\n"
                + ")\n\n"
                + "\"" + MSBUILD_EXE_PATH.Replace("/", "\\") + "\" "
                + "\"" + IMDN_SLN_PATH.Replace("/", "\\") + "\" "
                + "-t:build /p:Configuration=Release;Platform=\"x64\"";

            File.WriteAllText(BUILD_BAT_PATH, buildBatText);
        });

        Debug.Log("bat file saved");
        Debug.Log("building...");

        SetProgressBarValue(80);

        await Task.Run(() =>
        {
            Command("\"" + RESTORE_BAT_PATH + "\"");
        });

        SetProgressBarValue(90);

        await Task.Run(() =>
        {
            Command("\"" + BUILD_BAT_PATH + "\"");
        });

        Debug.Log("dll exported");

        SetProgressBarValue(95);

        if (!File.Exists(EXPORTED_DLL_PATH))
        {
            Debug.LogError("Does not exist exported dll file");

            progressBarRT.gameObject.GetComponent<Image>().color = Color.red;
        }
        else
        {
            Debug.Log("Existed dll file");

            File.Move(EXPORTED_DLL_PATH, FINAL_DLL_EXPORT_PATH);

            Directory.Delete(UNZIP_PATH, true);
            File.Delete(ZIP_PATH);
            File.Delete(BUILD_BAT_PATH);
            File.Delete(RESTORE_BAT_PATH);

            //if (File.Exists(MSBUILD_ZIP_PATH))
            //    File.Delete(MSBUILD_ZIP_PATH);

            Debug.Log("Clean Up");

            SetProgressBarValue(100);

            Debug.Log("Done :)");
        }
    }

    private string Command(string cmd)
    {
        var p = new Process();
        string output = "";

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                p.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
                p.StartInfo.Arguments = "/c " + cmd;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();

                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();

                break;
            default:
                Debug.LogError("Can not identify the OS on which the editor is running");
                break;
        }

        return output;
    }

    private void SetProgressBarValue(int value)
    {
        var p = (float)value * 5.5f;
        progressBarRT.sizeDelta = new Vector2(p, 0);
        progressParText.text = value.ToString() + "%";
    }
}
