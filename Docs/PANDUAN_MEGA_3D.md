# PANDUAN MEGA 3D — dari Character Sheet ke dalam game (khusus tablet)

Tujuan: menghasilkan file **Mega.fbx** (model + rig humanoid) dan beberapa file animasi, lalu mengunggahnya ke GitHub. Setelah itu Claude Code yang memasangnya ke game (Prompt 2).

> ⚠️ **Kapan dikerjakan:** boleh mulai sekarang, **paralel** dengan Prompt 1 (0.0.8.3). File baru dipakai saat kamu menjalankan Prompt 2.
> ⚠️ Nama tool, fitur, harga, dan lisensi di bawah **perlu kamu cek sendiri** di situsnya. Fitur tool AI 3D cepat berubah.

---

## Langkah 0 — Syarat wajib sebelum mulai

1. **Wajah harus original.** Wajah di sheet sekarang terlalu mirip tokoh nyata. Untuk game yang akan dimonetisasi, buat versi yang lebih *stylized* (proporsi kartun, mata sedikit lebih besar, garis wajah disederhanakan) tetapi tetap "ibu berkacamata, berwibawa". Kostum kebaya merah-hitam, selendang, dan sanggul boleh dipertahankan.
2. **Roh banteng di Ultimate** dibuat bentuk makhluk penjaga orisinal Konoha (misalnya berkepala lebih abstrak, bertanduk api teratai), jangan logo banteng yang dikenal publik.
3. **Lisensi komersial:** pastikan paket tool yang kamu pakai mengizinkan hasil model dipakai di game komersial. Banyak paket gratis membatasi hal ini.

## Langkah 1 — Siapkan gambar acuan yang "ramah 3D"

Tool image-to-3D bekerja paling baik jika gambarnya:
- **1 karakter saja, seluruh badan, tampak depan**, pose **A-pose** (tangan terbuka sekitar 45° ke bawah, kaki sedikit terbuka).
- Latar polos (putih/abu), tanpa efek api, tanpa teks.
- Selendang **tidak berkibar jauh**. Kain yang berkibar akan menjadi gumpalan kaku di model 3D.

Cara: potong panel **FRONT** dari turnaround sheet, atau minta AI gambar membuat ulang *"full body front view A-pose, plain white background, stylized game character, same costume"* dengan wajah yang sudah di-stylize (Langkah 0).
Siapkan juga panel **SIDE** dan **BACK**. Beberapa tool menerima multi-view dan hasilnya lebih rapi.

## Langkah 2 — Ubah gambar menjadi model 3D

Pilih **satu** tool image-to-3D berbasis web, misalnya Meshy, Tripo, atau Hunyuan3D (cek mana yang lancar dibuka di browser tablet).

Setelan yang dicari (nama menu bisa berbeda):
| Setelan | Nilai |
|---|---|
| Style | Stylized / cartoon (bukan realistic) |
| Polycount / target faces | **15.000 – 25.000** triangles (sheet menulis 45K, terlalu berat untuk banyak karakter di HP) |
| Texture | PBR / base color, **2048 px** |
| Topology | Quad jika ada pilihannya |
| Symmetry | On |

Periksa hasilnya: kacamata utuh, tangan terpisah dari badan, kaki terlihat (bukan satu blok dengan rok). Jika jelek, generate ulang 2–3 kali dan pilih yang terbaik.

## Langkah 3 — Rig (tulang) + animasi

**Opsi A (disarankan untuk tablet):** pakai fitur **auto-rig / animation** di tool yang sama, jika tersedia untuk karakter humanoid.
**Opsi B:** **Mixamo** (Adobe, gratis). Unggah FBX/OBJ → pasang titik dagu, pergelangan, siku, lutut, selangkangan → auto-rig. *Mixamo adalah web app desktop; di tablet coba mode "Situs desktop" di browser. Jika tidak jalan, pakai Opsi A.*

Animasi yang dibutuhkan (cari yang mirip di library):
| Nama file | Isi | Wajib? |
|---|---|---|
| `Mega.fbx` | Model + skin + rig (pose diam) | **Wajib** |
| `Mega@Idle.fbx` | Berdiri bernapas | **Wajib** |
| `Mega@Run.fbx` | Lari (pilih **in place**, tidak maju sendiri) | **Wajib** |
| `Mega@Attack.fbx` | Pukul / dorongan tangan satu kali | Disarankan |
| `Mega@Skill.fbx` | Gerakan mengangkat tangan / cast | Disarankan |
| `Mega@Hit.fbx` | Terkena pukulan | Opsional |
| `Mega@Jump.fbx` | Lompat | Opsional |
| `Mega@Runtuh.fbx` | Jatuh / KO | Opsional |

Saat download dari Mixamo: format **FBX for Unity**. File pertama (`Mega.fbx`) pakai **With Skin**; file animasi boleh **Without Skin**. Centang **In Place** untuk Run.

**Nama file harus persis seperti tabel** (huruf besar-kecil sama, pakai `@`). Generator game mengenali animasi dari nama ini.

## Langkah 4 — Unggah ke GitHub dari browser tablet

1. Buka `github.com/umarlim31/konoha-power-clash`.
2. Pindah ke branch kerja yang sedang dipakai (lihat laporan Claude Code terakhir; setelah Prompt 1 biasanya branch 0.0.8.3).
3. **Add file → Upload files.**
4. Di kolom nama path, ketik folder: `Assets/Konoha/Art/Heroes/Mega/`, lalu unggah semua file FBX (dan PNG tekstur jika tool memberikannya terpisah).
5. Batas unggah web GitHub **25 MB per file**. Jika lebih, turunkan polycount/tekstur lalu export ulang.
6. Commit message: `art(mega): add first 3D model and animations`.

Setelah itu jalankan **Prompt 2** di Claude Code.

## Cara menilai hasil di game (setelah build Prompt 2)

- Dari jarak kamera normal: kelihatan jelas ibu berkebaya merah-hitam, berkacamata, dengan sanggul? (siluet > detail)
- Tidak ada kapsul lama yang terlihat.
- Saat joystick digerakkan: animasi lari; saat berhenti: idle; tidak meluncur seperti patung.
- Kaki tidak tenggelam atau melayang di lantai.
- Frame rate tidak turun terasa dibanding 0.0.8.3.
