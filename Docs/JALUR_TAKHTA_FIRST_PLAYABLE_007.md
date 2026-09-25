# Jalur Takhta 0.0.8.1 — capital environment art pass

Source branch: `feat/jalur-takhta-first-playable`, based on `feat/hero-concept-alignment-006a3`.
The campaign retains a separate scene and Android package. Existing 4v4 generation, networking and match rules are unchanged.

## Manual Android/tablet build

1. In the existing Unity Build Automation Android target, select `feat/jalur-takhta-first-playable`, Unity **6000.0.60f1**.
2. Keep pre-export method **`Konoha.Editor.SpikeProject.prepare`**. This generates `Assets/Konoha/Generated/JalurTakhtaPreview.unity` and the saved meshes, textures, materials and solo URP asset under `Assets/Konoha/Generated/Capital`.
3. Start the build manually. Install the APK: **KONOHA Jalur Takhta Preview**, version **0.0.8.1**, Android code **18**, package `com.konoha.powerclash.jalurtakhta`.
4. Confirm the bottom label reads `JALUR TAKHTA 0.0.8.1 • SOLO PREVIEW`. An old label means the previous APK/commit was built or installed.

If the target does not run EditMode tests, an APK build only verifies import/compilation. The tests in the repository still require a Unity test runner. Send the build log if compilation or export fails.

## What changed after the 0.0.8 recording

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

The updated scene test reloads the saved scene, checks persisted meshes/materials and solo shadow settings, confirms old blockers are removed, and samples capsule clearance along the campaign route. A guard-route test checks that the monument is avoided and the throne can be reached. They are prepared tests, not reported passes.

This is an implemented procedural environment art pass, with original fictional civic motifs. It is not a validated match to the premium concept board. Characters retain their existing prototype models and animation; water uses a stylized surface rather than real-time planar reflections. Final sculpted assets, authored texture maps, NPC crowds and production animation remain future work. The PR stays draft until the manual APK has been reviewed on device.
