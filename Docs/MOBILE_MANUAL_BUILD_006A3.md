# Manual build from Android or tablet — hero concept alignment

This branch is built with the existing Unity Build Automation target. The owner does **not** need to run a local Unity Editor. The branch is `feat/hero-concept-alignment-006a3`, based on `feature/v0.0.6a2-hero-greybox-polish`.

1. In the Unity Build Automation dashboard on a mobile browser, select the existing Konoha Android target and point its source branch at `feat/hero-concept-alignment-006a3`. Keep Unity **6000.0.60f1** and the Android platform configuration already used for 0.0.6A.2.
2. Keep the configured pre-export method **`Konoha.Editor.SpikeProject.prepare`**. It regenerates the networked scene from source. Launch a **manual build**; do not use the obsolete GitHub Actions 0.0.1 instructions in the root README.
3. When the job completes, download its Android artifact on the phone/tablet and install it. Check its visible footer says **`0.0.6A.2 • Hero Identity + Greybox Polish`**; the revision label has intentionally not been promoted before testing.
4. If the target runs EditMode tests, check `CampaignRunStateTests`; otherwise the build alone verifies import/compilation but does not run the tests. Host a match and add bots. Cycle each hero; verify **MEGA / GEMOY / ABAH / PAK WI** in HUD and nameplates; inspect each hero's dark, light and accent parts, its team ring, and its appearance after Runtuh and recovery.
5. Record the build outcome and one gameplay screenshot or short video. If a build fails, collect the full build log and identify its failing step before changing source.

This build contains the **Rebut Kursi** 4v4 prototype plus a tested campaign route-state foundation. The Bible v2 **Jalur Takhta** campaign does not yet have a playable scene or encounter loop in this APK.
