# Jalur Takhta 0.0.8 — visual rebuild preview

Source branch: `feat/jalur-takhta-first-playable`, based on `feat/hero-concept-alignment-006a3`.
The existing 4v4 scene, networking, bots and match rules remain in the repository. This branch directs the **existing manual Unity Build Automation pre-export method** to a separate campaign preview scene.

## Build manually from Android/tablet

1. In the existing Unity Build Automation Android target, select source branch `feat/jalur-takhta-first-playable` and keep Unity **6000.0.60f1**.
2. Leave pre-export method `Konoha.Editor.SpikeProject.prepare` unchanged. On this branch it prepares `Assets/Konoha/Generated/JalurTakhtaPreview.unity` as the only enabled build scene.
3. Start the build manually in the mobile browser, then download and install its Android APK. The app name is **KONOHA Jalur Takhta Preview**, version `0.0.8`, code 17, with package ID `com.konoha.powerclash.jalurtakhta`. It updates the earlier solo preview and can stay installed beside the 4v4 app.
4. On startup, confirm the bottom text says `JALUR TAKHTA 0.0.8 • SOLO PREVIEW`. The game begins offline without host/join. No local Unity Editor or PC interaction is needed.

If the target does not execute EditMode tests, an APK build verifies import/compilation but does not verify the three state tests or scene-wiring test. Send the build log if compilation or export fails.

## Playthrough to check on device

1. Drag the joystick forward from Gerbang Rakyat to the green Plaza Aspirasi marker.
   The smaller top banner and a direction/distance prompt show the next destination. If you walk along a boundary, the camera should keep more of the arena in view and show a garden outside the wall rather than a black void.
2. Go left to **Majelis Daun** and stay within its gold outlined zone for 2.5 seconds; watch the bar beneath the top objective fill. Leaving the zone slowly reduces progress. Go right to **Biro Prosedur**, stand within its matching outline and press **SAHKAN** three times, about 0.65 second apart. Both Segel open the inner route.
3. Approach Garda Takhta beyond the central seat. Use **BASIC** at close range (four hits from 100 HP), or a hero skill, to defeat it. Press **GANTI HERO** to try a different kit: Mega has a 5-second shield and close-range attack; Gemoy has a heavy hit; Abah heals and briefly stops the guard; Pak Wi runs faster for 5 seconds. Each skill has a cooldown.
4. Watch the red seat enclosure disappear, enter the Kursi area and tap **DUDUK**. Remain near the seat to build Kuasa. A counterattack arrives shortly afterward; BASIC can defeat it. If pushed away or knocked out, Kuasa pauses until you sit again.
5. Reach 35 Kuasa to show the prototype result. Press **ULANG** to start a fresh run without closing the app.

Check joystick comfort, direction prompt at the north and west edges, legibility of left/right sector markers, gate collision, attack range, camera tracking, text safe area on phone and tablet, and whether a full run can be completed without a restart. The SAHKAN/DUDUK button appears only near its target; BASIC appears once the guard arrives. A recording reaching the two seals and the first guard would verify the rest of the loop. When entering Majelis, stop moving until the bar fills; this was hard to understand in the 0.0.7.2 recording.

## Honest scope

This is an **offline solo gameplay preview**. It reuses existing movement and builds a stone plaza from procedural geometry. The 0.0.7.2 recording spent much of its time around Majelis without earning the first seal. The unbuilt 0.0.7.3 source drew the interaction boundary at the actual trigger radius, added a progress bar and clearer single-step instructions, and eased the hold. Version 0.0.8 adds a lower solo camera, procedural stone paving and water ripples, two institution building volumes with distinct rooflines, a thinner sealed throne gate in place of the red box, and a smaller solo HUD and joystick. The concentric plaza, reflecting pools and bridges, enlarged original winged monument, distant fictional domed hall, guardian silhouettes and courtyard palms are retained from 0.0.7.3. These remain lightweight stylized placeholders, not a 3D reproduction of the concept sheet. The four hero actions and two sector tasks are playable placeholders; the guard is still a simple chasing enemy. Dodge, Modal/Koneksi, branching negotiation, cutscenes, saves, co-op, production animation, and finished character and environment models are not implemented. The bird monument is an original fictional motif, not an official Garuda emblem. Scene generation and Android compilation require the manual Unity Build Automation run; source checks cannot certify an APK or device performance.

## Visual acceptance on phone and tablet

- From Gerbang Rakyat, the player, two sector entrances, chair and northern hall should each read as distinct landmarks. The seat should have thin red and gold seals, without an opaque red cage.
- While approaching Majelis, the building should show a pitched roof; Biro should show a flat layered roof. Both zones have an outlined interaction radius matching the actual trigger.
- Walk around the gate and both bridges. Check that any place where geometry looks solid also has an obvious route or a deliberate blocker. Decorative water is shallow and walkable outside the visual bridge in this prototype.
- Watch for joystick/button overlap and excessive top-panel obstruction at phone landscape and tablet aspect ratios. Report device model, FPS impression and a screenshot at spawn, at Majelis, and at the open throne.

The concept Bible v2 calls for solo-first Jalur Takhta and retains Rebut Kursi as a separate PvP mode. We have deliberately kept the campaign preview on its own generated scene and separate Android package. The prior source branch remains available for testing the 4v4 game.
