# Jalur Takhta 0.2.1 — Material nyata, tanpa hiasan 17-an, kabel dirapikan

Branch kerja: `feat/material-nyata-0.2.1`, dibuat dari `feat/suasana-nusantara-0.2.0` (`4a236eb`). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

## Build

1. Unity Build Automation → branch **`feat/material-nyata-0.2.1`**, Unity 6000.0.60f1, pre-export `Konoha.Editor.SpikeProject.prepare`.
2. APK versi **0.2.1**, kode **30**. Label `JALUR TAKHTA 0.2.1  •  SOLO PREVIEW`.
3. Jika pemasangan ditolak, hapus aplikasi lama dulu.

## Masukan dari 0.2.0 dan perbaikannya

| Masukan owner | Perbaikan |
|---|---|
| Kabel hanya ada di depan dan mengganggu | Kabel yang **melintang di atas boulevard dihapus**. Tiang dipindah ke x ±12,5 (di luar pandangan utama) dan jaringannya **berlanjut sepanjang jalan raya di belakang spawn** (6 tiang per sisi + 1 bentang di atas pintu masuk), sehingga terbaca sebagai jaringan listrik sungguhan. |
| Tidak perlu hiasan 17 Agustus | **Umbul-umbul dan bendera segitiga dihapus.** |
| Masih terlihat seperti kartun | Lihat bagian **Material nyata** di bawah. |
| Serangan terasa aneh | **Belum** dikerjakan di versi ini. Direncanakan di 0.2.2 bersama karakter, karena "aneh"-nya berasal dari badan kapsul tanpa animasi (lihat Rencana). |

## Material nyata

1. **Semua permukaan utama mendapat tekstur detail + normal map** (tonjolan yang menangkap cahaya):
   - **Tembok plester:** butir pasir, gelombang acian, noda air.
   - **Genteng tanah liat:** baris genteng lengkung bertumpuk, warna per keping berbeda, noda jelaga. **Semua atap kini genteng** (sebelumnya hijau/merah polos). Kubah Istana tetap hijau tembaga.
   - **Batu andesit:** blok dengan nat.
   - **Paving conblock:** nat terlihat dalam.
   - **Kayu:** serat kayu.
   - **Aspal dan beton:** tekstur kasar.
   - **Rumput:** campuran hijau, bercak rumput kering (tanpa warna polos).
2. **Siap untuk foto sungguhan:** kalau file tekstur foto (Poly Haven, CC0) diunggah ke `Assets/Konoha/Art/Textures/<Slot>/`, generator otomatis memakainya menggantikan tekstur buatan kode. **Ini lompatan realisme terbesar yang bisa dicapai tanpa PC.** Panduan lengkapnya ada di `Docs/PANDUAN_TEKSTUR_NYATA.md` (8 slot, link unduhan langsung).
3. **Color grading ringan** (post-processing): tonemapping netral, kontras dan kehangatan sedikit dinaikkan, vignette tipis. Membuat gambar lebih "sinematik" dan tidak datar. Sengaja tanpa efek berat (tanpa SSAO/bloom) supaya tetap lancar di tablet.

## Rencana berikutnya

- **0.2.2 Karakter + serangan:** badan manusia (kepala, badan, lengan, kaki) dengan **animasi prosedural** (jalan, ayun serangan, terpental, tumbang), gerak menerjang saat skill, **efek benturan** (percikan, kilatan putih, getar kamera singkat, jeda sesaat "hit-stop"), dan **Kerbau Rakyat** untuk SERUAN IBU (MEGA). Semua ini khusus campaign; PvP tetap.
- **Jalur aset nyata (paralel, butuh unggahan owner):** tekstur foto (panduan di atas). Setelah itu, model karakter manusia beranimasi dari Mixamo dan model kerbau. Panduannya menyusul setelah tekstur berhasil.

## Checklist uji perangkat 0.2.1

1. Label 0.2.1.
2. Screenshot spawn + plaza + gedung Majelis/Biro dari dekat: tembok, genteng, dan paving harus bertekstur dan ada tonjolannya.
3. Umbul-umbul dan bendera segitiga sudah tidak ada. Kabel tidak lagi melintang di depan kamera.
4. Putar kamera ke belakang: tiang dan kabel berjajar sepanjang jalan raya.
5. Warna terasa lebih hidup atau malah terlalu gelap/terang? (Grading mudah disetel.)
6. FPS: grading menambah satu pass layar penuh. Laporkan kalau terasa lebih berat dari 0.2.0.

## Risiko

- Belum di-compile Unity. Bagian baru yang paling berisiko adalah pembuatan profil post-processing dari kode (`VolumeProfile`).
- Normal map buatan kode belum pernah dilihat di perangkat; bisa terlalu kuat atau terlalu lemah.
- Tekstur genteng memakai UV atap lama, jadi ukuran keping bisa berbeda antar-atap.
