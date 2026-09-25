# Jalur Takhta 0.0.7 — first playable solo greybox

Source branch: `feat/jalur-takhta-first-playable`, based on `feat/hero-concept-alignment-006a3`.
The existing 4v4 scene, networking, bots and match rules remain in the repository. This branch directs the **existing manual Unity Build Automation pre-export method** to a separate campaign preview scene.

## Build manually from Android/tablet

1. In the existing Unity Build Automation Android target, select source branch `feat/jalur-takhta-first-playable` and keep Unity **6000.0.60f1**.
2. Leave pre-export method `Konoha.Editor.SpikeProject.prepare` unchanged. On this branch it prepares `Assets/Konoha/Generated/JalurTakhtaPreview.unity` as the only enabled build scene.
3. Start the build manually in the mobile browser, then download and install its Android APK. The app name is **KONOHA Jalur Takhta Preview**, version `0.0.7.0`, with a separate package ID `com.konoha.powerclash.jalurtakhta`. The older 4v4 installation can stay installed.
4. On startup, confirm the bottom text says `JALUR TAKHTA 0.0.7 - SOLO GREYBOX PREVIEW`. The game begins offline without host/join. No local Unity Editor or PC interaction is needed.

If the target does not execute EditMode tests, an APK build verifies import/compilation but does not verify the three state tests or scene-wiring test. Send the build log if compilation or export fails.

## Playthrough to check on device

1. Drag the joystick forward from Gerbang Rakyat to the green Plaza Aspirasi marker.
2. Go left to Majelis Daun and right to Biro Prosedur. Stand in each marked zone for about 2.5 seconds to earn two different Segel; the hold resets on exit.
3. Approach Garda Takhta beyond the central seat. Use **BASIC** within range until its greybox HP reaches zero.
4. Watch the red seat enclosure disappear, enter the Kursi area and tap **DUDUK**. Remain near the seat to build Kuasa. A counterattack arrives shortly afterward; BASIC can defeat it. If pushed away or knocked out, Kuasa pauses until you sit again.
5. Reach 35 Kuasa to show the prototype result. Tap **GANTI HERO** at any point to compare the four generated greybox silhouettes and names.

Check joystick comfort, legibility of left/right sector markers, gate collision, attack range, camera tracking, text safe area on phone and tablet, and whether a full run can be completed without a restart. A 30–60-second screen recording would be particularly useful for the next iteration.

## Honest scope

This is an **offline solo traversal/combat-loop preview**. It reuses the existing movement, camera, material pipeline and generated hero silhouettes. The guarding enemy is a simple moving damage dummy: no faction organization, hero skills, Dodge, Modal/Koneksi, branching negotiation, cutscenes, saves, co-op, or production character models are implemented. The two sectors use timed presence as temporary stand-ins for unique Majelis and Biro mechanics. Their distinct encounters and a true choice of paths are the next campaign work. The separate original-IP bird monument is an abstract greybox shape, not an official Garuda emblem.

The concept Bible v2 calls for solo-first Jalur Takhta and retains Rebut Kursi as a separate PvP mode. We have deliberately kept the campaign preview on its own generated scene and separate Android package. The prior source branch remains available for testing the 4v4 game.
