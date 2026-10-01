# Jalur Takhta 0.3.2 — Rapikan setelah model 3D

Branch kerja: `feat/poles-0.3.2`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.3.1 bersama 16 model Nusantara di-merge. Owner melaporkan Uji A dan Uji B "lancar"; screenshot menunjukkan baris MODEL 3D dengan semua slot terpasang. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/poles-0.3.2`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.3.2**, kode Android **41**. Label bawah: `JALUR TAKHTA 0.3.2  •  SOLO PREVIEW`.

## Yang baru (dari screenshot 0.3.1)
- **Baris `MODEL 3D` diringkas.** Daftar lengkap ke-16 slot terlalu panjang: keluar dari layar dan menumpuk di bawah tombol. Sekarang baris itu berbunyi `MODEL 3D: 16/16 slot terpasang (N objek)`. Rincian hanya muncul untuk slot yang **TIDAK DIPAKAI**.
- **Kota pesisir di teluk** (deretan kotak berwarna di belakang Istana, terlihat di LIHAT ARENA) kini memakai model owner. Bangunan rendah memakai **RumahKampung** dan bangunan tinggi memakai **Ruko**; semuanya menghadap ibu kota dan tanpa bayangan supaya ringan. Kalau model dihapus, kotak versi kode kembali.
- **Test EditMode** kini memperhitungkan model yang terpasang. Contohnya, kalau model Lentera ada, test mencari `Model Lentera`, bukan bagian lentera versi kode.

## Sengaja tidak diubah
- Rute, collider, model, `atur.txt`, combat, kamera, dan PvP.
- Tabrakan pendopo (4 tiang tengah dan bangku). Belum ada keputusan owner.

## Checklist uji perangkat 0.3.2
1. Label 0.3.2. Baris di atasnya berbunyi `MODEL 3D: 16/16 slot terpasang (...)` dalam satu baris pendek, tidak tertutup tombol.
2. LIHAT ARENA: di tepi teluk (belakang Istana) sekarang tampak deretan rumah dan ruko beratap genteng, bukan kotak polos.
3. Main sampai menang. Pastikan tetap lancar.

## Risiko
- Ada ±26 objek tambahan di kejauhan. Segitiganya kecil (rumah 262, ruko 240) dan tanpa bayangan, jadi dampaknya ke FPS seharusnya sangat kecil.
