# Jalur Takhta 0.3.4 — Pendopo tidak hilang, warga bermuka

Branch kerja: `feat/warga-hidup-0.3.4`, dibuat dari `feat/model-tampil-0.3.3` (isi 0.3.2 dan 0.3.3 ikut). Owner sudah menguji 0.3.3: "semuanya udah normal kembali". PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C#, `scripts/source-check.py`, dan pratinjau wajah (digambar ulang dengan Python dari ukuran yang sama).

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/warga-hidup-0.3.4`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.3.4**, kode Android **43**. Label bawah: `JALUR TAKHTA 0.3.4  •  SOLO PREVIEW`.

## Yang baru
### Pendopo (dan bangunan lain) tidak lagi menghilang saat didekati
- **Penyebab:** kamera menyembunyikan benda tinggi yang menutupi hero. Di 0.3.3, model pendopo adalah satu objek utuh, jadi seluruh pendopo (lantai, tiang, atap) hilang dan hanya bayangannya yang tersisa.
- **Perbaikan:** model dipotong menjadi **bagian bawah** (selalu terlihat) dan **bagian atas** (atap/payung, yang boleh memudar). Batas potong per slot:

| Slot | Bagian yang boleh memudar |
|---|---|
| Pendopo | di atas 50% tinggi (atap) |
| Warung kopi | di atas 70% |
| Gerobak bakso | di atas 85% (payung) |
| Gapura | di atas 65% |
| Rumah kampung | di atas 60% |
| Gedung lama | di atas 70% |

Pohon tetap memudar utuh, sama seperti versi kode.

### Karakter lebih hidup
- **Mata kartun:** putih mata, pupil gelap, dan kilau kecil. Matanya **berkedip** setiap 2–5 detik. Wajah juga mendapat pipi kemerahan tipis, dan alis turun sedikit supaya tidak tertutup rambut. Berlaku untuk hero dan semua anggota Majelis, Biro, dan Garda.
- **Warga** (pejalan kaki, penjual, penonton) kini **punya wajah**: mata berkedip, alis, hidung, mulut, telinga, leher, dan tangan.
  - Di versi lama, bola rambut dan jilbab warga menutupi wajah, sehingga tampak "tanpa muka". Sekarang rambut digeser ke belakang dan **jilbab membingkai wajah**.
  - Tubuh warga kini membulat: badan, lengan, dan kaki berbentuk kapsul, sandal oval. Bentuk lamanya kotak dan tabung.

### Rekomendasi alur baru (dokumen)
`Docs/REKOMENDASI_ALUR_JALUR_TAKHTA_v2.md`: diagnosis kenapa alur terasa aneh, struktur baru **"Musim Pemilu"**, mekanik Lawan/Rangkul, Kartu Kebijakan, Krisis, ending Koran Konoha, bank satir, dan urutan versi 0.4.x. **Belum dikerjakan di game;** menunggu keputusan owner.

## Sengaja tidak diubah
- Rute, collider, combat, angka, kamera, model 3D dan `atur.txt`, serta PvP.
- Sendi dan animasi tubuh (hanya wajah yang ditambah).

## Checklist uji perangkat 0.3.4
1. Pastikan label 0.3.4.
2. Jalan masuk ke **pendopo** di taman sayap (dekat Majelis Daun). Saat atap menutupi kamera, **hanya atapnya yang memudar**; lantai dan tiang tetap terlihat.
3. Pakai KAMERA DEKAT dan dekati warga yang berdiri di warung kopi dan gerobak bakso. Mereka harus punya mata, alis, hidung, dan mulut, dan matanya berkedip. Warga berjilbab wajahnya terlihat.
4. Lihat wajah MEGA dan musuh dari dekat: mata putih berpupil dan berkedip.
5. Main sampai menang dan pastikan tetap lancar.

## Risiko
- Wajah menambah ±12 objek kecil per warga (±35 warga) dan ±8 per hero/musuh. Semuanya tanpa bayangan, jadi dampaknya ke FPS seharusnya kecil, tetapi perlu dicek.
- Dari kamera JAUH, wajah memang kecil. Perbedaan paling terasa di KAMERA DEKAT.
