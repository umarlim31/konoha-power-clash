# KONOHA BUILD REPORT

**BUILD:** KONOHA 0.0.1 / Android Technical Spike 0.1  
**STATUS:** PARTIAL - SOURCE PREPARED; UNITY BUILD AND DEVICE VALIDATION PENDING  
**Date:** 2026-09-19

This is a source/bootstrap deliverable, not a compiled Unity export or APK. It must not be reported as Android PASS or implementation validated. No visual runtime review was possible without Unity.

## IMPLEMENTED

- Pinned Unity 6 project bootstrap and URP dependency, editor-generated pipeline, renderer, materials and Spike Arena.
- Temporary capsule silhouette with facing marker; collidable perimeter and two navigation obstacles.
- Analog touch joystick with dead zone, single pointer ownership, multitouch isolation and lifecycle reset.
- Normalized movement intent, data-driven test locomotion and CharacterController motor separated from input.
- Elevated fixed-azimuth follow camera with smoothing and resume recentering.
- Landscape phone/tablet-aspect safe-area layout with edge-anchored controls.
- Toggleable development HUD: measured FPS/frame interval, position, grounding and build/commit/balance/server identity. Unimplemented gameplay/network fields explicitly unavailable. Diagnostics hidden in non-development players.
- Android ARM64 IL2CPP APK configuration, strict build method, full Git SHA stamping, checksum/artifact checks.
- Remote Linux GitHub Actions workflow and phone/tablet handoff guide; infrastructure not provisioned.
- Eight Unity EditMode tests prepared; static validators and real-device checklist.
- Local Git repository and source-history bundle in the delivery archive; no remote push.

## FILES CREATED

| Files | Responsibility |
| --- | --- |
| Assets/Konoha/Data/LocomotionDefinition.cs | Candidate tuning |
| Assets/Konoha/Input/MoveIntent.cs, TouchJoystick.cs | Intent and touch |
| Assets/Konoha/Character/CharacterMotor.cs, MobileCombatCamera.cs | Motor and camera |
| Assets/Konoha/Core/OfflineSpikeDriver.cs | Isolated local movement test driver |
| Assets/Konoha/UI/SafeAreaLayout.cs | Layout adaptation |
| Assets/Konoha/Debug/BuildIdentity.cs, SpikeDebugHud.cs | Provenance/diagnostics |
| Assets/Konoha/Editor/SpikeProject.cs, AndroidBuild.cs | Configuration, scene generator, build |
| Assets/Konoha/Tests/EditMode/SpikeFoundationTests.cs | Eight pending Unity tests |
| Three assembly definitions and associated .meta files | Runtime/editor/test boundaries and stable identities |
| Packages/manifest.json, ProjectSettings/ProjectVersion.txt | Pinned direct tool/dependency versions |
| scripts/android-build.sh, source-check.py, verify-test-results.py, verify-apk.py | Build and verification |
| .github/workflows/android-spike.yml, .gitignore | Remote workflow and source hygiene |
| README.md, docs/* | Audit, source hashes, change classification, evidence and instructions |

Generated .unity/.asset/.mat files are produced by the first Unity Prepare execution; they are not claimed to exist yet. No combat/hero assets generated.

## FILES MODIFIED

None from an existing project. Supplied PDFs remain unchanged.

## TESTS / PASSING

- Source structure, JSON manifests, assembly boundaries, unique GUID coverage and Python syntax: PASS.
- Bash syntax: PASS.
- GitHub workflow YAML parsing/manual-dispatch shape: PASS.
- Five synthetic test-report validator fixtures: PASS (accept pass; reject empty, failure, skipped, malformed).
- Two synthetic APK-container validator fixtures: PASS (accept expected structure; reject missing ARM64 payload).

These seven fixture checks test our validators only. They do not represent executed Unity tests or an actual APK. Exact record: `STATIC_VERIFICATION.txt`.

## FAILING / BLOCKED

No failing executed static checks remain. Unity import, C# compilation, eight EditMode tests, APK generation and device gate are **NOT RUN / BLOCKED**, not passed. No Unity Editor, Android toolchain, licensed cloud runner or device access exists in the current session.

## KNOWN ISSUES

1. First Unity import must resolve packages and confirm API compatibility. No packages-lock.json yet.
2. Generated scene/rendering and UI have not been viewed in Unity or on hardware.
3. The eight tests themselves await first Unity execution; a passing source audit cannot validate them.
4. Phone/tablet grip dimensions use aspect-based candidate layouts; real thumb reach and reported safe areas need device evidence.
5. Camera obstruction handling is limited to the low greybox fixture. No advanced combat framing/target awareness.
6. `CharacterController` is not guaranteed deterministic across machines. Prediction/reconciliation requires actual engineering and testing in later gates.
7. Only local pause/focus input reset exists. This is not network reconnection, grace or bot takeover.
8. The scene has enclosing movement-test walls; Ring-Out is deliberately absent until 0.0.7.
9. Engine pin is a candidate. Its documented GameActivity development-build issue is addressed in configuration by selecting Activity; no runtime validation of that choice yet.
10. Debug signer belongs to the remote build user; persistence needed for upgrade installs.

## ANDROID PERFORMANCE

NOT MEASURED. Target 60 FPS is configured; observed device FPS, GPU timing, memory, battery and thermal behavior are unknown. No LOW/STANDARD/HIGH tier or thermal pass claims. Frame interval shown in HUD is wall-clock frame pacing, not GPU execution time.

## NETWORK

NOT IMPLEMENTED / NOT TESTED, per 0.0.1 scope. No dedicated server, NGO/Transport configuration, ping measurement, simulated latency or two-device evidence. The offline driver must not be carried forward as competitive client authority.

## ARCHITECTURE NOTES

Input -> MoveIntent -> explicitly offline driver -> motor. Data, presentation and editor tooling are separate. Replace the driver with authoritative networking composition at later gates. Avoid speculative combat/state/objective abstractions before their requirements are actionable.

## RULE COMPLIANCE

- Only earliest incomplete build implemented in source.
- One temporary character, no full hero kits, no product layer and no PC/Web build target.
- No new damage, CC, Dodge, Ring-Out, Seat, POWER or victory behavior invented.
- Gameplay precedence conflicts and future blocked ambiguities recorded in the audit.
- No fake network metrics or test PASS evidence.
- Change types: TECHNICAL, PRESENTATION and candidate locomotion BALANCE; no DESIGN change.
- Unity stays ENGINE CANDIDATE LOCK.

## NEXT BUILD

**Remain at 0.0.1.** Provision/connect the remote licensed Unity build environment, run import/tests/build, resolve actual compilation/runtime issues, install on Android and collect device evidence. After the required device gate passes and owner review approves, 0.0.2 can begin. STOP here; do not implement it automatically.
