# CLAUDE.md — NEGARA KONOHA: POWER CLASH

File ini dibaca otomatis oleh Claude Code di setiap sesi. Isinya aturan tetap proyek.
Owner bekerja HANYA dari HP/tablet Android (tanpa PC, tanpa Unity Editor lokal).

## Identitas proyek
- Unity **6000.0.60f1**, target **Android**, URP.
- Branch dasar: `feat/jalur-takhta-first-playable`. Setiap versi dikerjakan di branch turunan sendiri, PR kembali ke branch dasar. JANGAN merge ke `main` tanpa persetujuan owner.
- Build HANYA lewat **Unity Build Automation** (manual, dipicu owner dari browser).
  Pre-export method: `Konoha.Editor.SpikeProject.prepare`.
- Workflow GitHub Actions `android-spike.yml` dan README root = sisa 0.0.1, **usang**. Jangan pakai sebagai acuan build.

## Sumber kebenaran (urutan prioritas)
1. Kode di repo ini (kondisi nyata).
2. `Docs/GAME_LOGIC_JALUR_TAKHTA_v2.md` = **design authority alur 0.4.x "Musim Pemilu"** (dikunci owner 2026-10-02); `Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md` tetap berlaku untuk combat, angka musuh, Garda, dan arsitektur yang tidak diubah v2.
3. `Docs/GAME_BIBLE_V2_IMPLEMENTATION.md` + Game Concept Bible v2.0 (arah produk jangka panjang).
4. Catatan versi di `Docs/JALUR_TAKHTA_*.md`.
5. Screenshot & deskripsi lama.
Urutan versi & tugas tiap versi: `Docs/PROMPTS_CLAUDE_CODE.md`. Kerjakan HANYA versi yang diminta di sesi ini.

## Keputusan terkunci
- Solo campaign = **Netcode host lokal** (StartHost offline). Tidak ada sistem combat kedua.
- Faksi MVP: Majelis Daun, Biro Prosedur, Garda Takhta. Modal/Koneksi & faksi lain ditunda.
- Hero 3D: model FBX + rig Humanoid di `Assets/Konoha/Art/Heroes/<Hero>/<Hero>.fbx`,
  animasi `<Hero>@<Clip>.fbx` (Idle, Run, Attack, Skill, Hit, Jump, Runtuh). Jika file tidak ada → fallback primitive, generator tidak boleh error.
- Aset 3D di-commit manual oleh owner lewat GitHub web (maks 25 MB/file). Jangan hapus/regenerate file di `Assets/Konoha/Art/`.

## Arsitektur yang wajib dipahami
- **Semua scene, material, mesh di-generate dari kode editor.** Tidak ada `.unity`, `.prefab`, `.mat` yang di-commit.
  Sumber kebenaran visual = generator di `Assets/Konoha/Editor/`, BUKAN output di `Assets/Konoha/Generated/`.
  Jangan pernah "memperbaiki output" sambil membiarkan generatornya salah.
- `SpikeProject.cs` → generate scene PvP 4v4 (networked) + prefab hero.
- `CampaignPreviewProject.cs` → generate scene solo `JalurTakhtaPreview.unity` dengan MENGHAPUS komponen network dari scene PvP, lalu memasang UI & logika campaign.
- `CampaignCapitalArt.cs` + `CampaignCapitalMeshes.cs` → environment ibu kota (primitive + mesh prosedural).
- `CampaignRunState.cs` → state machine rute (segel, gerbang, garda, kursi, Power). Murni C#, ada EditMode test.
- `CampaignPreviewController.cs` → logika slice solo saat ini (sementara/prototipe).
- Mode PvP (`Assets/Konoha/Networking/*`, Netcode for GameObjects) harus tetap bisa di-generate & dimainkan identik.

## Fitur 0.0.8.2 yang TIDAK BOLEH regresi
Kamera orbit (geser layar kanan), pinch zoom, tombol KAMERA AWAL, joystick relatif kamera,
tombol LOMPAT (tanpa double jump), bibir kolam rendah + ramp keluar, pemulihan jatuh ke bawah dunia,
batas gerak oval tak terlihat, mode LIHAT ARENA / KEMBALI MAIN.

## Aturan kerja
- Audit dulu sebelum mengubah. Ubah sekecil mungkin; game harus tetap bisa dimainkan tiap milestone.
- Setiap iterasi: naikkan versi di `CampaignPreviewProject.cs` (MenuItem, `bundleVersion`, `bundleVersionCode`, teks footer)
  secara konsisten. Footer APK harus menunjukkan versi yang benar.
- Tulis catatan iterasi di `Docs/` (format mengikuti `Docs/JALUR_TAKHTA_FIRST_PLAYABLE_007.md`), termasuk langkah uji di perangkat.
- Jalankan `python3 scripts/source-check.py` sebelum commit.
- Kamu TIDAK bisa compile Unity di sini. Jangan pernah klaim "compile PASS", "APK PASS", atau "gameplay PASS".
  Tulis **BELUM DIVERIFIKASI** sampai owner melaporkan hasil build/perangkat.
- Performa Android: material dipakai bersama, shader sederhana, minim realtime light & transparansi.
- Commit dikelompokkan per perubahan yang bermakna. Akhiri pekerjaan dengan PR ke branch kerja, bukan ke `main`.

## IP & satire (wajib)
Satir tajam (keputusan owner 2026-10-02): sindir kebijakan, perilaku, dan sistem; keempat hero disindir setara; tanpa tuduhan pidana spesifik terhadap orang nyata.
Dunia, institusi, lambang, gedung, dan karakter adalah **fiksi Negara Konoha**.
Jangan meniru lambang Garuda resmi, logo partai/lembaga nyata, gedung nyata 1:1, atau wajah tokoh nyata 1:1.
Nama hero di UI: MEGA, GEMOY, ABAH, PAK WI.

## Format laporan ke owner (Bahasa Indonesia, non-teknis)
Branch · commit awal → commit baru · versi · file diubah · yang dikerjakan · yang sengaja tidak diubah ·
hasil cek statis · compile/APK/gameplay (BELUM DIVERIFIKASI bila belum) · risiko · langkah uji persis untuk owner · fase berikutnya.
Selalu sebutkan kapan owner harus menjalankan build dan apa yang harus dicek.
