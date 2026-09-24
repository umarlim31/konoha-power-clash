# Negara Konoha — Arena gameplay prototype 0.0.2

Status: source implementation for Unity 6000.0.60f1. Editor compilation, Android build, and device playtest are pending. Character and arena concept sheets are art direction, not imported 3D game assets. The attached Game Concept Bible v2 was unavailable to this implementation; gameplay numbers here are temporary.

## What this prototype lets a player do

1. Start at the south approach with the existing touch joystick and follow camera.
2. Enter the red capture area for KPU, Komisi, and Parlemen in any order. Stay near each objective for 2.5 seconds; leaving resets its progress.
3. When all three indicators turn green, the central takhta becomes active. Stay near it for 4 seconds to finish the local route.

This is an offline traversal and readability test. It has no opponents, combat, simultaneous captures, cooldowns, characters, matchmaking, or server-authoritative state. The institution labels represent satirical fictional game objectives; the scene uses procedural placeholder architecture and an abstract bird statue.

## Open and build in Unity

1. Check out the feature branch or merge its PR. Open the repository root in **Unity 6000.0.60f1** with Android Build Support.
2. Let the project import. Choose **Konoha > Prepare Arena Prototype 0.0.2 (regenerates scene)**. Opened scene: `Assets/Konoha/Generated/PowerRouteArena.unity`.
3. Press Play to inspect the route. Mouse dragging the joystick works in Editor. In **File > Build Profiles**, switch the active platform to Android.
4. Choose **Konoha > Build Android Arena Prototype 0.0.2**. Output: `Builds/Android/KONOHA_Arena_0.0.2.apk`.
5. Install on a physical Android device. Verify portrait is disabled; navigate all three objectives, walk away midway to confirm capture reset, and verify the throne stays locked until all three are captured.

`Prepare` regenerates this prototype scene and also regenerates the original 0.0.1 spike scene. Hand edits in either generated scene will be lost. Change the generator source for persistent revisions. The original 0.0.1 build method remains unchanged.

## Visual and production assessment

The concept sheets establish a strong color and silhouette split: Mega red with flowing textile, Gemoy cream/red with a heavy commander silhouette, Abah deep green with a staff, and Pak Wi white/red with a mobile utility silhouette. The arena concept offers an overhead-readable central monument, side approaches, cover, and Nusantara-influenced architecture. The biggest gap is that none of these sheets are rigged 3D models or licensed production assets in GitHub. Close facial resemblance to real people and official bird/party insignia should be redesigned into clearly original shapes before publishing, while preserving the distinct playable silhouettes. The older name **Moi** on one sheet also conflicts with the later **Gemoy** naming decision.

Priority after this scene compiles and is tested:

1. Establish exact objective rules and timings from the latest approved Bible v2 and run a multiplayer fairness review. The three-step route here is a proposed vertical slice, not a frozen ruleset.
2. Make one playable original-IP hero as a low-poly, rigged, Android-budget prefab with LOD and animation, then validate scale and camera readability on a real phone.
3. Convert arena geometry into an authored modular kit, preserving the clear center and side paths. Add cover limits and collision tests before decoration.
4. Prototype network-authoritative objectives and combat in a separate phase. Never trust this local `PowerRoute` as online match authority.

## Initial visual feedback before freeze

- Four character sheets contain several text and naming inconsistencies; the Gemoy sheet still says Moi. Replace the photorealistic real-person reference panels with a separate internal reference document before public-facing use.
- Some ornamental details are too fine for an Android isometric camera. Test enlarged color blocks and strong outlines at actual screen size.
- The concept arena is denser than a fair greybox can safely assume. Preserve line of sight, distinguish walkable stone from decorative water, and give every objective a clearly visible capture radius.
- Confirm whether KPU, Komisi, and Parlemen are the final institution names and whether capturing is serial or simultaneous from the unavailable v2 document.
