# Build 0.0.1 without a personal development PC

## Required infrastructure - not yet provisioned

Use a private repository and a dedicated Linux x64 cloud VM/remote computer. It needs a valid Unity activation for the operating user, Unity 6000.0.60f1, Android Build Support including the bundled SDK/NDK/OpenJDK, Git, Python 3, Bash and outbound package access. Size the VM after measuring the first build; do not assume the phone runs Unity Editor.

This workflow uses a self-hosted Actions runner on that remote machine, not a player-hosted match server. It builds Android only. The Linux dedicated **match server** starts in 0.0.2 and is not implemented here.

1. Restore Git history with `git clone repository.bundle KONOHA` on the remote shell, or upload/import the source directory to a new repository. Do not put the bundle inside the source repo.
2. Configure `origin` to the chosen private Git repository and push `main`. No remote URL has been invented or configured.
3. Install/activate the exact editor and its Android modules through the remote environment. License credentials stay on the runner or in the service's secret store, never in Git or chat.
4. Register a GitHub Actions runner for that repo. Labels: `self-hosted`, `Linux`, `X64`, `konoha-unity-6000-0-60`.
5. Set repository Actions variable `UNITY_EDITOR` to the real absolute Unity executable path on that VM.
6. From a phone/tablet browser, trigger `KONOHA Android 0.0.1` under Actions. This workflow has no untrusted pull-request trigger.
7. Download the APK artifact after success; it contains the APK, identity JSON, SHA256 checksum and resolved settings archive. Download the separate logs artifact if any step fails.

No cloud machine has been purchased/allocated and no GitHub upload has occurred in this session. The pipeline is prepared, not remotely executed.

## Pipeline stages

Clean Git checkout -> source packaging validation -> Unity batch import/configuration/scene generation -> separate Editor process runs eight EditMode tests -> fail-closed test report validator -> Development APK build -> APK container sanity -> checksum -> archive resolved assets/settings.

Preparation and build use `-buildTarget Android`. The separate processes allow imported project settings to be applied before tests/build. Tests omit `-quit` so the test runner can finish itself. Build provenance is stamped from `git rev-parse HEAD`; dirty sources are rejected before Unity is started.

On first successful Unity import, review and commit generated `ProjectSettings` and `Packages/packages-lock.json` in a follow-up technical change. Generated arena/assets are recreated; their actual resolved versions are archived per build. This bootstrap archive is not a fully resolved Unity export yet.

## Android candidate settings

| Setting | Candidate |
| --- | --- |
| Unity | 6000.0.60f1, pinned for reproducibility, not claimed latest |
| URP | 17.0.3 |
| Backend | IL2CPP |
| Architecture | ARM64 |
| Minimum API | 26; provisional spike install floor, not final device support policy |
| Target API | Highest installed in bundled toolchain; APK sideload spike only |
| Graphics | OpenGLES3, linear color, no HDR, 2x MSAA, no shadows |
| Orientation | Landscape left/right |
| Identity | com.konoha.powerclash.spike / 0.0.1 / versionCode 1 |
| Build | Development, strict errors, debug signing, APK |

The pinned editor documents a GameActivity Development APK issue. The generator explicitly selects Activity. This workaround still needs actual build/device validation; it does not prove absence of other editor issues. Before distribution beyond this spike, review an engine patch update as a TECHNICAL change and rerun all gates.

Debug signing is for this prototype only. Use one stable remote build user's debug keystore so later APK updates share a signing identity. Recreating that keystore may require uninstall/reinstall on test devices. No Play Store or production signing is configured.

## Evidence sources for implementation APIs

- [Unity 6000.0.60f1 release notes](https://unity.com/releases/editor/whats-new/6000.0.60f1)
- [Unity Android build process](https://docs.unity3d.com/6000.0/Documentation/Manual/android-BuildProcess.html)
- [BuildPipeline.BuildPlayer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html)
- [URP asset API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html)
- [Android application entry](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AndroidApplicationEntry.html)

These documents support the chosen APIs, not successful compilation or device performance.
