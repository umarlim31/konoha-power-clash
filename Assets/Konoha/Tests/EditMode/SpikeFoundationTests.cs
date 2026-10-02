using Konoha.Character;
using Konoha.Core;
using Konoha.Editor;
using Konoha.Input;
using Konoha.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Konoha.Tests
{
    public sealed class SpikeFoundationTests
    {
        [OneTimeSetUp] public void Prepare() => SpikeProject.Prepare();

        [Test] public void DiagonalIntentCannotExceedCardinalSpeed()
        {
            var intent = new MoveIntent(new Vector2(1f, 1f));
            Assert.That(intent.WorldDirection.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(intent.WorldDirection.y, Is.Zero);
        }

        [Test] public void DeadZoneRemovesSmallFingerNoise()
        {
            Assert.That(TouchJoystick.RemapDeadZone(new Vector2(0.05f, 0f), 0.12f), Is.EqualTo(Vector2.zero));
            Assert.That(TouchJoystick.RemapDeadZone(Vector2.right, 0.12f).x, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test] public void SecondFingerCannotReleaseOwnedJoystick()
        {
            var stick = UnityEngine.Object.FindFirstObjectByType<TouchJoystick>();
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, stick.pad.position + stick.pad.right * 80f);
            var first = new PointerEventData(EventSystem.current) { pointerId = 1, position = point };
            var second = new PointerEventData(EventSystem.current) { pointerId = 2, position = point - Vector2.right * 200f };
            stick.OnPointerDown(first);
            Vector2 ownedValue = stick.Value;
            Assert.That(ownedValue.sqrMagnitude, Is.GreaterThan(0f));
            stick.OnPointerDown(second);
            stick.OnDrag(second);
            stick.OnPointerUp(second);
            Assert.That(stick.Value, Is.EqualTo(ownedValue));
            stick.OnPointerUp(first);
            Assert.That(stick.Value, Is.EqualTo(Vector2.zero));
        }

        [Test] public void InterruptionResetStopsMovementIntent()
        {
            var stick = UnityEngine.Object.FindFirstObjectByType<TouchJoystick>();
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, stick.pad.position + stick.pad.right * 80f);
            stick.OnPointerDown(new PointerEventData(EventSystem.current) { pointerId = 7, position = point });
            Assert.That(stick.Value.sqrMagnitude, Is.GreaterThan(0f));
            stick.SendMessage("OnApplicationPause", true);
            Assert.That(stick.Value, Is.EqualTo(Vector2.zero));
            Assert.That(stick.handle.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test] public void NetworkingFoundationPackagesAndAssembliesResolve()
        {
            Assert.That(NetworkingFoundationCompileGate.NetcodeType.Assembly.GetName().Name,
                Is.EqualTo("Unity.Netcode.Runtime"));
            Assert.That(NetworkingFoundationCompileGate.NetcodeTransportAdapterType.Assembly.GetName().Name,
                Is.EqualTo("Unity.Netcode.Runtime"));
            Assert.That(NetworkingFoundationCompileGate.TransportType.Assembly.GetName().Name,
                Is.EqualTo("Unity.Networking.Transport"));
        }

        [Test] public void AndroidIsArm64Il2CppLandscapeOnly()
        {
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android), Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
        }

        [Test] public void UrpAndArenaAreWired()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(SpikeProject.ScenePath));
            Assert.That(UnityEngine.Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var driver = UnityEngine.Object.FindFirstObjectByType<OfflineSpikeDriver>();
            Assert.That(driver.motor.definition, Is.Not.Null);
            Assert.That(driver.joystick, Is.Not.Null);
            Assert.That(driver.motor.allowJump, Is.False);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<MobileCombatCamera>().allowOrbit, Is.False);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<MobileCombatCamera>().target, Is.EqualTo(driver.motor.transform));
        }

        [Test] public void ArenaHasSolidPerimeterAndSafeSpawn()
        {
            Physics.SyncTransforms();
            foreach (var name in new[] { "NorthBoundary", "SouthBoundary", "EastBoundary", "WestBoundary" })
                Assert.That(GameObject.Find(name).GetComponent<BoxCollider>().isTrigger, Is.False);
            var motor = UnityEngine.Object.FindFirstObjectByType<CharacterMotor>();
            Assert.That(Physics.Raycast(motor.transform.position + Vector3.up * 0.1f, Vector3.down, out var hit, 2f), Is.True);
            Assert.That(hit.collider.gameObject.name, Is.EqualTo("Floor"));
        }

        [Test] public void SafeAreaRootAndPadAreConnected()
        {
            var layout = UnityEngine.Object.FindFirstObjectByType<SafeAreaLayout>();
            Assert.That(layout.joystick.parent, Is.EqualTo(layout.safeRoot));
            Assert.That(layout.joystick.GetComponent<TouchJoystick>(), Is.Not.Null);
        }
    }
}
