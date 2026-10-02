# Jalur Takhta 0.2.8 — Kamera jalanan dan kampung Indonesia

Branch kerja: `feat/kamera-jalan-0.2.8`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.7 di-merge berdasarkan hasil uji di tablet. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

Masukan owner: sudut kamera harus membuat pemain merasakan suasana Nusantara, infrastruktur harus lebih bernuansa Indonesia, dan owner bertanya apakah ada cara agar visual benar-benar sesuai keinginannya.

## Build manual Android/tablet (Unity Build Automation)

1. Branch **`feat/kamera-jalan-0.2.8`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.2.8**, kode Android **37**. Label bawah: `JALUR TAKHTA 0.2.8  •  SOLO PREVIEW`.

## Yang baru

### 1. Tombol KAMERA: JAUH / DEKAT
Tombol baru ini ada di kanan atas, di bawah SUARA. Pilihannya disimpan di tablet.

| | JAUH (seperti sebelumnya) | DEKAT (baru) |
|---|---|---|
| Sudut | 22° dari atas | **12°**, hampir setinggi mata |
| Jarak | 22 m (cubit: 13–28 m) | **9 m** (cubit: 5,5–16 m) |
| Titik pandang | 0,8 m di atas kaki hero | **1,55 m** (setinggi kepala) |
| Lensa | 50° | **60°** (lebih lebar) |
| Kesan | peta taktis dari atas | berdiri **di jalanan kota**: gedung, pohon, bendera, dan orang terlihat setinggi mata |

- Orbit (geser layar kanan), cubit zoom, KAMERA AWAL, penyembunyi atap, getar layar, dan LIHAT ARENA bekerja sama di kedua mode.
- **Bawaan tetap JAUH.** Kriteria MVP §13 mewajibkan Kursi terlihat dari spawn. Kamera rendah akan terhalang monumen, jadi DEKAT adalah pilihan pemain.

### 2. Istana beratap tumpang
Kubah hijau (yang lebih mirip gedung Eropa) diganti **atap tumpang tiga tingkat** bergenteng dengan **mustaka emas** di puncaknya, seperti balairung Jawa/Bali dan istana di gambar target.

### 3. Kampung dan jalanan Indonesia
- **Gapura kampung merah-putih:** "GANG MERDEKA RT 03 / RW 07" dan "GANG GOTONG ROYONG RT 05 / RW 07", di gang antara rumah kampung.
- **2 becak** menunggu penumpang di mulut boulevard, dengan kap, jok merah, dan tiga roda.
- **10 pohon pisang** di sela rumah kampung.
- **Lampu jalan PJU:** tiang besi dengan lengan melengkung di sepanjang jalan raya dan jalan samping.

## Sengaja tidak diubah
- Rute, tabrakan, musuh, dan hero.
- Kamera JAUH tetap sama persis seperti yang diuji di 0.2.7.
- PvP. Kamera PvP memakai nilai bawaan yang sama; batas zoom baru hanya diubah oleh mode DEKAT di campaign.

## Checklist uji perangkat 0.2.8
1. Label `JALUR TAKHTA 0.2.8  •  SOLO PREVIEW`. Ada tombol **KAMERA: JAUH** di bawah SUARA.
2. Tekan tombol itu sampai tertulis **KAMERA: DEKAT**. Kamera turun ke belakang hero setinggi kepala.
3. Berjalan di boulevard dengan kamera DEKAT: bendera, flamboyan, candi bentar, becak, gedung, dan orang terlihat dari samping. Putar kamera ke kiri-kanan.
4. Bertarung dengan Kroni di mode DEKAT: masih nyaman atau terlalu sempit? Kalau sempit, tekan lagi untuk kembali ke JAUH.
5. Tutup lalu buka game: pilihan kamera tetap tersimpan.
6. KAMERA AWAL mengembalikan sudut sesuai mode yang aktif. Cubit zoom juga berfungsi di kedua mode.
7. Istana beratap tumpang dengan puncak emas.
8. Dekati rumah kampung (kiri-kanan peta): ada gapura, pohon pisang, dan lampu PJU di jalan.
9. Main sampai menang.
10. PvP: kamera tetap sama seperti dulu.

## Risiko
- Di mode DEKAT, tembok dan gedung lebih sering berada di antara kamera dan hero. Penyembunyi atap dan tabrakan kamera sudah menangani ini, tapi perlu dirasakan langsung di tablet.
- Nilai kamera DEKAT (12°, 9 m) adalah tebakan awal. Setelah owner mencoba, angkanya bisa disetel.

## Cara agar visual benar-benar sesuai keinginan owner
Semua yang ada sekarang dibangun dari bentuk dasar (kotak, bola, silinder) oleh kode. Pendekatan ini bisa terus diperbaiki, tapi hasilnya selalu tampak seperti "game low-poly". Untuk tampilan seperti gambar target, ada empat jalan, dan bisa digabung:

| Cara | Hasil | Biaya/usaha | Catatan |
|---|---|---|---|
| **A. Slot model 3D** (disarankan dikerjakan berikutnya, 0.2.9) | Bangunan dan properti memakai model 3D sungguhan, dengan cadangan kode kalau file belum ada | Aku membangun slotnya; owner mengunggah file FBX/OBJ lewat GitHub web (maks 25 MB per file) | Pola yang sama dengan slot tekstur foto dan hero FBX |
| **B. Model gratis** | Cukup realistis | Gratis | Poly Haven (CC0), Sketchfab (banyak berlisensi CC-BY, wajib mencantumkan kredit). Lisensi tiap model perlu dicek |
| **C. Model buatan AI dari gambar** | Bisa mirip gambar target | Berbayar atau terbatas | Layanan text/image-to-3D (misalnya Meshy, Tripo). Harga dan lisensi perlu diverifikasi sendiri |
| **D. Seniman 3D** | Paling sesuai keinginan | Paling mahal | Cocok untuk 3–5 bangunan kunci (Istana, Majelis, Biro, Gerbang) |

Kalau owner bisa meminjam laptop atau PC, Unity Editor memungkinkan menyusun kota secara visual (seret-lepas). Hasilnya jauh lebih cepat daripada lewat kode, dan paket dari Unity Asset Store hanya bisa dipasang lewat Editor.
