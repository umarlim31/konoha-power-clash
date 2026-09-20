using System;
using UnityEngine;

namespace Konoha.Diagnostics
{
    [Serializable]
    public sealed class BuildIdentity
    {
        public string build = "0.0.1";
        public string commit = "UNSTAMPED - EDITOR ONLY";
        public string balance = "spike-locomotion-candidate-1";
        public string server = "N/A - OFFLINE 0.0.1";
        public string utc = "UNSTAMPED";
        public static BuildIdentity Load()
        {
            var asset = Resources.Load<TextAsset>("BuildIdentity");
            return asset == null ? new BuildIdentity() : JsonUtility.FromJson<BuildIdentity>(asset.text);
        }
    }
}
