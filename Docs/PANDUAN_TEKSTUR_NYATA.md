# Panduan tekstur nyata (foto) — dari tablet

Tujuannya: tembok, genteng, jalan, dan rumput di game memakai **tekstur foto sungguhan**, tidak lagi warna polos. Game sudah siap (0.2.1+). Selama file belum diunggah, game memakai tekstur buatan kode. Begitu file ada di folder yang benar, build berikutnya otomatis memakai foto.

**Sumber:** Poly Haven (polyhaven.com). Lisensi **CC0**: bebas dipakai, termasuk untuk komersial, tanpa wajib mencantumkan kredit.

## Daftar yang perlu diunduh (16 file, ±1–2 MB per file)

Buka setiap link di browser tablet dan file langsung terunduh. Setiap slot butuh **2 file**: *diff* (warna) dan *nor_gl* (tonjolan).

| Folder (slot) | Dipakai untuk | Link warna (diff) | Link tonjolan (nor_gl) |
|---|---|---|---|
| `Tembok` | Semua dinding dan gedung | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/beige_wall_001/beige_wall_001_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/beige_wall_001/beige_wall_001_nor_gl_1k.jpg |
| `Genteng` | Semua atap | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/clay_roof_tiles_02/clay_roof_tiles_02_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/clay_roof_tiles_02/clay_roof_tiles_02_nor_gl_1k.jpg |
| `Batu` | Tembok batu, pilar, gerbang | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/plaster_stone_wall_01/plaster_stone_wall_01_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/plaster_stone_wall_01/plaster_stone_wall_01_nor_gl_1k.jpg |
| `Paving` | Lantai plaza dan trotoar | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/pavement_02/pavement_02_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/pavement_02/pavement_02_nor_gl_1k.jpg |
| `Aspal` | Jalan raya ("jalan rusak", pas untuk satir) | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/road_damaged/road_damaged_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/road_damaged/road_damaged_nor_gl_1k.jpg |
| `Rumput` | Tanah dan rumput di sekitar kompleks | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/grass_ground/grass_ground_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/grass_ground/grass_ground_nor_gl_1k.jpg |
| `Kayu` | Kayu warung, pintu, ukiran | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/old_planks_02/old_planks_02_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/old_planks_02/old_planks_02_nor_gl_1k.jpg |
| `Beton` | Tiang listrik dan trotoar beton | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/cracked_concrete_wall/cracked_concrete_wall_diff_1k.jpg | https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/cracked_concrete_wall/cracked_concrete_wall_nor_gl_1k.jpg |

- **Nama file jangan diubah.** Game mengenali file dari kata `diff` dan `nor` di namanya.
- Kalau ada link yang gagal, buka `https://polyhaven.com/a/<nama>` (misalnya `https://polyhaven.com/a/beige_wall_001`), pilih **1K** dan **JPG**, lalu unduh *Diffuse* dan *Normal (GL)*.
- Boleh mengganti pilihan dengan tekstur Poly Haven lain, asalkan dimasukkan ke folder slot yang sama.

## Cara mengunggah lewat GitHub web (tablet)

Unggah ke branch **`feat/jalur-takhta-first-playable`**. Branch versi berikutnya dibuat dari branch ini, jadi tekstur ikut otomatis.

Untuk **setiap slot** (contoh: `Tembok`):

1. Buka `github.com/umarlim31/konoha-power-clash`, pindah ke branch `feat/jalur-takhta-first-playable`, lalu masuk ke folder `Assets/Konoha`.
2. **Add file → Create new file.** Di kolom nama, ketik: `Art/Textures/Tembok/README.md`. Setiap garis miring otomatis membuat folder.
3. Isi dengan satu kalimat, misalnya `Tekstur tembok dari Poly Haven (CC0)`, lalu **Commit changes**.
4. Buka folder `Assets/Konoha/Art/Textures/Tembok` → **Add file → Upload files** → pilih 2 file JPG slot itu → **Commit changes**.

Ulangi untuk ke-8 slot. Nama folder harus persis: `Tembok`, `Genteng`, `Batu`, `Paving`, `Aspal`, `Rumput`, `Kayu`, `Beton` (huruf besar di awal).

**Tidak harus sekaligus.** Slot yang belum diunggah tetap memakai tekstur kode. Mulai dari `Tembok`, `Genteng`, dan `Paving` saja, karena ketiganya paling terlihat.

## Setelah mengunggah

Beri tahu aku slot mana saja yang sudah diunggah. Aku akan menggabungkannya ke branch kerja, lalu kamu build seperti biasa.

**Batas:** tiap file maksimal 25 MB (tekstur 1K sekitar 1–2 MB, aman). Jangan pakai 4K: terlalu berat untuk tablet dan untuk APK.
