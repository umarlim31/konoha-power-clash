# Jalur Takhta 0.0.8.2 — open capital, orbit camera and traversal

Source branch: `feat/jalur-takhta-first-playable`, based on `feat/hero-concept-alignment-006a3`.
The campaign retains a separate scene and Android package. Existing 4v4 generation, networking and match rules are unchanged.

## Manual Android/tablet build

1. In the existing Unity Build Automation Android target, select `feat/jalur-takhta-first-playable`, Unity **6000.0.60f1**.
2. Keep pre-export method **`Konoha.Editor.SpikeProject.prepare`**. This generates `Assets/Konoha/Generated/JalurTakhtaPreview.unity` and the saved meshes, textures, materials and solo URP asset under `Assets/Konoha/Generated/Capital`.
3. Start the build manually. Install the APK: **KONOHA Jalur Takhta Preview**, version **0.0.8.2**, Android code **19**, package `com.konoha.powerclash.jalurtakhta`.
4. Confirm the bottom label reads `JALUR TAKHTA 0.0.8.2 • SOLO PREVIEW`. An old label means the previous APK/commit was built or installed.

If the target does not run EditMode tests, an APK build only verifies import/compilation. The tests in the repository still require a Unity test runner. Send the build log if compilation or export fails.

## Changes requested after the 0.0.8.1 device build

The user reported a significant visual improvement, but wanted free exploration, camera control and a fix for getting trapped in water. The supplied `7162.mp4` exceeded the 32 MiB transfer limit and was not inspected; this revision follows the written report and source audit.

- Removed all four rendered perimeter walls and their colliders. The campus now uses a logical oval movement boundary with radii 26 m / 29 m around (0, 4), spanning approximately 52 x 58 m. The previous walkable rectangle was 32 x 24 m. This is bounded exploration, not an unlimited open world.
- A separate oval stone surface blends the capital into surrounding lawns. The rectangular collision foundation is invisible and extends beyond the movement boundary. Garden promenades, pavilion shelters, planted beds, palms and a southern skyline fill the additional views.
- Players can approach the civic building and its raised terrace. Its stairs now meet a solid podium; reachable buildings, columns and palm trunks have collision.
- Drag unused right-side screen space to orbit 360 degrees and adjust elevation. Pinch with two fingers on that camera surface to zoom. A finger operating the left joystick is excluded from the camera gesture. Action buttons are above the gesture surface. **KAMERA AWAL** resets orientation and distance.
- Joystick movement and direction prompts follow the camera. The camera follows beyond the old arena limits and retracts in front of solid buildings. The central monument retains its separate fade behavior.
- **LOMPAT** performs a grounded 1.25 m jump. It does not allow repeated air jumps. The locked throne enclosure remains high enough to prevent skipping it by jumping.
- Old pool coping was .44 m high while the old controller step limit was .20 m. Coping is now .12 m, with 1.9 m wide shallow walk-out ramps at both ends of each pool. The solo step allowance is .25 m. Jumping is optional for leaving the shallow water.
- Falling below the world restores the character to a recent dry grounded position. Respawn/restart also clears vertical velocity. Overview pauses the traversal driver and restores it on exit.
- Jump and orbit are opt-in on shared components; the existing PvP input driver, prefab defaults and fixed camera behavior are retained.

## Device checks for 0.0.8.2

1. Confirm the version label, then walk across the former north/south/east/west wall positions. Explore the garden pavilions and civic forecourt. At the outer oval limit, the hero should stop or slide along it without a wall appearing.
2. Keep moving with the left joystick while dragging the right side. After a half-turn, joystick-up must still move deeper into the screen. Tap attacks and jump without causing a camera drag. Test pinch and KAMERA AWAL.
3. Enter each pool from both sides, then leave through its north and south openings **without jumping**. Jump off each bridge and return. Report the exact spot if collision still traps the hero.
4. Jump repeatedly: one launch per landing, no air jump, normal landing. Approach the locked seat while jumping; the mission gate must remain effective.
5. Enter LIHAT ARENA while moving and after rotating the camera. KEMBALI MAIN must resume the previous view and controls with no stuck movement or pause.
6. Complete both seals, Garda, throne and counterattack. Check performance with shadows and the expanded scenery on the actual phone/tablet.

## Environment foundation carried forward from 0.0.8.1

The earlier arena inherited disabled shadows and assembled landmarks largely from primitive boxes. This revision replaces its environment generator with a dedicated capital art pass:

- Saved custom meshes for an octagonal, tapered central monument; curved pointed bronze feathers; profiled columns; tiered hip roofs; arches; dome; palms; and thin circular inlays.
- The Garuda Konoha monument now sits within the central plaza, behind the throne. The northern civic hall, domed roof, side institutions, guardian sculptures, folded red/white banners, garden beds and distant buildings frame its silhouette.
- Andesite, pale stone, patinated roof, bronze and cloth materials have distinct surface settings. Fine grain and staggered stone joints replace the earlier coarse floor pattern.
- Solo URP enables main-light soft shadows, a 2048 shadow map, two cascades and a 48 m shadow distance. Tropical sky, gradient ambient lighting and distant fog add depth. MSAA remains 2x, HDR stays off; the PvP pipeline asset is separate and unchanged.
- Pool bridges have physical decks and 15-degree entry ramps. Old PvP covers, relays and their colliders are removed from the solo scene.
- The monument fades when it obscures the player's torso; saved transparent variants are included in the scene. The guard takes a route around the monument footprint.
- **LIHAT ARENA** opens a slowly moving overview, pauses the solo game and hides other controls. **KEMBALI MAIN** restores the camera, HUD, input and time scale.

Scene elements use shared materials and saved mesh assets. Static scenery is marked for batching; the fading monument is excluded from static batching. These are budget choices, not measured device performance.

## Device acceptance

First use **LIHAT ARENA** for a full-arena recording, then **KEMBALI MAIN** for the gameplay check. The source has not been rendered by Unity in the authoring environment, so the APK is the required visual acceptance step.

- At spawn: verify the central winged monument, roof silhouettes, pale main lane, side institutions and cast shadows are recognizable; check for pink materials, missing meshes or flicker.
- Walk north behind the monument: verify it fades only when blocking the avatar and becomes opaque again when the sightline clears. In overview it should be fully visible.
- Check both bridge approaches, boundary walls and the two institution entrances. The shallow water remains non-lethal and walkable; bridge decks rise physically above it.
- Enter/exit overview while moving: the joystick must reset; gameplay and the previous camera must resume without a stuck pause or hidden controls.
- On phone and tablet, check the top-left overview button, objective panel, joystick and action buttons for overlap. Record device model, frame-rate impression and any sustained heat/stutter. Shadows may require tuning after measured device results.

## Complete solo playthrough

1. Move forward from Gerbang Rakyat to the bronze **Plaza Aspirasi** ring.
2. Go left to **Majelis Daun**, remain within its ring for 2.5 seconds and watch the progress bar. Go right to **Biro Prosedur**, stand in its ring and tap **SAHKAN** three times, at least 0.65 seconds apart.
3. Follow the direction prompt around the central enclosure to **Garda Takhta**. Use BASIC at close range or a hero skill. GANTI HERO cycles Mega, Gemoy, Abah and Pak Wi.
4. After defeating Garda, the thin red/gold seals disappear. Approach the throne from the south, tap **DUDUK** and remain nearby. The counterattack should move around the monument instead of crossing through its base.
5. Reach 35 Kuasa. Tap **ULANG** to restart. Verify the whole route, both seals, guard fight, throne and counterattack in one run.

## Validation and remaining scope

Local validation: `python scripts/source-check.py` and `git diff --check`. No Unity Editor or C# compiler is available in this workspace. Unity import, compilation, EditMode tests, APK generation and device rendering/performance have **not** been run here.

The scene test reloads saved assets, checks separate rendering settings and removed walls, checks overview pause/restore, and samples mission-route clearance. It also moves a CharacterController out of all four pool exits, exercises jump/landing and checks the locked gate. Additional tests cover camera-relative movement, oval bounds, gesture ownership and orbit limits. PvP tests check jump/orbit remain disabled. These are prepared Unity tests, not reported passes.

This is an implemented procedural environment art pass, with original fictional civic motifs. It is not a validated match to the premium concept board. Characters retain their existing prototype models and animation; water uses a stylized surface rather than real-time planar reflections. Final sculpted assets, authored texture maps, NPC crowds and production animation remain future work. The PR stays draft until the manual APK has been reviewed on device.
