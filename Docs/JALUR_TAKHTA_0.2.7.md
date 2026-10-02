# Jalur Takhta 0.2.7 — Tanpa kedip, bangunan lebih nyata, suasana imersif

Branch kerja: `feat/imersi-nusantara-0.2.7`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.6 di-merge berdasarkan hasil uji di tablet. Owner menilai gamelan latar "sudah pas". PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

Masukan owner: "perbaiki bagian yang kelihatan kedip-kedip, perbaiki infrastruktur bangunannya supaya lebih realistis, dan buat suasananya seperti benar-benar berada di Nusantara."

## Penyebab kedip-kedip (dari video LIHAT ARENA)

Di video, lantai plaza berganti-ganti dengan rumput hijau, dan atap serta spanduk muncul-hilang. Ada dua penyebab:

1. **Lapisan lantai terlalu berdekatan (z-fighting).**
   - Rumput lanskap hanya 1,1 cm di bawah lantai plaza.
   - Beberapa lapisan tipis bahkan berada di ketinggian yang sama persis: jalur upacara dengan pelataran motif, jalur upacara dengan boulevard, penyeberangan taman dengan promenade, dan jalan raya dengan jalan samping di tikungan.
   - Dari jauh, layar tablet tidak bisa memutuskan lapisan mana yang di depan, sehingga keduanya bergantian terlihat.
   - Sejak 0.2.5 jarak pandang dibuat lebih jauh, jadi gejalanya makin kelihatan.
2. **Penyembunyi atap tetap bekerja saat LIHAT ARENA.** Fitur 0.0.9.4 itu menyembunyikan atap dan spanduk yang menghalangi pandangan kamera ke hero. Di tampilan LIHAT ARENA kamera berputar tinggi di atas peta, sehingga atap dan spanduk terus ditutup-buka mengikuti posisi hero.

### Perbaikan
- **Semua lapisan lantai yang bertumpuk kini berjarak ±2 cm.**
  - Rumput lanskap diturunkan 10 cm, dan sawah serta laut berada di antaranya.
  - Lapisan jalan dan trotoar di tikungan jalan lingkar dipisahkan.
  - Semua lapisan ini tanpa tabrakan, jadi rute dan langkah hero tidak berubah.
- **Kamera:** bidang potong dekat dinaikkan ke 0,5 m (kamera orbit memang tidak pernah lebih dekat dari 0,8 m). Saat LIHAT ARENA dinaikkan ke 2 m, sehingga presisi kedalaman jauh lebih baik.
- **LIHAT ARENA mematikan penyembunyi atap** selama tampilan itu aktif, lalu menyalakannya lagi saat KEMBALI MAIN.

## Bangunan lebih nyata

- **Rumah kampung** (22 buah):
  - bidang pelana atap kini tertutup (sebelumnya segitiga atapnya bolong);
  - alas batu, kusen jendela dan pintu putih, dan ambang jendela;
  - genteng bubungan dan lisplang putih di bawah atap;
  - teras beratap genteng dengan dua tiang kayu, dan pot tanaman di depan pintu.
- **Ruko** (16 buah): alas batu, pilaster putih di kedua sisi, balkon beton berpagar besi di lantai 2, unit AC, dan talang air.
- **Menara** (6 buah): alas batu yang lebih lebar dan kornis di puncak batang, supaya tidak terlihat seperti cerobong asap.

## Suasana

- **Burung berkicau** sesekali, setiap 5–12 detik dengan nada yang berbeda-beda.
- **Tukang bakso mengetuk mangkok** ("ting-ting-ting") di gerobaknya setiap 7–13 detik. Makin dekat ke gerobak, makin jelas terdengar.
- Gamelan latar tetap seperti 0.2.6.
- Semua suara ikut tombol SUARA.

## Sengaja tidak diubah

- Rute, tabrakan, musuh, hero, dan gamelan.
- Kamera main. Sudut dan jaraknya disetel bersama owner di 0.0.9.4, dan cubit layar tetap bisa dipakai untuk mendekat.
- PvP.

## Checklist uji perangkat 0.2.7

1. Label `JALUR TAKHTA 0.2.7  •  SOLO PREVIEW`.
2. Tekan **LIHAT ARENA** dan tunggu 10–20 detik: **tidak ada lagi** lantai yang berkedip menjadi rumput, dan tidak ada atap atau spanduk yang muncul-hilang.
3. Berjalan dari Gerbang Rakyat ke Plaza: sambungan boulevard, jalur upacara, dan pelataran motif tidak berkedip.
4. Putar kamera ke rumah kampung di kiri-kanan: atap pelana tertutup, dan terlihat kusen putih, teras beratap, serta pot tanaman.
5. Lihat ruko di seberang jalan raya: ada balkon, AC, dan pilaster. Menara punya alas dan kornis.
6. Dengarkan: burung berkicau sesekali. Dekati gerobak bakso: bunyi "ting-ting-ting".
7. Main sampai menang. Rute dan kamera harus sama seperti sebelumnya.
8. FPS: sama seperti 0.2.6?

## Risiko

- Detail rumah dan ruko menambah ±430 bagian diam. Unity menggabungkannya (static batching), tapi tetap perlu dicek FPS-nya.
- Kalau ternyata masih ada kedipan kecil di tempat lain, kirim screenshot atau rekaman lokasinya, supaya lapisan di tempat itu bisa dipisahkan dengan cara yang sama.
