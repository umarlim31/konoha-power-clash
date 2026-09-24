# KONOHA: POWER CLASH - Android Technical Spike 0.1

> **Current development is on feature branches.** `main` and much of this README describe the old 0.0.1 spike. The 4v4 prototype lives on `feature/v0.0.6a2-hero-greybox-polish`; character concept alignment is on `feat/hero-concept-alignment-006a3`. For manual Unity Build Automation from a phone/tablet, read [Docs/MOBILE_MANUAL_BUILD_006A3.md](Docs/MOBILE_MANUAL_BUILD_006A3.md). The new solo-first PvE campaign is scoped separately in [Docs/GAME_BIBLE_V2_IMPLEMENTATION.md](Docs/GAME_BIBLE_V2_IMPLEMENTATION.md).

Current build: **0.0.1**. Status: **PARTIAL - source prepared; Unity compilation and device validation pending**.
Engine: **CANDIDATE LOCK**, not Final Freeze.

This repository contains one offline movement greybox. It does not prove multiplayer combat.
Do not start 0.0.2 until the real-device 0.0.1 gate is recorded and the owner approves continuation.

## Start here from a phone or tablet

1. Place this source in a private Git repository. A `repository.bundle` accompanies the delivery to preserve the initial Git history; see `docs/CLOUD_BUILD.md`.
2. Provision a remote Linux build machine with Unity **6000.0.60f1**, valid license, and the matching Android Build Support, SDK, NDK and JDK. No local Windows PC is required.
3. Register its GitHub Actions runner using the labels and repository variable in `docs/CLOUD_BUILD.md`.
4. From GitHub in the mobile browser: Actions -> KONOHA Android 0.0.1 -> Run workflow.
5. Download the successful workflow artifact, extract `KONOHA_0.0.1.apk` on Android, install, and complete `docs/DEVICE_GATE.md`.

**This delivery contains source, not an APK. The runner has not been provisioned.** No credentials or cloud account were supplied.

## Remote editor

Open this folder in the pinned Unity Editor. Run `Konoha > Prepare Build 0.0.1 (regenerates greybox)`.
This generates the scene, URP renderer/pipeline, materials and locomotion asset, configures Android, and opens SpikeArena.
Enter Play and drag the on-screen joystick to inspect. This mouse interaction is editor-only touch emulation, not a PC gameplay target.

The generated scene lives under `Assets/Konoha/Generated`; edit `SpikeProject.cs` for reproducible arena changes.
Preparation regenerates the greybox scene; it is not intended to preserve hand edits to that generated scene.

## Architecture

| Module | Responsibility |
| --- | --- |
| Input | Touch pointer ownership, dead-zone handling, normalized MoveIntent |
| Data | One candidate locomotion tuning asset |
| Character | Collision motor with caller-supplied timestep; separate follow camera |
| Core | Offline-only 0.0.1 composition driver |
| UI | Safe-area and tablet-aspect grip layout |
| Debug | Build identity and measured frame interval/FPS; unavailable systems explicitly N/A |
| Editor | Reproducible scene/configuration and strict Android build entrypoint |
| Tests | Eight Unity EditMode tests plus build artifact validators |

`OfflineSpikeDriver` owns local movement only in this isolated build. It must be removed/replaced as the gameplay driver when networking begins. `CharacterMotor` is not a deterministic rollback motor and has not been validated for network prediction. Its separation prevents touch or camera code from becoming server authority.

Only joystick and DEBUG are interactive. Combat controls, hero kits, data-driven AbilityDefinition/HeroState, networking, prediction, Dodge, Ring-Out and objectives are deferred to their authorized gates.

## Reproducibility limits

Direct dependency versions are pinned. Unity must perform the first package resolution; `packages-lock.json`, generated settings, assets and resolved package configuration must be reviewed from the CI artifact and then committed where appropriate. No fabricated lockfile or imported scene is claimed here.
All supplied script/folder `.meta` GUIDs are stable. CI archives generated assets/settings for provenance.

## Verification

Run `python3 scripts/source-check.py` for source structure checks.
Run `bash scripts/android-build.sh` only on the configured licensed remote machine from a clean Git checkout.
See `docs/BUILD_REPORT_0.0.1.md` for evidence and remaining gates.
