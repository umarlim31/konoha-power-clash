# PROMPT CLAUDE CODE — NEGARA KONOHA: POWER CLASH

Cara pakai:
- **Satu prompt = satu sesi Claude Code = satu versi.** Jangan gabungkan dua prompt.
- Salin blok prompt dari garis `=== MULAI ===` sampai `=== SELESAI ===`, lalu tempel ke Claude Code (claude.ai/code, repo `umarlim31/konoha-power-clash`).
- Setelah Claude Code selesai dan membuat PR → **build di Unity Build Automation** (branch yang disebut di laporannya) → uji di tablet → lapor memakai **Template Laporan Build** di bagian bawah.
- **Jangan lanjut ke prompt berikutnya** sebelum build versi sebelumnya berhasil di perangkat.

Urutan versi:
| Versi | Prompt | Isi | Kapan dijalankan |
|---|---|---|---|
| 0.0.8.3 | Prompt 1 | Fondasi logika + pipeline hero 3D + bersih-bersih | ✅ Selesai, lolos di tablet |
| 0.0.9 | Prompt 3 | Solo = host lokal + combat asli di campaign | ✅ Selesai, lolos di tablet (2026-09-29) |
| (menyusul) | Prompt 2 | Pasang MEGA 3D | Kapan saja setelah file FBX MEGA diunggah; nomor versi = versi berikutnya saat itu |
| 0.0.9.1 | Prompt 4 | Tata ulang peta menjadi rute | ✅ Selesai, lolos di tablet (2026-09-29) |
| 0.0.9.2 | Prompt 5 | Faksi Majelis Daun | ✅ Selesai (bersama 0.0.9.2.1), lolos di tablet (2026-09-29) |
| 0.0.9.2.1 | (perbaikan) | Restu Rakyat, kamera stabil, pilih hero di awal | ✅ Selesai, lolos di tablet (2026-09-29) |
| 0.0.9.3 | Prompt 6 | Faksi Biro Prosedur | ✅ Selesai, lolos di tablet (2026-09-29) |
| 0.0.9.4 | (perbaikan) | Kamera tidak tertutup atap, efek suara | ✅ Selesai (bersama 0.1.0) |
| 0.1.0 | Prompt 7 | Garda Takhta + Fase Memerintah + Hasil = MVP | ✅ Terpasang dan dimainkan di tablet (2026-09-30), dengan masukan → 0.1.1 |
| 0.1.1 | (perbaikan) | Majelis selalu muncul, DUDUK = MENANG | Dikerjakan (branch `feat/jalur-takhta-0.1.1`), menunggu uji tablet |
| 0.2.0 | (visual) | Suasana Nusantara (jalan, tiang listrik, umbul-umbul, baliho, warung, sawah, gunung) | ✅ Diuji di tablet (menang 5:33); masukan → 0.2.1 |
| 0.2.1 | (visual) | Material nyata + siap tekstur foto, tanpa hiasan 17-an, kabel dirapikan | ✅ Diuji di tablet; di-merge. Masukan: kabel melintang depan kamera → 0.2.2 |
| 0.2.2 | (visual) | Kota Hidup: jalan lingkar + lalu lintas, warga, air mancur, bendera merah-putih, ruko, rumah kampung, teluk | ✅ Diuji di tablet ("lumayan lebih bagus"); di-merge |
| 0.2.3 | (visual) | Tubuh manusia hero & musuh, efek pukulan (percikan, kilat, jeda, getar), Kerbau Rakyat, efek khas tiap skill | ✅ Diuji di tablet (4 hero, menang 3:27); masukan: PAK WI paling lemah → 0.2.4 |
| 0.2.4 | (balance) | Keseimbangan 4 hero di mode solo (profil solo HeroBalance; PvP tetap) | ✅ Diuji di tablet ("hasilnya udah bagus"); di-merge |
| 0.2.5 | (visual) | Nusantara Megah: ubin motif, spanduk merah-putih, menara, air terjun Istana, flamboyan & bugenvil, langit cerah, teluk-pulau-kota pesisir | ✅ Diuji di tablet; masukan: "upgrade lagi agar lebih terasa Nusantara" → 0.2.6 |
| 0.2.6 | (visual+audio) | Rasa Nusantara: gamelan latar, candi bentar, stupa, gedung lama, padma di plaza, flamboyan & ubin motif dihaluskan | Dikerjakan (branch `feat/nusantara-rasa-0.2.6`), menunggu uji tablet |
| 0.2.2 | (visual) | Karakter + serangan (animasi prosedural, efek benturan, Kerbau Rakyat) | Rencana |

> Catatan: fase "faksi" dari rencana sebelumnya dipecah menjadi 0.0.9.2 dan 0.0.9.3, dan "combat asli" dipisah dari "peta baru". Alasannya: kamu tidak bisa compile sendiri, jadi setiap build harus kecil agar error mudah dilacak.

---

## PERSIAPAN (sekali saja, sebelum Prompt 1)

**Kapan:** sekarang, lewat browser tablet.
1. Buka `github.com/umarlim31/konoha-power-clash` → pindah ke branch **`feat/jalur-takhta-first-playable`**.
2. **Add file → Upload files** → unggah `CLAUDE.md` ke **root** (folder paling atas). Commit.
3. Ulangi: unggah `GAME_LOGIC_JALUR_TAKHTA_v1.md`, `PANDUAN_MEGA_3D.md`, dan file ini (`PROMPTS_CLAUDE_CODE.md`) ke folder **`Docs/`** (ketik `Docs/` di kolom nama). Commit.
4. Buka **claude.ai/code** → hubungkan akun GitHub (jika diminta) → pilih repo `umarlim31/konoha-power-clash`.

---

## PROMPT 1 — 0.0.8.3 "Fondasi Jalur Takhta"

```
=== MULAI ===
Kamu melanjutkan proyek Unity "NEGARA KONOHA: POWER CLASH". Proyek ini BUKAN proyek baru.

WAJIB BACA DULU, sebelum mengubah apa pun:
1. CLAUDE.md (aturan tetap proyek)
2. Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md (design authority MVP)
3. Docs/GAME_BIBLE_V2_IMPLEMENTATION.md
4. Docs/JALUR_TAKHTA_FIRST_PLAYABLE_007.md (catatan 0.0.8.2)

Mulai dari branch `feat/jalur-takhta-first-playable` (HEAD seharusnya b9e5188 = 0.0.8.2 atau commit dokumen di atasnya; verifikasi dengan git). Buat branch baru `feat/jalur-takhta-0.0.8.3`. PR ke `feat/jalur-takhta-first-playable`, BUKAN ke main.

TUJUAN 0.0.8.3: fondasi. Gameplay di layar HARUS SAMA seperti 0.0.8.2 (kecuali label versi). Tidak ada fitur gameplay baru.

LANGKAH 1 — AUDIT (laporkan singkat sebelum coding):
- Git status, branch, 5 commit terakhir.
- Pastikan fitur 0.0.8.2 ada di kode: orbit kamera, pinch zoom, KAMERA AWAL, joystick relatif kamera, LOMPAT, ramp kolam, pemulihan jatuh, batas oval, LIHAT ARENA.
- Petakan alur generator: SpikeProject.prepare → CampaignPreviewProject.Prepare → CampaignCapitalArt.Build.
- Periksa bagaimana pre-export `Konoha.Editor.SpikeProject.prepare` memanggil Prepare campaign (huruf kecil "prepare"). Jangan ubah nama entrypoint itu.

LANGKAH 2 — LAPISAN LOGIKA MURNI (C#, tanpa MonoBehaviour, ada EditMode test):
Buat di Assets/Konoha/Campaign/Logic/ (namespace Konoha.Campaign):
a) CampaignTuning: semua angka dari Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §5–§9 sebagai const/static readonly, dikelompokkan per sistem.
b) UnitRole (enum: Kroni, Guard, Spesialis, Senior, Pemimpin) + UnitRoleStats (Wibawa, damage, kecepatan, radius jaga) sesuai tabel §7.
c) FactionId (enum: MajelisDaun, BiroProsedur, GardaTakhta, KomisiSuara, KonsorsiumModal, MenaraNarasi) + FactionDefinition (nama tampil, warna, daftar peran dasar).
d) EncounterComposer.Compose(FactionId, int playerCount) → IReadOnlyList<UnitSpawn>, sesuai tabel scaling §11. playerCount di-clamp 1..4.
e) Perluas CampaignRunState TANPA merusak API yang dipakai sekarang: checkpoint terakhir (enum CampaignCheckpoint), RuntuhCount, RecordRuntuh(), SectorState per sektor. Semua test lama harus tetap lulus.
f) Tambah EditMode test: komposisi solo per faksi sesuai §8, scaling 2–4 menambah unit (bukan hanya HP), clamp playerCount, checkpoint maju dan tidak mundur, RuntuhCount.

LANGKAH 3 — RAPIKAN CampaignPreviewController (perilaku identik):
- Pindahkan logika objective (Majelis hold, Biro 3 langkah, gerbang, garda, power, counterattack) ke kelas C# murni CampaignObjectiveDirector yang menerima posisi pemain + input dan mengubah CampaignRunState.
- Controller hanya menjadi penghubung Unity (input, HUD, visual).
- Ganti angka hardcode dengan CampaignTuning, TAPI nilai slice sementara (hold 2.5 dtk, 3 sahkan, guard 100 HP, power 35) disimpan sebagai konstanta "PreviewSlice" agar rasa main tidak berubah.

LANGKAH 4 — PIPELINE HERO 3D (disiapkan, belum ada file model):
- Folder konvensi: Assets/Konoha/Art/Heroes/<Hero>/ dengan <Hero>.fbx (model+rig) dan <Hero>@<Clip>.fbx (clip: Idle, Run, Attack, Skill, Hit, Jump, Runtuh). Hero: Mega, Gemoy, Abah, PakWi.
- AssetPostprocessor (Editor) untuk path itu: animationType Humanoid, Idle/Run loop, tanpa import kamera/lampu, material dari tekstur embedded.
- HeroVisualCatalog (Editor): jika <Hero>.fbx ada → buat visual dari model itu. Skala otomatis tinggi bounds ≈ 1.7 m, kaki di y=0, HAPUS semua Collider di model, generate AnimatorController (Assets/Konoha/Generated/...) dengan state yang clip-nya tersedia (Idle default, Run via parameter float Speed, trigger Attack/Skill/Hit/Jump, bool Runtuh). Clip yang tidak ada dilewati tanpa error.
- Jika <Hero>.fbx TIDAK ada → pakai visual primitive yang sekarang, hasil generate identik 0.0.8.2. Generator TIDAK BOLEH throw.
- Runtime: HeroAnimatorDriver (Assets/Konoha/Character/) mengisi Speed dari kecepatan horizontal karakter dan menyediakan method PlayAttack/PlaySkill/PlayHit/SetRuntuh. Aman jika Animator tidak ada.
- Saat visual 3D dipakai, sembunyikan "PlaceholderSilhouette" untuk hero itu, baik di prefab PvP maupun campaign.
- Pakai di dua tempat: prefab hero PvP (SpikeProject) dan campaign. Jangan duplikasi logika; satu jalur kode.

LANGKAH 5 — BERSIH-BERSIH:
- Ganti nama material "HeroPrabowoAccent" → "HeroGemoyAccent", "HeroJokowiAccent" → "HeroPakWiAccent". Enum PrototypeHero JANGAN diubah (beri komentar: nama internal, UI memakai MEGA/GEMOY/ABAH/PAK WI).
- Label skill MEGA "BANTENG\nCHARGE" → "SERUAN\nIBU" dan "KADER!" → "PERISAI\nRAKYAT". Hanya label, mekanik tidak diubah.
- README root: ganti isinya dengan ringkasan status saat ini + tautan ke CLAUDE.md, Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md, dan catatan versi terbaru. Tandai .github/workflows/android-spike.yml sebagai LEGACY di field `name:` (jangan dihapus).

LANGKAH 6 — VERSI & DOKUMEN:
- 0.0.8.3, bundleVersionCode 20, di SEMUA tempat (MenuItem, bundleVersion, footer "JALUR TAKHTA 0.0.8.3 • SOLO PREVIEW"). Cek juga string versi PvP agar konsisten dengan kebiasaan repo (jangan ubah jika memang terpisah; jelaskan).
- Buat Docs/JALUR_TAKHTA_0.0.8.3.md: apa yang berubah, langkah build manual Unity Build Automation, dan checklist uji perangkat (semua fitur 0.0.8.2 + label 0.0.8.3).

VALIDASI:
- python3 scripts/source-check.py harus lulus (perbarui skrip jika perlu mengenali file baru, jangan dilemahkan).
- Cek statis: namespace, using, referensi asmdef (Runtime vs Editor; test asmdef bisa mengakses Logic), tidak ada class duplikat, tidak ada API Editor di assembly Runtime.
- Jangan klaim compile/APK/gameplay PASS. Tulis BELUM DIVERIFIKASI.

COMMIT: 3–5 commit bermakna (logic+tests / refactor controller / hero pipeline / cleanup+versi+docs). Push branch, buat PR.

LAPORAN AKHIR: ikuti format di CLAUDE.md, dalam Bahasa Indonesia sederhana. Tutup dengan instruksi build yang persis: branch mana yang dipilih di Unity Build Automation, label apa yang harus muncul di layar, dan 5 hal yang harus aku cek di tablet.
=== SELESAI ===
```

---

## PROMPT 2 — MEGA 3D (versi menyusul)

**Syarat:** build 0.0.8.3 berhasil di tablet, dan file `Mega.fbx` + minimal `Mega@Idle.fbx` + `Mega@Run.fbx` sudah diunggah ke `Assets/Konoha/Art/Heroes/Mega/` (lihat Docs/PANDUAN_MEGA_3D.md).

```
=== MULAI ===
Baca CLAUDE.md, Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §12, Docs/PANDUAN_MEGA_3D.md, dan Docs/JALUR_TAKHTA_0.0.8.3.md.

Mulai dari `feat/jalur-takhta-first-playable` terbaru (verifikasi git; jika PR versi terakhir belum di-merge, TANYA aku dulu). Hero campaign sudah memakai prefab jaringan yang sama dengan PvP sejak 0.0.9, jadi satu pemasangan model berlaku untuk kedua mode. Buat branch `feat/mega-3d-<versi>`.

TUJUAN: MEGA tampil sebagai model 3D asli di campaign DAN PvP, memakai pipeline HeroVisualCatalog dari 0.0.8.3.

1. Audit file di Assets/Konoha/Art/Heroes/Mega/: daftar file, ukuran, apakah nama sesuai konvensi. Jika nama salah (misal "mega.fbx", "Mega Idle.fbx"), rename via git mv ke konvensi dan catat di laporan.
2. Karena kamu tidak bisa membuka FBX di Unity, periksa yang bisa diperiksa: ukuran file, header FBX (binary/ASCII, versi), dan apakah tekstur embedded atau terpisah. Laporkan risikonya.
3. Pastikan AssetPostprocessor menangani: skala import, Humanoid avatar dari model (clip memakai avatar model Mega.fbx via "Copy From Other Avatar" jika perlu), loop Idle/Run, root motion OFF.
4. Material: pakai shader URP Lit/Simple Lit yang murah untuk Android. Jika tekstur tersedia, set base map. Jangan transparansi.
5. Ring tim/hero marker tetap terlihat di bawah model. Nameplate/Wibawa bar tetap di atas kepala (sesuaikan tinggi dengan bounds model).
6. Hubungkan HeroAnimatorDriver:
   - Campaign: Speed dari gerak, Attack saat BASIC, Skill saat skill, Jump saat LOMPAT, Runtuh saat tumbang.
   - PvP: Speed dari gerak (klien lokal & remote dari posisi yang di-interpolasi), Attack/Skill dari RPC FX yang sudah ada di NetworkHeroKit. Jangan tambah NetworkVariable baru kecuali perlu.
7. Tiga hero lain TETAP primitive.
8. Versi = sub-versi berikutnya dari versi campaign terbaru (cek CampaignPreviewProject.cs), bundleVersionCode +1, footer diperbarui, Docs/JALUR_TAKHTA_<versi>.md dengan checklist visual (siluet dari kamera normal, tidak ada kapsul, kaki menapak, animasi lari/idle, FPS).

Validasi & laporan sesuai CLAUDE.md. Tulis BELUM DIVERIFIKASI untuk tampilan model sampai aku kirim screenshot/video.
=== SELESAI ===
```

---

## PROMPT 3 — 0.0.9 "Solo = Host Lokal + Combat Asli"

```
=== MULAI ===
Baca CLAUDE.md dan Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md (terutama §6, §7, §14).
Mulai dari `feat/jalur-takhta-first-playable` terbaru. Branch baru `feat/jalur-takhta-0.0.9`.

TUJUAN: campaign memakai combat hero yang SAMA dengan PvP. Hapus combat mainan di CampaignPreviewController.

KEPUTUSAN ARSITEKTUR (sudah dikunci di §14): solo = NetworkManager.StartHost() lokal tanpa klien, offline.

1. Audit NetworkPlayerCombat, NetworkHeroKit, NetworkBotController, NetworkMatchManager, RuntimeNetworkingProof. Petakan apa yang bergantung pada NetworkMatchManager (mode PvP) dan harus diabstraksi agar campaign tidak memakai state PvP (skor tim, PowerToWin, timer match).
2. CampaignPreviewProject: JANGAN lagi menghapus networking. Gunakan player prefab network dengan hero kit. Scene campaign auto-StartHost saat dibuka. Tombol Host/Client PvP tidak muncul di campaign.
3. Buat CampaignDirector (NetworkBehaviour, logika hanya jalan di server) yang memegang CampaignRunState + CampaignObjectiveDirector. HUD campaign membaca state lewat NetworkVariable/RPC ringan.
4. Musuh PvE: konfigurasikan bot yang ada menjadi PveEnemy dengan UnitRole (stats dari CampaignTuning), tim Sistem, target = pemain terdekat, radius jaga untuk Guard. Tanpa menyentuh perilaku bot PvP.
5. Gerbang Rakyat: 3 Kroni sebagai encounter tutorial. Garda Takhta slice lama diganti 1 Guard-role + 1 Pemimpin-role memakai combat asli (mekanik khas faksi BELUM).
6. Runtuh campaign sesuai §6: respawn di checkpoint, Pengaruh −30%.
7. Pertahankan semua fitur 0.0.8.2 (kamera, lompat, kolam, jatuh, batas oval). HUD kontrol campaign sekarang: joystick, BASIC, S1, S2, ULT, DODGE, LOMPAT, DUDUK/SAHKAN kontekstual, KAMERA AWAL. Susun ulang agar tidak menumpuk di tablet (layout mengikuti SafeAreaLayout).
8. Mode PvP harus tetap di-generate dan dimainkan identik. Tulis test/cek statis yang menunjukkan jalur PvP tidak berubah.
9. Versi 0.0.9, code 22. Docs/JALUR_TAKHTA_0.0.9.md dengan checklist: bisa pukul Kroni dengan BASIC/S1/S2/ULT, Runtuh → checkpoint, main offline (mode pesawat), PvP masih jalan.

Jika perubahan terlalu besar untuk satu PR yang aman, BERHENTI setelah audit dan usulkan pemecahan menjadi 0.0.9a/0.0.9b sebelum coding.
Validasi & laporan sesuai CLAUDE.md.
=== SELESAI ===
```

---

## PROMPT 4 — 0.0.9.1 "Peta Rute Kompleks Kekuasaan"

```
=== MULAI ===
Baca CLAUDE.md, Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §3, §13, dan concept art yang dijelaskan di sana.
Mulai dari 0.0.9 terbaru. Branch `feat/jalur-takhta-0.0.9.1`.

TUJUAN: ubah peta dari satu plaza menjadi rute sesuai diagram §13. Kursi terlihat dari spawn tapi jauh.

1. Tata ulang CampaignCapitalArt menjadi sektor-sektor terpisah dengan fungsi builder per sektor (BuildGerbangRakyat, BuildPlazaAspirasi, BuildMajelisDaun, BuildBiroProsedur, BuildGerbangDalam, BuildPlazaTakhta). Pakai ulang aset/material yang sudah ada; material dibagi bersama.
2. Kursi di dais tinggi di ujung utara. Dari kamera default di spawn, Kursi + Istana harus terlihat sebagai landmark. Monumen burung Konoha di plaza tidak boleh menutup garis pandang itu (tulis test geometri sederhana jika bisa: raycast dari posisi kamera default ke Kursi tidak kena monumen).
3. Gerbang Dalam fisik terkunci (collider) sampai CampaignDirector membukanya berdasarkan state.
4. Papan nama besar per sektor + warna faksi (§8). Signage teks fiktif saja.
5. Batas oval diperluas sesuai ukuran baru; ramp/jalur keluar air tetap ada di setiap kolam.
6. Waypoint/arah di HUD menunjuk sektor aktif.
7. Performa Android: laporkan perkiraan jumlah renderer & material sebelum/sesudah. Gabungkan mesh statis per sektor jika jumlahnya melonjak.
8. Versi 0.0.9.1, code 23, dokumen + checklist (jalan dari spawn ke Istana, Kursi terlihat dari awal, gerbang terkunci, FPS).
Validasi & laporan sesuai CLAUDE.md.
=== SELESAI ===
```

---

## PROMPT 5 — 0.0.9.2 "Faksi Majelis Daun"

```
=== MULAI ===
Baca CLAUDE.md dan Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §7, §8.1, §11.
Mulai dari 0.0.9.1 terbaru. Branch `feat/majelis-daun-0.0.9.2`.

Implementasikan encounter MAJELIS DAUN persis sesuai §8.1: komposisi dari EncounterComposer, VOTING BLOCK (pengurangan damage 40% selama ≥2 Senior hidup, ring burgundy), notifikasi "BLOK MAJELIS PECAH!", serangan KETOK PALU dengan telegraf, Kroni menyerah saat Ketua tumbang, hadiah SEGEL MAJELIS + Pengaruh.
- Logika di server (SectorEncounter), angka dari CampaignTuning, EditMode test untuk aturan block (unit logic dipisah dari MonoBehaviour).
- Encounter mulai saat pemain masuk sektor; checkpoint 3 tercatat.
- Nameplate peran + bar Wibawa untuk Senior/Ketua. Telegraf terbaca di tablet.
- Visual unit: primitive berwarna faksi dengan siluet peran berbeda (Ketua lebih besar + ornamen). Jangan menunggu model 3D.
- Hapus objective "tahan lingkaran" lama untuk Majelis.
- Versi 0.0.9.2, code 24, dokumen + checklist.
Validasi & laporan sesuai CLAUDE.md.
=== SELESAI ===
```

---

## PROMPT 6 — 0.0.9.3 "Faksi Biro Prosedur"

```
=== MULAI ===
Baca CLAUDE.md dan Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §8.2, §11.
Mulai dari 0.0.9.2 terbaru. Branch `feat/biro-prosedur-0.0.9.3`.

Implementasikan encounter BIRO PROSEDUR persis sesuai §8.2: 3 LOKET dengan progres (berhenti jika ada musuh di zona, turun jika ditinggal), STEMPEL TUNDA (cooldown +30% dalam radius Pengawas, ikon debuff di HUD), respawn Petugas Arsip tiap 12 dtk maks 2, pintu Kepala Biro terbuka setelah 3 loket, serangan SALAH LOKET dengan telegraf, hadiah SEGEL BIRO + Pengaruh.
- Pengubah cooldown harus lewat satu titik di hero kit (modifier), bukan hack per-skill. PvP tidak terpengaruh.
- Hapus objective "SAHKAN 3x" lama; tombol SAHKAN tidak diperlukan lagi (loket berbasis zona).
- Majelis dan Biro bisa dikerjakan dalam urutan bebas. Gerbang Dalam terbuka saat 2 segel.
- Versi 0.0.9.3, code 25, dokumen + checklist.
Validasi & laporan sesuai CLAUDE.md.
=== SELESAI ===
```

---

## PROMPT 7 — 0.1.0 "MVP Jalur Takhta"

```
=== MULAI ===
Baca CLAUDE.md dan Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md §8.3, §9, §10, §16.
Mulai dari 0.0.9.3 terbaru. Branch `feat/jalur-takhta-mvp-0.1.0`.

1. GARDA TAKHTA sesuai §8.3: LOCKDOWN ring, COUNTER PUSH bertelegraf, bala bantuan saat Panglima < 50%, KURSI TERBUKA setelah Panglima tumbang.
2. FASE MEMERINTAH sesuai §9: duduk, Power +2/dtk ke 100, serangan balik tiap 15 dtk, Power berhenti saat keluar radius, KUDETA setelah 3 Runtuh.
3. LAYAR HASIL sesuai §10 (waktu, Runtuh, Pengaruh, arketipe TAKHTA BESI, tombol ULANG / GANTI HERO).
4. Menu mode sederhana: JALUR TAKHTA (solo) dan REBUT KURSI (PvP). Pemilihan hero sebelum run.
5. Audit ulang seluruh campaign terhadap checklist §16 dan laporkan mana yang terpenuhi di kode vs yang harus aku uji.
6. Versi 0.1.0, code 26. Docs/JALUR_TAKHTA_0.1.0_MVP.md berisi skenario uji lengkap dari spawn sampai MENANG (target 8–15 menit).
Validasi & laporan sesuai CLAUDE.md.
=== SELESAI ===
```

---

## TEMPLATE LAPORAN BUILD (kirim ke Claude Code setelah tiap build)

```
=== MULAI ===
Hasil build versi: [0.0.x.x]
Branch yang di-build: [nama branch]
Status Unity Build Automation: [BERHASIL / GAGAL]
Jika GAGAL: [tempel bagian error dari log, cari baris yang mengandung "error CS" atau "Exception"]
Label di layar game: [tulis persis teks footer]
Perangkat: Infinix XPad 20

Hasil uji (tulis OK / MASALAH + penjelasan):
1. ...
2. ...
3. ...

Screenshot/video: [lampirkan jika ada]

Tugas: perbaiki masalah di atas di branch yang sama, naikkan versi kecil (mis. 0.0.8.3 → 0.0.8.3.1) HANYA jika perlu build ulang untuk diuji. Laporan sesuai CLAUDE.md.
=== SELESAI ===
```
