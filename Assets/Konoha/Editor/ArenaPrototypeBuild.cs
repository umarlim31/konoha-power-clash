using System;
using System.IO;
using Konoha.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Konoha.Editor
{
    public static class ArenaPrototypeBuild
    {
        [MenuItem("Konoha/Build Android Arena Prototype 0.0.2")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch the Unity project to Android before building.");
            ArenaPrototypeProject.Prepare();
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Builds/Android");
            var identity = new BuildIdentity
            {
                build = "0.0.2-arena",
                commit = Environment.GetEnvironmentVariable("KONOHA_COMMIT") ?? "LOCAL UNITY BUILD",
                balance = "power-route-greybox-1",
                server = "N/A - OFFLINE ARENA PROTOTYPE",
                utc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText("Assets/Resources/BuildIdentity.json", JsonUtility.ToJson(identity, true));
            AssetDatabase.ImportAsset("Assets/Resources/BuildIdentity.json", ImportAssetOptions.ForceSynchronousImport);
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ArenaPrototypeProject.ScenePath },
                locationPathName = "Builds/Android/KONOHA_Arena_0.0.2.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Arena Android build failed: " + report.summary.result);
            Debug.Log("Arena prototype APK built. Physical Android test still required.");
        }
    }
}
