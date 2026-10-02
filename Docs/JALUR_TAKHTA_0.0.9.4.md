# Jalur Takhta 0.0.9.4 — kamera tidak tertutup atap, efek suara

Branch kerja: `feat/jalur-takhta-0.0.9.4`, dibuat dari `feat/jalur-takhta-first-playable` (`b591f68`; 0.0.9.3 sudah di-merge setelah lolos di tablet). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#) dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/jalur-takhta-0.0.9.4`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: versi **0.0.9.4**, kode Android **26**.
4. Label bawah: `JALUR TAKHTA 0.0.9.4  •  SOLO PREVIEW`.

## 1. Kamera: hero tidak lagi tertutup atap/gerbang

Dari screenshot 0.0.9.3: saat hero berada di bawah gapura Gerbang Rakyat, **atap merah dan tulisan GERBANG RAKYAT menutupi seluruh layar**. Hal yang sama terjadi dengan Gerbang Dalam, dinding, dan pelepah palem.

- Penyebabnya: sejak 0.0.9.2.1 kamera sengaja tidak maju untuk benda seperti ini (supaya tidak zoom sendiri), tetapi benda itu tetap tergambar di depan hero.
- **Perbaikan:** benda tinggi yang berada **di antara kamera dan hero** kini disembunyikan sementara, lalu muncul lagi ±0,35 detik setelah tidak menghalangi. Bayangannya tetap ada, jadi pencahayaan tidak berubah.
- Yang termasuk: semua atap (gapura, paviliun, gedung Majelis/Biro, Istana), lintel/pylon/dinding Gerbang Rakyat dan Gerbang Dalam, kantor Kepala Biro, pelepah/batang palem, bendera, dan papan nama gerbang.
- Monumen Garuda tetap memakai efek transparan miliknya sendiri.
- Tanpa physics atau material transparan: hanya cek kotak batas (murah untuk Android).

## 2. Efek suara

Semua suara **dibuat dari kode saat game dibuka** (sintesis), jadi tidak ada file audio yang perlu diunggah.

| Kejadian | Suara |
|---|---|
| Hero memukul musuh / hero terkena | "duk" pendek / "dug" berat; perisai menahan: denting logam |
| Skill S1/S2 dan ULT | desing naik; ULT: dentum + desing |
| Musuh tumbang / Staf menyerah | nada turun |
| KETOK PALU / SALAH LOKET mulai | bip peringatan dua kali |
| Palu mengetuk / hero dipindah loket | "tok" kayu / desing turun bergetar |
| Loket tercap | "jeglek" stempel |
| BLOK MAJELIS PECAH | kaca pecah |
| Pintu Kepala Biro / Gerbang Dalam / Kursi terbuka | gemuruh pintu |
| Segel didapat / duduk di Kursi | arpeggio naik |
| RESTU RAKYAT | arpeggio berkilau |
| MULAI (setelah pilih hero) | gong |
| Runtuh | nada turun sedih |
| Menang | fanfare |
| Tombol menu (pilih hero, MULAI, DUDUK/ULANG, LIHAT ARENA, KAMERA AWAL) | klik |

- Tombol baru **SUARA: NYALA / MATI** di kanan atas, di bawah KAMERA AWAL. Pilihan disimpan di tablet.
- Suara di kejauhan lebih pelan; di luar ±38 m tidak terdengar.
- Suara hanya membaca keadaan permainan yang sudah ada (tanpa lalu lintas jaringan baru). Kode tempur bersama hanya mendapat dua "kait" presentasi. **PvP tetap tanpa suara**, sama seperti sebelumnya.

## Sengaja tidak diubah

Aturan permainan, angka, sudut default kamera, dan PvP. Keseimbangan Garda (RUNTUH 12 di screenshot) ditangani di 0.1.0 bersama Garda Takhta versi lengkap.

## Checklist uji perangkat 0.0.9.4

1. Label `JALUR TAKHTA 0.0.9.4  •  SOLO PREVIEW`. Ada tombol **SUARA: NYALA** di kanan atas.
2. Pilih hero: terdengar klik. Tekan MULAI: terdengar gong.
3. Berjalan **di bawah gapura Gerbang Rakyat**: atap dan tulisan GERBANG RAKYAT menghilang selama menutupi hero, lalu muncul lagi.
4. Pukul Kroni: ada suara pukulan. Terkena pukulan: suara berbeda. Kalahkan 3 Kroni: suara tumbang, lalu suara RESTU.
5. Majelis: bip sebelum KETOK PALU, "tok" saat palu turun, dan kaca pecah saat blok pecah.
6. Biro: suara stempel tiap loket tercap, gemuruh saat pintu Kepala Biro terbuka, dan desing saat SALAH LOKET memindahkan hero.
7. Lewat Gerbang Dalam dan dekat dinding: hero tetap terlihat.
8. Tekan **SUARA** → MATI: semua efek diam. Tutup lalu buka game: tetap MATI.
9. Apakah volume dan jenis suara terasa pas? Sebutkan mana yang mengganggu atau terlalu pelan.
10. FPS tetap sama seperti 0.0.9.3.

## Risiko

- Belum di-compile Unity.
- Suara sintetis terdengar "retro/chiptune". Kalau nanti ingin suara rekaman, tinggal ganti sumber klipnya; kaitan ke kejadian tetap sama.
- Atap yang disembunyikan bisa terlihat "berkedip" saat hero berjalan di tepi atap. Jeda 0,35 detik mengurangi ini, dan nilainya bisa disetel.
