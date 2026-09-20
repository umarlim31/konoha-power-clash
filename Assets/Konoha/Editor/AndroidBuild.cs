using System;
using System.IO;
using System.Text.RegularExpressions;
using Konoha.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Konoha.Editor
{
    public static class AndroidBuild
    {
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Start Unity with -buildTarget Android.");
            string commit = Environment.GetEnvironmentVariable("KONOHA_COMMIT") ?? "";
            if (!Regex.IsMatch(commit, "^[0-9a-f]{40}$"))
                throw new InvalidOperationException("KONOHA_COMMIT must be a full Git SHA.");
            SpikeProject.Prepare();
            var identity = new BuildIdentity { commit = commit, utc = DateTime.UtcNow.ToString("O") };
            string json = JsonUtility.ToJson(identity, true);
            File.WriteAllText("Assets/Resources/BuildIdentity.json", json);
            AssetDatabase.ImportAsset("Assets/Resources/BuildIdentity.json", ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory("Builds/Android");
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SpikeProject.ScenePath },
                locationPathName = "Builds/Android/KONOHA_0.0.1.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            File.WriteAllText("Builds/Android/build-identity.json", json);
            Debug.Log("KONOHA APK COMPILED. DEVICE GATE REMAINS PENDING.");
        }
    }
}
