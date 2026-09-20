# Real Android gate - 0.0.1

Current result: **NOT RUN**. Fill this after an actual APK exists.

Record tester/date, device model, Android version, RAM, screen resolution/aspect, phone/tablet, APK SHA256, BUILD, COMMIT, BALANCE and SERVER from the HUD. SERVER should read N/A/OFFLINE at this build.

| Test | Procedure | PASS requirement | Result |
| --- | --- | --- | --- |
| Installation | Install APK and launch twice | Arena, character and UI appear; no crash/black/pink scene | NOT RUN |
| Cardinal movement | Drag each direction, release | Matching movement; no drift after release | NOT RUN |
| Diagonal/analog | Partial and full drag, including diagonal | Analog control; diagonal no faster than full cardinal | NOT RUN |
| Collision | Walk at both blocks and all four perimeter walls for 10s each | No traversal or falling through geometry; slide along wall | NOT RUN |
| Camera | Navigate entire arena and corners | Hero remains readable; follows without camera thumb | NOT RUN |
| Multitouch | Hold joystick and tap DEBUG with other finger | Movement remains controlled; second finger cannot steal/release it | NOT RUN |
| Interruptions | While dragging: background, lock/unlock, notification; return | Hero stops; old touch does not resume; fresh touch works | NOT RUN |
| Landscape | Rotate between both supported landscape orientations | Safe area and input remain aligned; no blocked touch target | NOT RUN |
| Phone/tablet | Test narrow phone and 4:3/16:10 tablet where available | Joystick close to thumb grip; no cutout overlap or cropped text | NOT RUN |
| Diagnostics | Toggle HUD twice | Frame interval/FPS update; correct provenance; unavailable metrics N/A | NOT RUN |
| Short stability | Navigate for 5 minutes | No crash, stuck movement or increasing obvious stutter | NOT RUN |

Attach a short screen recording showing movement, release, wall contact, camera, DEBUG and build identity. Note FPS/frame interval at start and end and every visible issue. This is not GPU timing or thermal evidence.

Build 0.0.1 may pass its required phone gate once actual evidence is recorded; tablet coverage must be reported independently, not assumed. No PASS can be inferred from EditMode tests, APK structure or screenshots of source.

Thermal endurance, 150ms network tests, server agreement and 2-device combat are NOT TESTED and belong to later builds.
Even after device PASS, stop and get owner review before implementing 0.0.2.
