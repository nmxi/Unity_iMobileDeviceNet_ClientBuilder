using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

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

    //private static string[] EXCLUSION_DIRS;

    private Progress<ZipProgress> progress;

    private float progressPer;

    private void Awake()
    {
        STREAMINGASSETS = Application.dataPath + "/StreamingAssets";

        IMDN_URL = "https://github.com/libimobiledevice-win32/imobiledevice-net/archive/v1.2.186.zip";
        ZIP_PATH = Application.dataPath + "/StreamingAssets/v1.2.186.zip";
        UNZIP_PATH = Application.dataPath + "/StreamingAssets/imdn";
        MSBUILD_ZIP_PATH = Application.dataPath + "/StreamingAssets/msbuild_net472.zip";
        MSBUILD_PATH = Application.dataPath + "/StreamingAssets/msbuild/net472/MSBuild/Current/Bin";

        RESTORE_BAT_PATH = Application.dataPath + "/StreamingAssets/restore.bat";
        BUILD_BAT_PATH = Application.dataPath + "/StreamingAssets/build.bat";

        IMDN_SLN_PATH = UNZIP_PATH + "/imobiledevice-net-1.2.186/iMobileDevice.NET.sln";
        MSBUILD_EXE_PATH = MSBUILD_PATH + "/MSBuild.exe";

        //var bp = UNZIP_PATH + "/imobiledevice-net-1.2.186/";
        //EXCLUSION_DIRS = new String[1];
        //EXCLUSION_DIRS[0] = bp + "iMobileDevice.Generator";
        //EXCLUSION_DIRS[1] = bp + "iMobileDevice.Generator.Tests";
        //EXCLUSION_DIRS[2] = bp + "iMobileDevice.IntegrationTests.net45";
        //EXCLUSION_DIRS[3] = bp + "iMobileDevice.IntegrationTests.netcoreapp30";
        //EXCLUSION_DIRS[4] = bp + "iMobileDevice.Tests";
        //EXCLUSION_DIRS[5] = bp + "iMobileDevice-net.Demo";
    }

    public void StartProcess()
    {
        progress = new Progress<ZipProgress>();
        progress.ProgressChanged += Report; progressPer = 0;

        progressPer = 0;

        StartCoroutine(downloadWithProgress(IMDN_URL
            , (progress) =>
            {
                Debug.Log("Downloading " + (progress * 100f).ToString("F2") + " %");
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
        await Task.Run(() => UnZipTask(ZIP_PATH, UNZIP_PATH));

        //await Task.Run(() => {
        //    try
        //    {
        //        foreach (var dir in EXCLUSION_DIRS)
        //        {
        //            Directory.Delete(dir, true);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError(e);
        //    }
        //});

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

        Debug.Log("Extract " + progressPer.ToString("F2") + " %");

        if (zipProgress.Total == zipProgress.Processed)
        {
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

        await Task.Run(() => {
            ExtUtils.Command(RESTORE_BAT_PATH);
        });

        await Task.Run(() => {
            ExtUtils.Command(BUILD_BAT_PATH);
        });

        Debug.Log("dll exported");
    }
}
