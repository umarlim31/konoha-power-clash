# Jalur Takhta 0.2.0 — Suasana Nusantara

Branch kerja: `feat/suasana-nusantara-0.2.0`, dibuat dari `feat/jalur-takhta-0.1.1` (`0e2f815`; 0.1.1 ikut di dalamnya karena belum diuji). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

## Rencana visual (disetujui owner 2026-09-30)

- **Bertahap.** Pertama dari kode (tanpa unggah file), lalu model 3D siap pakai satu per satu.
- **Urutan:** 0.2.0 suasana Indonesia → 0.2.1 karakter lebih hidup → 0.2.2 efek serangan (MEGA: **Kerbau Rakyat**, bukan banteng).
- "Realistis penuh" seperti foto tidak mungkin dari primitive dan terlalu berat untuk tablet. Targetnya **low-poly bergaya yang terasa Indonesia**.

## Isi 0.2.0

| Unsur | Letak |
|---|---|
| **Jalan raya aspal** bermarka putih putus-putus + kerb **belang kuning-hitam** + trotoar | Selatan spawn (di luar batas oval) |
| **Kerb belang kuning-hitam** | Kedua sisi boulevard Gerbang Rakyat |
| **Tiang listrik beton** + palang, isolator, trafo, papan bahaya, **kabel semrawut** (menjuntai, menyilang, satu diagonal) | x ±9,8 sepanjang boulevard |
| **Umbul-umbul** bambu melengkung, bendera panjang warna-warni | x ±5,3 sepanjang boulevard |
| **Bendera segitiga merah-putih** (hiasan 17-an) | 3 untaian melintang boulevard |
| **4 baliho satir fiktif:** "SELAMAT DATANG DI IBU KOTA KONOHA", "JALAN RUSAK? SABAR, MASIH DIANGGARKAN", "KONOHA MAJU, RAKYAT ANTRI", "DILARANG KAMPANYE DI SINI\* \*kecuali yang sedang berkuasa" | Kiri-kanan boulevard |
| **Warung kopi** "WARKOP RAKYAT": terpal biru, renteng sachet, termos, toples kerupuk, bangku panjang | Barat gerbang |
| **3 motor bebek** parkir | Depan warung |
| **Gerobak bakso** + payung oranye | Timur gerbang |
| **Pohon trembesi** (peneduh jalan khas Indonesia) | Dekat gerbang dan luar sayap |
| **Sawah** berpematang | Kiri-kanan kompleks |
| **Gunung berapi** berkabut | Cakrawala utara |
| **Paving conblock** anyaman, warna lebih hangat | Seluruh plaza |
| Cahaya tropis lebih hangat, langit lebih biru, kabut lebih jauh (far clip 260) | Seluruh peta |

## Catatan IP / satir

- Semua teks fiktif. Tidak ada nama atau logo partai dan tidak ada wajah tokoh.
- **Sengaja tidak memakai pohon beringin** (simbol partai nyata); diganti trembesi.
- Merah-putih hanya muncul sebagai hiasan jalan 17-an, bukan lambang negara. Garuda tetap tidak dipakai.

## Teknis

- `Assets/Konoha/Editor/CampaignCapitalArt.Nusantara.cs` (baru, `partial`) dan `CampaignCapitalMeshes.Pennant()`.
- Semua dekor **tanpa collider**, sehingga rute, titik spawn, dan test jalur tidak berubah. Hero bisa menembus motor atau gerobak; collider bisa ditambah nanti setelah posisinya dianggap pas.
- Dekor tinggi bernama `NusantaraTall …` ikut disembunyikan kamera saat menutupi hero (sistem 0.0.9.4).
- Dekor bersifat batching-static dengan material dipakai bersama: ±430 objek baru, termasuk ±130 potongan kabel.

## Checklist uji perangkat 0.2.0

1. Label `JALUR TAKHTA 0.2.0  •  SOLO PREVIEW`. Jika pemasangan ditolak, hapus aplikasi lama dulu.
2. **Screenshot dari spawn tanpa memutar kamera**, lalu **putar kamera ke belakang** (jalan raya).
3. Apakah terasa "Indonesia"? Unsur mana yang paling kena, dan mana yang aneh atau mengganggu?
4. Kursi di ujung utara masih terlihat dari spawn.
5. Jalan di boulevard: kabel, umbul-umbul, dan baliho tidak menutupi hero terlalu lama.
6. **FPS** di boulevard (bagian paling padat). Laporkan kalau terasa patah-patah.
7. Semua gameplay 0.1.1 (Majelis dari jalur taman, DUDUK = MENANG) tetap berjalan.

## Risiko

- Belum di-compile Unity.
- FPS bisa turun karena ±400 renderer baru dan jarak pandang yang lebih jauh. Kalau terasa berat, kabel dan umbul-umbul bisa dikurangi.
- Posisi dekor dihitung dari koordinat, belum pernah dilihat. Mungkin ada yang tumpang tindih atau melayang; screenshot sangat membantu.

## Berikutnya

- **0.2.1 Karakter lebih hidup:** tubuh berkepala, bertangan, dan berkaki dengan animasi jalan/serang/tumbang, plus seragam khas (safari Majelis, khaki Biro, seragam Garda, peci, batik motif prosedural).
- **0.2.2 Efek serangan:** Kerbau Rakyat untuk MEGA, dan efek khas untuk tiap skill hero lain.
