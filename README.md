# NEGARA KONOHA: POWER CLASH

Game aksi mobile (Android, Unity **6000.0.60f1**, URP) berlatar **Negara Konoha** — dunia, institusi, dan tokohnya fiksi dan satir. Hero di UI: MEGA, GEMOY, ABAH, PAK WI.

## Status saat ini

| Mode | Status |
|---|---|
| **JALUR TAKHTA** (solo campaign) | **0.2.0** — suasana Nusantara (jalan, tiang listrik, umbul-umbul, baliho satir, warung, sawah, gunung); DUDUK di Kursi = MENANG, menu mode (Solo / PvP), layar hasil, Garda Takhta dengan LOCKDOWN dan DORONGAN BALIK, Fase Memerintah dengan serangan balik dan KUDETA — peta rute: Gerbang Rakyat → Plaza Aspirasi (Majelis/Biro di sayap) → Gerbang Dalam terkunci → Garda Takhta → Kursi di teras Istana. Pilih hero di awal (terkunci per perjalanan), RESTU RAKYAT dari Gerbang Rakyat, faksi **Majelis Daun** (Blok Majelis, Senior, Ketua dengan KETOK PALU) dan **Biro Prosedur** (3 loket, antrian, Stempel Tunda, Kepala Biro dengan SALAH LOKET). Efek suara sintetis dan kamera yang tidak tertutup atap. Combat hero asli (host Netcode lokal), Runtuh → checkpoint. |
| **REBUT KURSI** (PvP 4v4) | Prototipe jaringan (Netcode for GameObjects) tetap di-generate dari kode yang sama dan tidak diubah perilakunya. |
| **BENTUK KOALISI** (co-op 2–4) | Direncanakan setelah MVP. |

Semua status compile/APK/gameplay mengikuti laporan build owner. Selama belum dilaporkan: **BELUM DIVERIFIKASI**.

## Baca dulu

1. [CLAUDE.md](CLAUDE.md) — aturan tetap proyek (branch, build, keputusan terkunci, format laporan).
2. [Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md](Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md) — design authority MVP (aturan, angka awal, faksi, arsitektur).
3. [Docs/JALUR_TAKHTA_0.2.0.md](Docs/JALUR_TAKHTA_0.2.0.md) — catatan versi terbaru, langkah build, dan checklist uji perangkat.
4. [Docs/PROMPTS_CLAUDE_CODE.md](Docs/PROMPTS_CLAUDE_CODE.md) — urutan versi berikutnya.
5. [Docs/PANDUAN_MEGA_3D.md](Docs/PANDUAN_MEGA_3D.md) — cara menyiapkan dan mengunggah model hero 3D.

## Cara build (ringkas)

- Build **hanya** lewat **Unity Build Automation**, dipicu manual oleh owner.
- Pre-export method: `Konoha.Editor.SpikeProject.prepare` (huruf kecil). Method ini men-generate scene, material, mesh, dan prefab dari kode di `Assets/Konoha/Editor/`.
- Scene/prefab/material **tidak** di-commit; sumber kebenaran visual adalah generator, bukan `Assets/Konoha/Generated/`.
- Workflow GitHub Actions `android-spike.yml` adalah sisa 0.0.1 (**LEGACY**) dan tidak dipakai.

## Struktur kode

| Folder | Isi |
|---|---|
| `Assets/Konoha/Campaign/Logic/` | Logika campaign C# murni: `CampaignTuning`, `UnitRoleStats`, `FactionDefinition`, `EncounterComposer`, `CampaignObjectiveDirector` |
| `Assets/Konoha/Campaign/` | `CampaignRunState` (state rute) + komponen Unity solo (kamera, traversal, controller preview) |
| `Assets/Konoha/Networking/` | Mode PvP 4v4 (Netcode) |
| `Assets/Konoha/Character/` | Motor karakter, kamera, `HeroAnimatorDriver` |
| `Assets/Konoha/Editor/` | Generator scene/prefab, seni ibu kota, pipeline hero 3D (`HeroVisualCatalog`, `HeroModelImporter`) |
| `Assets/Konoha/Art/Heroes/<Hero>/` | Model hero dari owner (`<Hero>.fbx`, `<Hero>@<Clip>.fbx`) |
| `Assets/Konoha/Tests/EditMode/` | EditMode test |

Cek statis sebelum commit: `python3 scripts/source-check.py` (bukan pengganti compile Unity).

Dokumen 0.0.1 lama (runner GitHub Actions, gate perangkat awal) tetap ada di `docs/` sebagai arsip.
