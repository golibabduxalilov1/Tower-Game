# QALQON — loyiha va o‘zgarishlar tekshiruvi

Sana: 2026-10-21. Asos: joriy manba kod, foydalanuvchi bergan 38 bo‘limli TZ, qayta bajarilgan build/testlar va saqlangan hisobotlar.

Bu tekshiruvda o‘yin manba kodi o‘zgartirilmadi. Tekshiruv uchun alohida `artifacts/review-probes` dasturi, build loglari, UI loglari va skrinshot yaratildi. Papkada `.git` yo‘q: oldingi versiyaga nisbatan aniq diff yoki commit tarixini tiklab bo‘lmaydi. Quyidagi o‘zgarishlar tavsifi mavjud funksiyalar inventaridir.

## Natija

Loyiha dastlabki bo‘sh papkadan ishlaydigan C# + MonoGame ilovasiga aylangan. Windows oynasi ishga tushadi, Android loyihasi yig‘iladi. Asosiy mexanikalar va kontent mavjud. Biroq buzilgan saqlash va progress xatolari hamda telefon paneli o‘lchamlari sabab yakuniy TZ qabulini yopish mumkin emas.

## Tasdiqlangan topilmalar

### 1. [P1] Buzilgan saqlash konverti ilovani yopib yuboradi

Manba: `src/Game.Persistence/GameStore.cs:222` va `:225`; xatoni tutish filtri `:207`.

`Checksum: null` bo‘lsa `envelope.Checksum.Length` `NullReferenceException` beradi. `Data` yo‘q va checksum 64 belgidan iborat bo‘lsa `GetRawText()` `InvalidOperationException` beradi. `Read()` ikkala exceptionni ham tiklanadigan saqlash xatosi sifatida tutmaydi. Natijada standart sozlamalar yoki yaroqli `.bak`ga o‘tish bajarilmaydi. Bu `settings.json` orqali ilova ochilishini ham to‘sadi.

Izolyatsiyalangan tekshiruvda uchala holat takrorlandi: null checksum, yo‘q Data, yaroqli backup yonidagi null checksum. Dalil: `artifacts/review-probes.txt`.

Tavsiya: konvertning majburiy maydonlari va `JsonElement.ValueKind`ni ishlatishdan oldin tekshirish; yaroqsiz konvertni `InvalidDataException`ga aylantirish; primary/backup uchun ayni yo‘lni saqlash. Regression testlarda backup bilan va backupsiz holatlarni tekshirish.

### 2. [P2] Profil yozish xatosi keyingi jang saqlashi bilan yashiriladi

Manba: `src/Game.UI/DefenseGame.Battle.cs:153` va `:154`; `src/Game.Persistence/GameStore.cs:116`.

To‘lqin oxirida `RecordProgress()` profilni yozadi, ammo natija qaytarmaydi. So‘ng `SaveBattle()` jang faylini muvaffaqiyatli yozsa `LastError`ni tozalaydi. Interfeys faqat shu oxirgi operatsiyaning natijasini ko‘radi.

Tekshiruvda profil uchun yozish xatosi hosil qilindi: `RecordProgress()`dan keyin `save_failed`, jangni saqlagandan keyin `true` va `LastError=null`. Ilova qayta yaratilganda 50-to‘lqinda ochilishi kerak bo‘lgan ikkinchi xarita yopiq qoldi. Dalil: `artifacts/review-probes.txt`.

Tavsiya: progress operatsiyasining natijasini qaytarish va ikkala yozish natijasini tekshirish; muvaffaqiyatsiz progressni qayta yozish uchun belgilab qo‘yish. Xato to‘liq bartaraf etilmaguncha foydalanuvchi muvaffaqiyatli saqlash deb qabul qilmasin.

### 3. [P2] 16:9 telefon panelida minora tugmalari 48 dp dan kichik

Manba: `src/Game.UI/DefenseGame.Battle.cs:353`–`:354` va `:372`.

Qurilishni tasdiqlash/bekor qilish tugmalari uchun joy ajratilgach, qolgan maydon olti minora orasida bo‘linadi. `bw` uchun minimal sensor kengligi tekshirilmaydi.

Haqiqiy MonoGame oynasida `--scene=build --touch=3 --size=1920x1080` bilan takrorlandi. Minora kengligi 39 mantiqiy birlik, taxminan 87 piksel yoki 29 dp chiqadi; TZ kamida 48 dp talab qiladi. Narx yozuvlari ham yonma-yon tugmalar ustiga chiqadi. Dalil: `artifacts/review-phone-build.png`. Bu desktopda telefon ko‘rinishi sinovi; jismoniy Android touch sinovi emas.

Tavsiya: tasdiqlash tugmalarini alohida qatorga joylashtirish, panelni sahifalash/scroll qilish yoki moslashuvchi joylashuv qo‘llash; barcha asosiy touch maydonlarining ikkala o‘lchamini kamida 48 dp qilish. Test matritsasiga 16:9 va yuqori density qo‘shish.

## Mavjud arxitektura va funksiyalar

| Qism | Hozirgi vazifasi |
| --- | --- |
| Game.Core | 1/60 qadamli jang, har chiqishga BFS, yo‘lni saqlovchi atomik qurilish, fazoviy indeks va obyekt qayta ishlatish |
| Game.Content | JSON kontentni yuklash, xarita/to‘lqin validatsiyasi, tavsiflar |
| Game.Rendering | Kod orqali geometrik xarita, minora/dushman siluetlari, radius/yo‘l/effektlar, kamera |
| Game.UI | Menyu, xarita/rejim/qiyinlik, HUD, pauza, natija, yordam, sozlamalar, ikki til |
| Game.Audio | Ishga tushishda sintezlangan effektlar/musiqa, bir vaqtdagi ovozlarga limit |
| Game.Persistence | Alohida settings/profile/battle, SHA-256 nazorat qiymati, vaqtinchalik fayl, backup va v1/v2 konvert |
| Game.Platform | Saqlash, platforma safe area/touch hamda storefront chegaralari |
| Game.Windows | Windows x64 kirish nuqtasi, avtomatlashtirish parametrlari |
| Game.Android | Landscape activity, private storage, background saqlash, cutout ma’lumotlari |
| Game.Tests | Grafikaga bog‘liq bo‘lmagan yadro sinovlari |
| Game.IntegrationTests | Kontent, progress, saqlash, tillar va shrift tekshiruvlari |
| Game.Tools | Kampaniya generatori, validator, AutoPlayer balans sinovi va do‘kon grafikasi vositasi |
| Game.Steam | Ixtiyoriy Steamworks adapteri, yutuqlar/til, offline fallback |

Kontent: 8 ta 24×16 xarita, har birida 100 ta aniq to‘lqin; 8 minora × 3 daraja; 8 oddiy dushman va 3 boss; Classic/Extended/Endless; Easy/Normal/Hard. Kontentning joriy versiyasi `1.1.0`.

## Qayta bajarilgan tekshiruvlar

| Tekshiruv | Natija va dalil |
| --- | --- |
| Windows Release build | 0 warning, 0 error; `artifacts/review-build.txt` |
| Yadro testlari | 33/33; shu log ichida |
| Integratsion testlar | 17/17; shu log ichida |
| Kontent validatori | 8 xarita / 800 to‘lqin / route, enemy, tower, boss va population tekshiruvlari o‘tdi |
| Android Debug build | 0 warning, 0 error; `artifacts/review-android-debug-default.txt` |
| Android ARM64 Release build | `-r android-arm64` bilan 0 warning, 0 error; `artifacts/review-android-arm64-build.txt` |
| Desktop native UI skripti | Yangi izolyatsiyalangan profilda `SCRIPT PASS`, exit 0 |
| Telefon ko‘rinishi UI skripti | Yangi izolyatsiyalangan profilda `SCRIPT PASS`, exit 0 |
| Qo‘shimcha xato holatlari | Uch konvert crash holati va profil xatosining yashirilishi takrorlandi; standart testlar bularni qamramaydi |

Yangi UI loglari: `artifacts/review-ui-e2653fb72b164f6481100c57bac72e19/desktop-flow.txt.log` va `touch-flow.txt.log`. UI skriptlari ilova ichidagi input injeksiyasidan foydalanadi; ular jismoniy touch panel, OS klaviatura/fokus yoki Android hayot siklining barcha holatlarini tasdiqlamaydi.

Android Release uchun RID ko‘rsatilmagan dastlabki `--no-restore` urinishida XA0035 (`win-x64` ABI) chiqdi. Aniq `-r android-arm64` bilan build o‘tdi. Androidga oid ko‘paytiriladigan buyruqlarda shu RIDni aniq ko‘rsatish ma’qul.

## Oldindan mavjud balans dalillari

Quyidagilar shu tekshiruvda qayta o‘ynalmadi; repodagi hisobotlar o‘qildi:

- `balance-classic.txt`: AutoPlayer 24/24 xarita×qiyinlik kombinatsiyasida g‘alaba qilgan.
- `balance-extended.txt`: 23/24 g‘alaba; `crystal-mine / Hard` 10-to‘lqinda mag‘lub bo‘lgan.
- `balance-endless.txt`: 19/24 yugurish 150-to‘lqin chegarasiga yetgan; qolganlari tugagan to‘lqinni qayd etgan.

AutoPlayerning bir strategiyasi yutqazishi xarita yengib bo‘lmasligini isbotlamaydi. Ammo Extended uchun barcha kombinatsiya yengilishi haqidagi qabul mezoni ushbu dalillar bilan to‘liq yopilmagan.

## Nashr va hujjat holati

- `artifacts/android-arm64/uz.qalqon.defense-Signed.aab` bor. Sertifikat egasi `CN=Android Debug, O=Android, C=US`; bu publisher upload kaliti bilan tayyorlangan nashr emas.
- Windows executable build papkasida mavjud. `artifacts/windows-x64` tayyor self-contained distributivi tekshiruv boshida mavjud emas edi; `steam-check` boshqa tekshiruv chiqishi hisoblanadi.
- Steam adapteri mavjud, lekin haqiqiy App ID, yutuq backend sozlamalari va Cloud bilan runtime sinovi uchun dalil topilmadi. Kod havola qiladigan `docs/store/steam-setup.md` ham yo‘q.
- Haqiqiy past/o‘rta/yuqori Android qurilmalaridagi FPS, lock/resume va audio almashinuvi hisobotlari topilmadi. Yadro stress testi grafik/device FPS kafolati emas.
- Asosiy README/build-run qo‘llanma yo‘q; ishga tushirish buyruqlari `scripts/` ichida.
- `docs/task-list.md` hali Gate 1 kutilayotganini yozadi; bu joriy implementatsiya holatiga mos emas.
- `content/README.md` versiyani `1.0.0` deydi, kontent esa `1.1.0`.
- `docs/store/store-drafts.md` nomni Bastion Grid deydi; yangi listinglar va ilova nomi QALQON. Eskirgan materiallarni yangilash yoki arxivlash kerak.
- Git repository yo‘qligi sabab TZdagi versiya nazorati talabi hozir bajarilmagan.

## Tavsiya etilgan keyingi tartib

1. Konvert validatsiyasi va backupga o‘tishdagi crashlarni tuzatish; reproducerlarni regression testga aylantirish.
2. Progress/jang yozish natijalarini birga kuzatish va xatoni yashirmaslik.
3. Telefon panelini 48 dp bo‘yicha qayta joylashtirish va ekran matritsasini kengaytirish.
4. Extended `crystal-mine / Hard` uchun qo‘shimcha o‘ynash/balans dalili olish.
5. Hujjatlar, Git, Windows distributivi, publisher imzosi, haqiqiy qurilma va Steam tekshiruvlarini yakunlash.
