# Jalur Takhta 0.3.1 — Slot model 3D untuk bangunan & properti

Branch kerja: `feat/slot-model-0.3.1`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.3.0 di-merge (owner: "hasilnya bagus aman"). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`. Pemasangan model belum bisa dicoba karena belum ada file model di repo.

Permintaan owner: slot model 3D untuk bangunan dan properti, supaya owner bisa mengunggah file FBX/OBJ sendiri.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/slot-model-0.3.1`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.3.1**, kode Android **40**. Label bawah: `JALUR TAKHTA 0.3.1  •  SOLO PREVIEW`.

## Yang baru
- **16 slot model**:
  - Pohon: `PohonPalem`, `PohonKetapang`, `PohonFlamboyan`, `PohonTrembesi`, `PohonPisang`.
  - Lampu: `Lentera`, `LampuPJU`.
  - Kendaraan dan jualan: `Becak`, `MotorBebek`, `GerobakBakso`, `WarungKopi`.
  - Bangunan: `GapuraKampung`, `RumahKampung`, `Ruko`, `GedungLama`, `Pendopo`.
- **Cara pakai:** unggah file ke `Assets/Konoha/Art/Models/<Slot>/<Slot>.fbx` (atau `.obj`, atau satu-satunya file model di folder itu) beserta teksturnya. Panduan lengkap: **`Docs/PANDUAN_MODEL_3D.md`**.
- **Model dipasang otomatis:**
  - Ukurannya dipaskan ke kotak slot, model berdiri di tanah dan menghadap ke arah bentuk aslinya.
  - Bahannya diubah ke shader URP, jadi tidak pink. Daun transparan dibuat tembus pandang dengan benar.
  - Tekstur dibatasi 1024 px.
  - Kamera, lampu, collider, dan animasi bawaan file dibuang.
- **Tabrakan tidak berubah.** Bagian yang terlihat dari bentuk kode dihapus, tetapi collider-nya (batang palem, gedung lama, tiang pendopo) tetap di tempat.
- **Pengaman:**
  - Model yang melebihi batas segitiga slot atau gagal dibaca tidak dipakai, dan bentuk kode tetap tampil.
  - Generator tidak pernah gagal karena file model.
- **Baris `MODEL 3D: ...`** muncul di atas label versi. Isinya slot terpasang beserta jumlahnya, serta slot yang **TIDAK DIPAKAI** beserta alasannya. Baris ini hanya muncul kalau ada model yang diunggah.
- **`atur.txt` opsional** per slot: `skala`, `putar`, `naik`, `ukuran=asli`, `kredit`.
- Model pohon dan bangunan tinggi ikut memudar saat menutupi kamera, sama seperti bentuk kodenya.

## Sengaja tidak diubah
- Tampilan game tanpa model: identik dengan 0.3.0.
- Rute, collider, musuh, kamera, HUD, dan PvP.
- Istana, gerbang, monumen, menara, dan Kursi: tidak diberi slot karena bagian dari rute.
- Kendaraan yang berjalan di jalan lingkar: belum diberi slot. Mobil bergerak butuh penanganan roda dan arah, jadi ditunda ke versi berikutnya bila diperlukan.
- Folder `Assets/Konoha/Art/` tidak disentuh. Hanya setelan import Unity untuk file di dalamnya yang diatur.

## Checklist uji perangkat 0.3.1
**Uji A — tanpa model (sekarang):**
1. Build branch ini dan pastikan label `JALUR TAKHTA 0.3.1  •  SOLO PREVIEW`.
2. Pastikan baris `MODEL 3D` **tidak** muncul.
3. Main sampai menang. Tampilan dan permainan harus sama dengan 0.3.0.

**Uji B — dengan satu model:**
1. Unggah satu model kecil, misalnya lentera atau pohon low poly, ke branch `feat/jalur-takhta-first-playable` sesuai panduan.
2. Beri tahu aku. Aku gabungkan, lalu kamu build ulang.
3. Pastikan baris `MODEL 3D: Lentera x22` (atau slot yang kamu pilih) muncul.
4. Datangi tempatnya: model tampil, ukurannya pas, tidak pink, dan berdiri di tanah.
5. Kalau arah atau ukurannya salah, atur lewat `atur.txt`.

## Risiko
- Model unduhan sangat beragam. Arah depan yang salah bisa diperbaiki dengan `putar=180`. Model dengan banyak bagian kosong (misalnya ada bidang tanah besar di bawahnya) membuat model mengecil; perbaiki dengan `skala` atau dengan model lain.
- OBJ dengan tekstur yang tidak terhubung: game memakai satu-satunya tekstur warna di folder. Kalau ada banyak tekstur, warnanya bisa salah. FBX lebih aman.
- Kalau banyak slot diisi model yang berat, FPS bisa turun. Batas segitiga per slot membatasinya, tetapi totalnya tetap perlu diuji di tablet.
