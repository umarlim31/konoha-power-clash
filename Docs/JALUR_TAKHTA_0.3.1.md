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

## Integrasi paket model owner (NEGARA_KONOHA_NUSANTARA_SLOT_MODELS_v0.3.1)

**Audit paket (16 OBJ + MTL):**
- Semua 16 folder memakai nama slot yang tepat.
- Semua model di bawah batas segitiga: terberat Trembesi 3.400 dari batas 20.000; Palem 2.208 dari 3.000.
- Geometri: tidak ada mesh bolong, segitiga cacat, atau bahan yang tidak terdefinisi. Semua model berdiri di y=0, depan +Z.
- Tidak ada tekstur; warna diambil dari `Kd` di file MTL.

**Perbaikan pemuat model dari hasil audit:**
1. **Penggabungan mesh.** Model OBJ terpecah menjadi banyak bagian (Palem 141 bagian × ±40 pohon ≈ 5.600 objek). Sekarang semua bagian satu model digabung menjadi **satu mesh per slot**, dengan satu submesh per bahan. Hasilnya satu objek per tempat, jauh lebih ringan untuk tablet.
2. **Warna MTL dibaca langsung berdasarkan nama bahan.** Ini menjaga agar warna tidak hilang atau berubah putih kalau import OBJ di Unity tidak membawa `Kd`. Kaca dibuat mengkilap; logam dan perunggu agak mengkilap.
3. **PohonPalem memakai titik asal model (pangkal batang).** Sebelumnya model ditaruh di tengah tajuknya, sehingga batang bergeser ±0,3 m dari collider batang yang tetap.
4. **GedungLama dipaskan berdasarkan lebar** (4,4 m), tidak berdasarkan tinggi. Untuk gedung setinggi 5 m, cara lama mengecilkan model ke 0,85. Akibatnya dinding tabrak 4 × 4 m menonjol 0,4 m di luar dinding yang terlihat.
5. **Tulisan nama tetap tampil:**
   - nama toko ruko, di tepi kanopi;
   - nama gang di papan gapura;
   - "WARKOP RAKYAT" di tepi atap warung.
6. `Pendopo/atur.txt` ditambah `skala=1.1` dan `naik=-0.41`. Dengan itu tiang sudut model tepat di collider tiang (±1,85 m) dan lantainya setinggi lantai tabrak (0,15 m), jadi hero tidak tenggelam di lantai.

**Ukuran hasil pemasangan (perkiraan dari ukuran file):**

| Slot | Skala | Ukuran (meter) |
|---|---|---|
| Lentera | 0,98 | 2,3 tinggi |
| Palem | ±0,9 | 5,9 tinggi |
| Ketapang | 1,41 | 6,4 tinggi |
| Flamboyan | 1,23 | 5,2 tinggi |
| Trembesi | 0,97 | 11 × 4,8 tinggi |
| Pisang | 0,88 | 2,7 tinggi |
| Becak | 1,04 | — |
| Motor | 0,97 | — |
| Gerobak | 0,96 | — |
| Warung | 0,93 | — |
| PJU | 1,02 | 7,2 tinggi |
| Gapura | 0,85 | 3,6 tinggi (dibatasi tebal kotak 0,8 m) |
| Rumah | 0,96 | — |
| Ruko | 1,01 | — |
| Gedung Lama | 1,01 | 8,6 tinggi |
| Pendopo | 1,0 | — |

**Yang perlu diperhatikan di Uji B:**
- **Pendopo:** model punya 8 tiang, tetapi tabrakan lama hanya 4 tiang sudut, jadi 4 tiang tengah bisa ditembus. Tabrakan lama juga punya 2 bangku di dalam pendopo yang tidak ada di model, jadi bisa terasa menabrak "udara". Tabrakan sengaja tidak diubah tanpa persetujuan owner.
- **Gapura** jadi lebih pendek (3,6 m) karena modelnya lebih tebal dari kotak slot. Bisa dinaikkan dengan `skala=1.2` di `atur.txt`.
- **Trembesi** lebih pendek (4,8 m) daripada versi kode (±7,5 m), tetapi lebarnya sama 11 m.

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
