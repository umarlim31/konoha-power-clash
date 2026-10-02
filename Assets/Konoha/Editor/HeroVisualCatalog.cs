using System;
using System.Collections.Generic;
using System.IO;
using Konoha.Character;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Konoha.Editor
{
    // Chooses each hero's visual for the generated PvP prefabs (and, through them, the
    // solo campaign). When Assets/Konoha/Art/Heroes/<Hero>/<Hero>.fbx exists, the visual
    // is built from that model; otherwise the primitive visual is kept untouched, so the
    // output matches 0.0.8.2. Any problem with a model is logged and falls back to the
    // primitive: generation must never throw because of owner-supplied art.
    public static class HeroVisualCatalog
    {
        public const string HeroesRoot = "Assets/Konoha/Art/Heroes";
        public const string ControllerFolder = SpikeProject.Generated + "/Heroes";
        public const float TargetHeight = 1.7f;
        public const float RunThreshold = 0.1f;

        // Index order matches Konoha.Networking.PrototypeHero (Mega, Gemoy, Abah, Pak Wi).
        public static readonly string[] HeroFolders = { "Mega", "Gemoy", "Abah", "PakWi" };
        public static readonly string[] ClipNames = { "Idle", "Run", "Attack", "Skill", "Hit", "Jump", "Runtuh" };

        private static readonly Dictionary<string, RuntimeAnimatorController> Controllers =
            new Dictionary<string, RuntimeAnimatorController>();

        // Called once per generation pass: the player and bot prefabs share one controller per hero.
        public static void BeginGeneration() => Controllers.Clear();

        public static string ModelPath(int hero) => HeroesRoot + "/" + HeroFolders[hero] + "/" + HeroFolders[hero] + ".fbx";

        public static string ClipPath(int hero, string clip) =>
            HeroesRoot + "/" + HeroFolders[hero] + "/" + HeroFolders[hero] + "@" + clip + ".fbx";

        public static bool IsLoopingClip(string clip) => clip == "Idle" || clip == "Run";

        public static bool IsHeroModelAsset(string assetPath) =>
            assetPath.StartsWith(HeroesRoot + "/", StringComparison.Ordinal) &&
            assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);

        // Parses Assets/Konoha/Art/Heroes/<Hero>/<Hero>@<Clip>.fbx (names are case-sensitive).
        public static bool TryParseClipPath(string assetPath, out string hero, out string clip)
        {
            hero = clip = null;
            if (!IsHeroModelAsset(assetPath)) return false;
            string file = Path.GetFileNameWithoutExtension(assetPath);
            int at = file.IndexOf('@');
            if (at <= 0 || at == file.Length - 1) return false;
            string folder = Path.GetFileName(Path.GetDirectoryName(assetPath));
            hero = file.Substring(0, at);
            clip = file.Substring(at + 1);
            return hero == folder && Array.IndexOf(ClipNames, clip) >= 0;
        }

        // Returns the visual to use for this hero. The primitive is returned unchanged when
        // no model exists; otherwise it is replaced in place by a model-based visual.
        public static GameObject Resolve(int hero, GameObject primitiveVisual)
        {
            if (primitiveVisual == null || hero < 0 || hero >= HeroFolders.Length) return primitiveVisual;
            GameObject model = TryCreateModelVisual(hero, primitiveVisual.name, primitiveVisual.transform.parent);
            if (model == null) return primitiveVisual;
            model.transform.SetSiblingIndex(primitiveVisual.transform.GetSiblingIndex());
            Object.DestroyImmediate(primitiveVisual);
            return model;
        }

        private static GameObject TryCreateModelVisual(int hero, string visualName, Transform parent)
        {
            string path = ModelPath(hero);
            if (!File.Exists(path)) return null;
            GameObject root = null;
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null)
                {
                    Debug.LogWarning("Hero model not imported, keeping primitive: " + path);
                    return null;
                }
                root = new GameObject(visualName);
                root.transform.SetParent(parent, false);
                var model = Object.Instantiate(source);
                model.name = HeroFolders[hero] + "Model";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;

                // The CharacterController owns collision; imported colliders, cameras and
                // lights would interfere with movement, the orbit camera and performance.
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var camera in model.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(camera);
                foreach (var light in model.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);

                if (!FitToHeight(root.transform, model.transform))
                    Debug.LogWarning("Hero model has no renderable bounds; scale left unchanged: " + path);

                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                if (animator.avatar == null) animator.avatar = LoadSubAsset<Avatar>(path);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.runtimeAnimatorController = GetController(hero);

                var driver = root.AddComponent<HeroAnimatorDriver>();
                driver.animator = animator;
                return root;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Hero model " + path + " could not be used, keeping primitive: " + exception.Message);
                if (root != null) Object.DestroyImmediate(root);
                return null;
            }
        }

        // Uniformly scales the model to TargetHeight and puts the lowest point on the root's y=0.
        private static bool FitToHeight(Transform root, Transform model)
        {
            if (!TryGetBounds(model, out Bounds bounds) || bounds.size.y < 0.01f) return false;
            model.localScale *= TargetHeight / bounds.size.y;
            if (!TryGetBounds(model, out bounds)) return false;
            model.position += Vector3.up * (root.position.y - bounds.min.y);
            return true;
        }

        private static bool TryGetBounds(Transform model, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static RuntimeAnimatorController GetController(int hero)
        {
            string path = ControllerFolder + "/" + HeroFolders[hero] + "Hero.controller";
            if (Controllers.TryGetValue(path, out var cached) && cached != null) return cached;
            if (!AssetDatabase.IsValidFolder(ControllerFolder))
                AssetDatabase.CreateFolder(SpikeProject.Generated, "Heroes");
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(HeroAnimatorDriver.SpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(HeroAnimatorDriver.AttackTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HeroAnimatorDriver.SkillTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HeroAnimatorDriver.HitTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HeroAnimatorDriver.JumpTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HeroAnimatorDriver.RuntuhBool, AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            // Idle is always the default state; without an Idle clip it holds the bind pose.
            var idle = machine.AddState("Idle");
            idle.motion = LoadClip(hero, "Idle");
            machine.defaultState = idle;

            var runClip = LoadClip(hero, "Run");
            if (runClip != null)
            {
                var run = machine.AddState("Run");
                run.motion = runClip;
                var toRun = idle.AddTransition(run);
                Configure(toRun, false, 0.1f);
                toRun.AddCondition(AnimatorConditionMode.Greater, RunThreshold, HeroAnimatorDriver.SpeedParameter);
                var toIdle = run.AddTransition(idle);
                Configure(toIdle, false, 0.12f);
                toIdle.AddCondition(AnimatorConditionMode.Less, RunThreshold, HeroAnimatorDriver.SpeedParameter);
            }

            foreach (string trigger in new[] { HeroAnimatorDriver.AttackTrigger, HeroAnimatorDriver.SkillTrigger,
                         HeroAnimatorDriver.HitTrigger, HeroAnimatorDriver.JumpTrigger })
            {
                var clip = LoadClip(hero, trigger);
                if (clip == null) continue;
                var state = machine.AddState(trigger);
                state.motion = clip;
                var enter = machine.AddAnyStateTransition(state);
                Configure(enter, false, 0.08f);
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, HeroAnimatorDriver.RuntuhBool);
                var back = state.AddTransition(idle);
                Configure(back, true, 0.1f);
                back.exitTime = 0.9f;
            }

            var runtuhClip = LoadClip(hero, "Runtuh");
            if (runtuhClip != null)
            {
                var runtuh = machine.AddState("Runtuh");
                runtuh.motion = runtuhClip;
                var enter = machine.AddAnyStateTransition(runtuh);
                Configure(enter, false, 0.1f);
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0f, HeroAnimatorDriver.RuntuhBool);
                var recover = runtuh.AddTransition(idle);
                Configure(recover, false, 0.2f);
                recover.AddCondition(AnimatorConditionMode.IfNot, 0f, HeroAnimatorDriver.RuntuhBool);
            }

            EditorUtility.SetDirty(controller);
            Controllers[path] = controller;
            return controller;
        }

        private static void Configure(AnimatorStateTransition transition, bool hasExitTime, float duration)
        {
            transition.hasExitTime = hasExitTime;
            transition.duration = duration;
        }

        private static AnimationClip LoadClip(int hero, string clip)
        {
            string path = ClipPath(hero, clip);
            if (!File.Exists(path)) return null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip animation && !animation.name.StartsWith("__preview__", StringComparison.Ordinal))
                    return animation;
            Debug.LogWarning("No animation clip found in " + path + "; state skipped.");
            return null;
        }

        private static T LoadSubAsset<T>(string path) where T : Object
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is T value) return value;
            return null;
        }
    }
}
