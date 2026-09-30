# Jalur Takhta 0.2.6 — Rasa Nusantara

Branch kerja: `feat/nusantara-rasa-0.2.6`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.5 (Nusantara Megah) di-merge berdasarkan hasil uji di tablet. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

Masukan owner: "upgrade lagi agar lebih terlihat seperti Nusantara Indonesia."

## Evaluasi screenshot 0.2.5 (jujur)

| Yang terlihat | Masalah |
|---|---|
| Pohon flamboyan | 3–4 gumpalan oranye raksasa, terlihat seperti mainan dan menutupi layar di depan kamera |
| Ubin boulevard | Kontras terlalu keras (navy dan emas terang, ubin 3 m), sehingga terkesan seperti karpet kartun |
| Gedung jauh di sisi Plaza | Kotak tinggi dengan celah-celah hitam, tampak seperti nisan, tidak berkesan Indonesia |
| Patung penjaga | Badan bulat dengan kepala seperti telur emas, terlihat aneh |
| Lingkaran monumen | 16 garis gelap di sekelilingnya membuatnya tampak seperti jam dinding |
| Suasana | Sepi. Belum ada suara yang langsung "berbunyi Indonesia" |

## Yang baru

1. **Musik gamelan latar** (dibuat dari kode, bukan rekaman).
   - Satu gongan 12 detik berulang tanpa putus, berlaras **slendro**: saron memainkan balungan, bonang bermain mipil dua kali lebih cepat, kempul di tengah, dan **gong** di awal putaran.
   - Volumenya pelan. Tombol **SUARA** ikut mematikannya.
2. **Candi bentar dari bata merah**, gerbang terbelah bergaya Majapahit/Bali, di mulut boulevard dekat jalan raya: tingkat bertangga, pelipit batu, dan kemuncak stupa kecil.
3. **Stupa candi** di atas alas (4 buah, di Plaza dan halaman Garda): bantalan teratai, stupa berbentuk genta, harmika, dan yasti. Patung penjaga berkepala telur dihapus.
4. **Gedung lama Indonesia** menggantikan kotak bercelah hitam. Dindingnya putih dengan alas batu, jendela berdaun krepyak hijau, balkon kayu, pintu kayu, teras beratap genteng, dan atap genteng bertingkat. Ukurannya sama, jadi tabrakan tidak berubah.
5. **Padma (teratai delapan kelopak)** perunggu dengan titik terakota di lingkaran monumen, menggantikan tanda-tanda yang mirip jam.
6. **Flamboyan diperbaiki:** payung lebar dari banyak rumpun kecil, kebanyakan daun hijau dengan bercak bunga merah tua. Ukurannya kira-kira setengah dari sebelumnya.
7. **Ubin motif dihaluskan:** ubin 1,5 m (sebelumnya 3 m), warnanya kalem seperti ubin tegel lama.

## Sengaja tidak diubah

Rute, tabrakan, musuh, keseimbangan hero, kamera, kota hidup, dan PvP. Musik gamelan hanya ada di mode solo.

## Checklist uji perangkat 0.2.6

1. Label `JALUR TAKHTA 0.2.6  •  SOLO PREVIEW`. Setelah MULAI terdengar **gamelan** pelan dengan gong setiap ±12 detik.
2. Tekan SUARA → MATI: gamelan diam. NYALA: gamelan kembali.
3. Putar kamera ke belakang di awal permainan: **candi bentar bata merah** mengapit mulut boulevard.
4. Ubin boulevard lebih kecil dan kalem. Pohon flamboyan kini hijau dengan bunga merah, tidak lagi gumpalan oranye besar.
5. Di Plaza: lingkaran monumen bermotif **teratai**, **stupa** di atas alas, dan **gedung lama** berjendela hijau di kiri-kanan belakang.
6. Main sampai menang. FPS sama seperti 0.2.5?
7. Pendapatmu: apakah volume gamelan pas, atau terlalu keras / terlalu pelan?

## Risiko

- Gamelan dibuat saat game dibuka (±0,5 juta sampel suara). Di tablet ini mungkin menambah jeda sesaat (<0,5 detik) saat membuka mode solo.
- Nada gamelan sintetis terdengar seperti "bel logam". Untuk suara asli nanti bisa diganti rekaman bebas lisensi yang diunggah owner.
