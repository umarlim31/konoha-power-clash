# Jalur Takhta 0.0.8.3 — fondasi logika + pipeline hero 3D

Branch kerja: `feat/jalur-takhta-0.0.8.3`, dibuat dari `feat/jalur-takhta-first-playable` (`75ff398`, di atas 0.0.8.2 `b9e5188`). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

**Tujuan versi ini: fondasi.** Gameplay yang terlihat di layar harus sama dengan 0.0.8.2, kecuali label versi dan dua nama tombol skill MEGA di mode PvP. Tidak ada fitur gameplay baru.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**.

## Build manual Android/tablet (Unity Build Automation)

1. Di target Android Unity Build Automation yang sudah ada, pilih branch **`feat/jalur-takhta-0.0.8.3`** (setelah PR di-merge: `feat/jalur-takhta-first-playable`). Unity **6000.0.60f1**.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`** (huruf kecil). Jangan diubah.
3. Jalankan build secara manual. APK: **KONOHA Jalur Takhta Preview**, versi **0.0.8.3**, kode Android **20**, package `com.konoha.powerclash.jalurtakhta`.
4. Label di bawah layar harus berbunyi `JALUR TAKHTA 0.0.8.3  •  SOLO PREVIEW`. Jika masih 0.0.8.2, APK/commit lama yang ter-build atau terpasang.

Jika build gagal, kirim bagian log yang mengandung `error CS` atau `Exception`.

## Perbaikan build #35 (gagal compile)

Build #35 (commit 8c8a0b6) gagal: `HeroAnimatorDriver.cs` memakai `Animator`, tetapi modul bawaan Unity **Animation** tidak tercantum di `Packages/manifest.json` (error CS1069). Perbaikan: `com.unity.modules.animation` ditambahkan. Tidak ada kode game yang berubah; versi tetap **0.0.8.3 / code 20** karena APK 0.0.8.3 belum pernah terbentuk.
`scripts/source-check.py` sekarang menolak source yang memakai tipe dari modul bawaan (Animation, Physics, Audio, dll.) tanpa modul tersebut di manifest.

## Yang berubah

### 1. Lapisan logika murni — `Assets/Konoha/Campaign/Logic/` (namespace `Konoha.Campaign`)
- `CampaignTuning`: semua angka dari GAME_LOGIC §5–§9, dikelompokkan per sistem (`ResourceRules`, `Seals`, `Runtuh`, `Enemy`, `Majelis`, `Biro`, `Garda`, `Memerintah`). Nilai slice sementara 0.0.8.x disimpan terpisah di `CampaignTuning.PreviewSlice` (hold 2,5 dtk, 3× SAHKAN, Garda 100 HP, Kuasa 35, dll.) supaya rasa main tidak berubah.
- `UnitRole` + `UnitRoleStats`: tabel §7 (Wibawa, damage, kecepatan, radius jaga, mengejar/tidak, elite, telegraf).
- `FactionId` + `FactionDefinition`: nama tampil, warna utama/aksen, dan peran dasar. Hanya Majelis Daun, Biro Prosedur, Garda Takhta yang MVP; tiga faksi lain didefinisikan tanpa roster (ditunda, §15).
- `EncounterComposer.Compose(faksi, jumlahPemain)`: komposisi solo §8 dan scaling §11; jumlah pemain di-clamp 1..4. Scaling **menambah unit**, bukan HP. Juga `LoketCount`, `ArsipMaxAlive`, `ReinforcementWaves`.
  - **Asumsi yang perlu dikonfirmasi:** baris tabel §11 dibaca kumulatif (baris 3 sudah termasuk tambahan baris 2). Garda 3 pemain = gelombang bantuan 4 Kroni; 4 pemain = 2 gelombang masing-masing 4 Kroni.
- `CampaignObjectiveDirector`: aturan objective slice (Majelis tahan, Biro 3 langkah, gerbang dalam, Garda, Kursi, Kuasa, satu serangan balik) dipindah dari controller ke kelas C# biasa (tanpa MonoBehaviour). Menerima posisi pemain, input, dan waktu; satu-satunya yang mengubah `CampaignRunState` di preview.

### 2. `CampaignRunState` diperluas (API lama tetap)
- `Checkpoint` (enum `CampaignCheckpoint` 1–5) hanya bisa maju. Majelis/Biro boleh urutan bebas; checkpoint yang lebih jauh dipertahankan.
- `RuntuhCount` + `RecordRuntuh()` (untuk layar hasil nanti).
- `SectorState` per sektor: `Terkunci → Tersedia` saat sampai Plaza, `Berlangsung` saat sektor dimulai, `Selesai` saat segel didapat.
- Belum dipakai untuk respawn: Runtuh di preview tetap kembali ke Gerbang Rakyat seperti 0.0.8.2. Respawn di checkpoint masuk 0.0.9 (§6).

### 3. `CampaignPreviewController` dirapikan
Sekarang hanya penghubung Unity: input tombol, HUD, visual, dan combat mainan Garda (combat asli menggantikannya di 0.0.9). Angka hardcode diganti `CampaignTuning.PreviewSlice`. Teks HUD, urutan kejadian, dan waktu tidak berubah.

### 4. Pipeline hero 3D (disiapkan, belum ada file model)
- Konvensi: `Assets/Konoha/Art/Heroes/<Hero>/<Hero>.fbx` (model + rig) dan `<Hero>@<Clip>.fbx`; hero `Mega`, `Gemoy`, `Abah`, `PakWi`; clip `Idle`, `Run`, `Attack`, `Skill`, `Hit`, `Jump`, `Runtuh`. Nama peka huruf besar/kecil.
- `HeroModelImporter` (AssetPostprocessor, hanya untuk folder itu): rig Humanoid, avatar dari file itu sendiri, tanpa kamera/lampu/collider, material + tekstur embedded di dalam FBX (URP lewat material description), Idle/Run loop, root motion di-bake (karakter tetap digerakkan oleh game).
- `HeroVisualCatalog` (Editor): jika `<Hero>.fbx` ada → visual dibuat dari model; tinggi diskalakan ≈ 1,7 m, kaki di y=0, semua Collider/Camera/Light model dihapus, AnimatorController dibuat di `Assets/Konoha/Generated/Heroes/<Hero>Hero.controller` hanya dengan state yang clip-nya ada (Idle default, Run via float `Speed`, trigger `Attack/Skill/Hit/Jump`, bool `Runtuh`). Jika file tidak ada atau bermasalah → visual primitive lama dipakai, generator hanya menulis peringatan (tidak throw).
- `HeroAnimatorDriver` (runtime, `Assets/Konoha/Character/`): mengisi `Speed` dari gerak horizontal visual (bekerja untuk solo, pemilik PvP, dan pemain remote), menyediakan `PlayAttack/PlaySkill/PlayHit/PlayJump/SetRuntuh`. Aman jika Animator/parameter tidak ada.
- Satu jalur kode: katalog dipakai saat membuat prefab hero PvP di `SpikeProject`; campaign menyalin visual dari prefab itu. Jika hero memakai model 3D, kapsul `Body` (PvP) dan `PlaceholderSilhouette` (campaign) disembunyikan untuk hero itu saja.
- Di campaign, BASIC memicu `PlayAttack`, skill memicu `PlaySkill`, pukulan Garda memicu `PlayHit`. Di PvP, KO memicu `SetRuntuh`. Semua tanpa efek bila hero masih primitive.

### 5. Bersih-bersih
- Material `HeroPrabowoAccent` → `HeroGemoyAccent`, `HeroJokowiAccent` → `HeroPakWiAccent` (warna sama). Enum `PrototypeHero` tidak diubah; komentar menjelaskan bahwa itu nama internal dan UI memakai MEGA/GEMOY/ABAH/PAK WI.
- Label skill MEGA di PvP: `BANTENG CHARGE` → `SERUAN IBU`, `KADER!` → `PERISAI RAKYAT`. Mekanik tidak berubah. (Label skill campaign tidak tersentuh: MEGA tetap `PERISAI`.)
- README root diganti ringkasan status. Workflow `android-spike.yml` ditandai **LEGACY** di field `name:` (tidak dihapus).
- `scripts/source-check.py` diperketat: batas assembly Runtime/Editor/Test, tidak ada API `UnityEditor` di assembly Runtime, tidak ada tipe duplikat, logika campaign bebas MonoBehaviour, versi campaign konsisten (MenuItem = bundleVersion = footer) + catatan versi ada, dan peringatan nama file hero yang salah. Satu pengecualian yang disengaja: file di `Assets/Konoha/Art/` boleh tanpa `.meta` (diunggah dari GitHub web; Unity membuatnya saat import) — dilaporkan sebagai `WARN`, tidak gagal.

### Versi
- Campaign: MenuItem `Konoha/Prepare Jalur Takhta Preview 0.0.8.3`, `bundleVersion` 0.0.8.3, `bundleVersionCode` 20, footer `JALUR TAKHTA 0.0.8.3  •  SOLO PREVIEW`.
- String versi PvP (`SpikeProject`: MenuItem `0.0.6A.2`, `bundleVersion` 0.0.6.2/kode 12, label `0.0.6A.2 • Hero Identity + Greybox Polish`) **sengaja tidak diubah**: itu versi prototipe PvP yang terpisah. Pengaturan PvP selalu ditimpa oleh pengaturan campaign saat `prepare` dijalankan, jadi APK tetap bernomor 0.0.8.3.

## Checklist uji perangkat 0.0.8.3

Semua harus **sama seperti 0.0.8.2**.

1. **Label versi:** bawah layar `JALUR TAKHTA 0.0.8.3  •  SOLO PREVIEW`. Info aplikasi Android: versi 0.0.8.3.
2. **Kamera:** geser layar kanan → orbit 360°; cubit dua jari → zoom; **KAMERA AWAL** mengembalikan sudut dan jarak.
3. **Joystick relatif kamera:** setelah kamera diputar setengah lingkaran, joystick ke atas tetap berjalan "masuk ke layar". Tap tombol tidak memutar kamera.
4. **LOMPAT:** satu lompatan per pendaratan, tidak ada lompatan di udara. Melompat di depan kursi terkunci tidak bisa menembus segel.
5. **Kolam:** masuk kolam dari dua sisi, keluar lewat ramp utara dan selatan **tanpa melompat**.
6. **Jatuh:** jika jatuh ke bawah dunia, hero kembali ke posisi kering terakhir.
7. **Batas oval:** jalan ke tepi taman → hero berhenti/meluncur di batas tak terlihat, tanpa dinding.
8. **LIHAT ARENA / KEMBALI MAIN:** masuk saat bergerak dan setelah memutar kamera; kembali tanpa kontrol macet.
9. **Rute lengkap:** Plaza → Majelis (tahan 2,5 detik, bar penuh) → Biro (SAHKAN 3×) → Garda (BASIC/skill) → DUDUK → bertahan dari serangan balik → Kuasa 35 → ULANG. Pesan dan urutan harus sama seperti 0.0.8.2.
10. **Hero:** GANTI HERO berputar MEGA → GEMOY → ABAH → PAK WI; tampilan primitive dan kapsul sama seperti sebelumnya (belum ada model 3D).
11. **Performa:** tidak ada penurunan FPS/panas dibanding 0.0.8.2.

## Validasi di lingkungan kerja

- `python3 scripts/source-check.py`: PASS (cek statis, bukan compile Unity).
- Logika murni + test logika (`CampaignLogicTests`, `CampaignObjectiveDirectorTests`, `CampaignRunStateTests`) di-compile dengan Mono `mcs` memakai stub kecil `Vector3/Mathf/NUnit` di luar Unity: 17/17 lulus. Ini **bukan** compile Unity.
- Uji diferensial di luar Unity: logika objective 0.0.8.2 (disalin dari `b9e5188`) vs `CampaignObjectiveDirector` pada 3000 run acak (8000 langkah, 2896 run sampai MENANG): **0 perbedaan** state, Kuasa, HP Garda, dan pesan.
- Belum dijalankan: compile Unity, EditMode test di Unity (termasuk scene test dan `HeroVisualCatalogTests`), APK, perangkat.

## Risiko

- API `AnimatorController`/`ModelImporter` di pipeline hero hanya berjalan saat ada FBX; tanpa FBX kodenya tetap harus lolos compile. Jika compile gagal di sana, log UBA akan menunjuk `HeroVisualCatalog.cs` atau `HeroModelImporter.cs`.
- File FBX yang diunggah lewat GitHub web tidak punya `.meta`; GUID-nya dibuat Unity saat build. Tidak masalah karena scene/prefab di-generate ulang tiap build.
- Kualitas model (skala, arah hadap, rig Humanoid valid) baru bisa dinilai di 0.0.8.4 setelah `Mega.fbx` tersedia.

## Berikutnya

0.0.8.4 — pasang MEGA 3D memakai pipeline ini (setelah build 0.0.8.3 OK dan `Mega.fbx`, `Mega@Idle.fbx`, `Mega@Run.fbx` diunggah ke `Assets/Konoha/Art/Heroes/Mega/`).
