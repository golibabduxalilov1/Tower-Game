# Tower Defense — to‘liq o‘yin uchun ish rejasi

Manba: `C:\Users\Mr.Moon\Downloads\Tower_Defense_Texnik_Topshiriq.pdf`, 20-oktabr 2026, 38 bo‘lim, 19 sahifa.

Holat: reja tayyor; Gate 1 tasdig‘i kutilmoqda. Hali o‘yin kodi yozilmagan. Ko‘lam: **large**. Quyidagi dastlabki o‘ynaladigan bosqich yakuniy mahsulot o‘rnini bosmaydi; barcha bosqichlar to‘liq nashr uchun zarur.

## Mahkamlangan qarorlar

- C# + MonoGame; umumiy, oynasiz test qilinadigan yadro; Windows x64 va Android ARM64. .NET, MonoGame, Android SDK/JDK va vositalarning mos versiyalari texnik sinovdan keyin aniq mahkamlanadi.
- Original nom, xaritalar, geometrik 2D ko‘rinish va audio. Fieldrunners resurslari ishlatilmaydi. Tekstura, ikonka, animatsiya va oddiy tovushlar kod/yig‘ish vositasi orqali yaratiladi va qayta ishlatiladi. Unicode shrift litsenziyasi distributivga kiritiladi.
- 8 xarita; 8 minora × 3 daraja; 8 oddiy dushman; 3 boss; Easy/Normal/Hard; Classic/Extended/Endless; Classic va Extended uchun har xaritada 100 to‘lqin; o‘zbek va ingliz tillari.
- Bir mahalliy profil va bir faol jang. Asosiy o‘yin offline; multiplayer, chat, akkaunt, reklama, ichki xarid va reyting serveri yo‘q. Google Play/Steam bir martalik xarid; narxni egasi belgilaydi.
- 24×16 standart katak, 20 hayot, Normal 150 pul, 1 katakli minora, 10 soniya tanaffus. Birinchi to‘lqin faqat “Boshlash” bilan. 1×/2×, 1/60 soniya qat’iy hisoblash qadami; 2× qadamlar sonini oshiradi.
- Yig‘malar va tekshiruv natijalari mavjud dalil bilan qayd qilinadi. Haqiqiy qurilma, do‘kon imzosi yoki nashr tasdig‘i yo‘q bo‘lsa, ular bajarilgan deb belgilanmaydi.

## Majburiy modullar

| Modul | Mas’uliyat |
|---|---|
| Game.Core | Qoidalar, deterministik jang, iqtisod, holat mashinasi; MonoGame chizish/kiritishdan mustaqil |
| Game.Content | Xaritalar, minoralar, dushmanlar, versiyalangan to‘lqin va balans ma’lumotlari |
| Game.Rendering | SpriteBatch, kamera, tekstura keshi, animatsiya, chegaralangan effektlar |
| Game.UI | Menyu, HUD, yordam, ikki til va sozlamalar |
| Game.Audio | Koddan yaratilgan effektlar/musiqa, ovoz kanallari va cheklash |
| Game.Persistence | Alohida sozlama/profil/jang fayllari, checksum, atomik almashtirish, backup/migratsiya |
| Game.Platform | Saqlash, audio hayot sikli, ekran va xizmatlar interfeyslari |
| Game.Android | Android kirish nuqtasi, touch, safe area, hayot sikli |
| Game.Windows | Windows kirish nuqtasi, oyna, sichqoncha/klaviatura |
| Game.Tests | Yadro, kontent, integratsiya va saqlash testlari |
| Game.Tools | Resurs/to‘lqin yaratish, kontent tekshirish, benchmark va release yordamchilari |

Steam adapteri alohida modulda joylashadi; asosiy yadro Steam mavjudligiga bog‘lanmaydi.

## Ketma-ket vertikal bosqichlar

Har bosqichda ishlaydigan yakuniy yo‘l, tegishli testlar, generator/evaluator bahosi va review bo‘ladi. `gan-harness/spec.md` va `gan-harness/eval-rubric.md` TZga mos baholash manbai bo‘ladi. UI avtomatlashtirish vositasi native MonoGame oynasini tekshira olmasa, code-only baholashga qo‘shimcha native/manual dalil talab qilinadi; brauzer tekshiruvi bilan native o‘yin tayyor deb hisoblanmaydi.

### 1. Ikkala platformada birinchi o‘ynaladigan jang

- [ ] Solution va 11 modul; dependency versiya/litsenziya ro‘yxati; Windows va Android oyna, chizish, input, audio, shaxsiy katalogga yozish texnik sinovi.
- [ ] Menyu → Yashil vodiy → tayyorgarlik → jang → g‘alaba/mag‘lubiyat. Gatling, Goo, Missile, Tesla; dastlabki 3 dushman; 10 sinov to‘lqini.
- [ ] Qurilish, oddiy nishon tanlash, hayot/pul/HUD, boshlash, pauza va 1×/2×.
- [ ] Qabul: ikkala platformada kirishdan jang yakunigacha dalil; SDK/qurilma to‘sig‘i bo‘lsa aniq qayd; yadro oynasiz testdan o‘tadi. Ushbu bosqichdagi 10 to‘lqin yakuniy 100 to‘lqin talabi emas.

### 2. Yo‘lni o‘zgartirib himoyalanishning to‘liq sikli

- [ ] 7 katak turi: oddiy yer, faqat yo‘l, qoya, qurilish maydonchasi, kirish, chiqish, rels; dekor faqat vizual.
- [ ] Har chiqishga teskari BFS; 4 yo‘nalish; tenglikda joriy yo‘nalish, keyin doimiy tartib; faqat yurish imkoniyati o‘zgarganda qayta hisoblash.
- [ ] Katak markazlari orasida silliq harakat; teleportatsiyasiz yo‘l yangilash; birliklar bir-birini to‘smaydi; havo/rels waypoint harakati.
- [ ] Qurishda katak, bo‘shlik, pul, yer dushmani joyi va joriy segmenti hamda barcha kirish/aktiv dushman yo‘llari tekshiriladi. Rad etish mutatsiyasiz; qabul pul/minorani atomik o‘zgartiradi.
- [ ] Qabul: oxirgi yo‘lni yopish, faol dushmanni qamash, yetarli bo‘lmagan pul testlari; yo‘l preview va qisqa o‘zbek/ingliz xatolari.

### 3. To‘liq jang arsenali va iqtisod

- [ ] Barcha 8 minora, 3 daraja, nishon niqobi/minimal radius, taymer, joriy nishon va ustuvorlik.
- [ ] Gatling tez bitta zarba; Goo 2 s 35/45/55%; Missile tegishli qatlamda radius 1; Tesla 1,5 radiusdagi yana 2 nishonga 50/25%, takrorsiz; Flame 60° konus; Mortar otishdagi nuqta va radius 1,5, minimal radius 3; Laser eni 0,35; Ice 2,5 s 25/35/45% va zarar. Minoralar/dekor snaryadni to‘smaydi.
- [ ] Boshlang‘ich narx/zarar/oraliq/radius: Gatling 40/8/0,25/3; Goo 55/0/0,8/3; Missile 100/45/1,5/4,5; Tesla 150/70/1/3,5; Flame 120/12/0,2/2,5; Mortar 140/90/2,5/3–6; Laser 170/45/0,8/5; Ice 70/4/1/3.
- [ ] Upgrade: bazaviy narxning 75% va 125% qo‘shimcha xarajati; bazaga nisbatan zarar 1/1,7/2,8, oraliq 1/0,9/0,8, radius +0/+0,25/+0,5. Taymer reset/bepul zarba yo‘q. Birinchi to‘lqingacha 100% refund; keyin floor(jami sarf×0,70).
- [ ] Standart nishon qolgan yo‘l/joriy tezlik; yaqin/kuchli/zaif/chiqishga yaqin rejimlar; tenglik ID; yaroqli mavjud nishon saqlanadi.
- [ ] Barcha 8 oddiy dushman: Piyoda, Yuguruvchi, Og‘ir askar, Mototsikl, Tank, Vertolyot, Samolyot, Rels aravasi; TZdagi sog‘liq/tezlik/mukofot qiymatlari. Tank Gatling/Flame’dan 20% kam zarar.
- [ ] Bosslar: Og‘ir tank 1500/0,45 va 25% qarshilik; Havo qal’asi 1200/0,7; Rels kolossi 1800/0,6. Mukofot 100; bossga slow yarim, kolossga yo‘q; oddiy chiqish −1, boss −3 hayot.
- [ ] Zarar daraja×qarshilik; kuchli slow amal qiladi, muddatlar alohida; har o‘limga faqat bir mukofot. O‘lgan/chiqqan nishonga kech snaryad mukofot bermaydi; oddiy bekor, Mortar nuqtaga boradi; AoE bir hujumda bir marta.
- [ ] Qabul: geometriya, rezist, slow, reward, refund, upgrade va nishon barqarorligi avtomatik testlari; barcha qurollar UI orqali qo‘llanadi.

### 4. Sakkiz xarita va to‘liq kampaniya

- [ ] Aniq koordinatali original xaritalar: Yashil vodiy; Ikki oqim; Qum qal’asi; Kristall kon; Tosh darvozalar; Muz markazi; Botqoq chegarasi; Vulkan halqasi. TZdagi kirishlar, kesishmalar, markaziy chiqish, to‘siq, havo va 1/2 rels sxemalari saqlanadi.
- [ ] Xarita ID/nom/tavsif/o‘lcham/katak/kirish-chiqish bog‘lanishi/havo-rels/minora-dushman cheklovi/to‘lqin/mavzu/ochilish sharti kontentda.
- [ ] Har xaritada 100 versiyalangan to‘lqin; dushman turi/son/kirish/kechikish/interval/HP koeffitsiyent/boss/yakun mukofoti. N=min(60,8+floor(0,6w)); H=(1+0,075(w−1))×1,012^(w−1); T=max(0,22;0,9−0,006w) dastlabki generator, keyin faylga saqlash va balans sinovi.
- [ ] Ko‘p kirishda umumiy N bo‘linadi; har 10-to‘lqin yaroqli boss, oldindan HUD ogohlantirishi; spawn navbati va barcha dushman tugagandagina to‘lqin yakuni.
- [ ] Easy HP×0,8/pul180; Normal ×1/150; Hard ×1,35/130. Classic asosiy 4; Extended/Endless xaritaga mos yana 2; jami kontentda barcha 8 ishlatiladi.
- [ ] 100-to‘lqinda hayot>0 g‘alaba; hayot 0 darhol mag‘lubiyat, natija bir marta. 10 s tanaffus, erta chaqirish bonussiz. Wave bonus 10+floor(w/5); kill ball=reward×10, wave=100, g‘alaba hayot×500.
- [ ] Qabul: 8 xaritaning kirish/chiqish/yo‘l/rels/havo va qurol qamrovi validatorlari; ketma-ket 100 to‘lqin testlari; barcha xarita/qiyinlik yengilishi amaliy dalil bilan alohida tekshiriladi.

### 5. Davomli o‘yin, Endless va progress

- [ ] Birinchi xarita ochiq; oldingining 50-to‘lqini yakunida keyingi ochiladi; Classic g‘alabasi shu xaritadagi Extended/Endlessni ochadi.
- [ ] G‘alabada 20 hayot=3, 10–19=2, 1–9=1 yulduz; jangga bonus bermaydi. Rekordlar xarita/rejim/qiyinlik kesimida; pul, ball, yulduz ajratilgan.
- [ ] Endless 100 dan so‘ng davom etadi, N≤60, boss har 10; cheklangan/saturatsiyalangan hisoblar overflow bermaydi; katta sonlar qisqa; seed va generator holati tiklanadi.
- [ ] Settings, profil/progress, faol jang, backup alohida; versiya/app versiya/vaqt/data/checksum; temp yozish→tekshirish→almashtirish; eski format migratsiyasi va buzilishda backup.
- [ ] To‘lqin oxiri, pauzadan chiqish, Saqlash va chiqish, Android background saqlashi; barcha jang holati/taymer/snaryad/slow/seed tiklanadi. Muvaffaqiyatsiz saqlash yaroqli nusxani yo‘qotmaydi.
- [ ] Qabul: save/resume ekvivalentligi, buzilgan/katta/noo‘rin qiymatli fayl, migratsiya, disk to‘lishi; uzun Endless; bir xil seed+buyruq va 1×/2× deterministik natijalar.

### 6. To‘liq boshqaruv, ko‘rinish, ovoz va lokalizatsiya

- [ ] Barcha 11 holat: yuklanish, menyu, xarita, rejim/qiyinlik, tayyorgarlik, faol to‘lqin, tanaffus, pauza, g‘alaba, mag‘lubiyat, tiklash xatosi; ikki jang/natija oynasi bir vaqtda yo‘q.
- [ ] Android minora tanlash→katak preview→tasdiq, bekor, minora menyusi; pinch zoom va 2 barmoq pan; Back avval oynani yopadi, keyin pauza; ≥48 dp; landscape/safe area.
- [ ] Windows chap tanlash/tasdiq, o‘ng/Esc bekor/yopish, wheel zoom, middle pan, Space pause, 1–6 minora, U upgrade, Delete sotish tasdig‘i, F tezlik; qayta tayinlanadigan klavishlar.
- [ ] HUDdagi barcha ko‘rsatkichlar va to‘liq minora statistikasi/nishon rejimi; yordamda rezistlar; o‘zbek/ingliz matnlar, xatoda ichki yo‘l/texnik detalsiz.
- [ ] Tur/daraja siluetlari, radius/yo‘l rejimi, sog‘liq chiziqlari, o‘qiladigan effektlar; shake/flash o‘chirish; 16:9/16:10/keng ekranlarda nisbat saqlanishi.
- [ ] Gatling/raketa/portlash/elektr/olov/qurish/upgrade/xato/wave/g‘alaba/mag‘lubiyat effektlari; ovoz soni limiti; master/music/effects/mute; backgroundda ovoz to‘xtaydi.
- [ ] Qabul: barcha menyular ikki tilda, Unicode gliflar; touch/mouse/key, kichik/katta ekran, qayta ochish, ovoz chiqishi almashishi qo‘lda tekshiriladi.

### 7. Balans, xavfsizlik va haqiqiy qurilma barqarorligi

- [ ] Pooling, fazoviy indeks, BFS/tekstura kesh, effect limiti, hot loop ajratmalarini kamaytirish, ko‘rinmas dekorni chizmaslik.
- [ ] 300 dushman+200 minora+600 snaryad/effekt, 2× stress; 60 FPS maqsad va 30 FPS sozlama. Haqiqiy past/o‘rta/yuqori Android va integratsiyalashgan GPUli Windowsda vaqt/xotira/FPS hisobotlari.
- [ ] Lock/unlock, background/resume, internet yo‘q, Steam yo‘q, resize/fullscreen, buzilgan save/disk yetishmasligi; bir soatlik yoki hujjatlashtirilgan uzun soak.
- [ ] Faqat zarur ruxsatlar, private storage, bounded input, fayldan kod bajarmaslik, PII yo‘q loglar, secrets Gitdan tashqarida. Persistence/platform/input/Steam uchun security-review.
- [ ] Qabul: har xarita×qiyinlik bo‘yicha amaliy g‘alaba dalili; muhim testlar o‘tgan, muhim kod coverage maqsadi ≥80%; ochiq CRITICAL/HIGH va release blocker yo‘q.

### 8. Release va topshirish

- [ ] Windows x64 release oddiy huquqlar bilan; Android ARM64 release va imzolangan AAB; nashr vaqtida amaldagi Play target API/native mosligi tekshiriladi.
- [ ] Steam adapter: yutuqlar, profil/progress Cloud, dastlabki til; offline fallback. Faol jang platformalararo sinxronlanmaydi.
- [ ] Original nom/ikonka, Play tavsif/skrinshot/yosh savollari/data declaration/privacy/support; Steam kapsula/skrinshot/tavsif/tizim talabi/support.
- [ ] Manba, projectlar, kontent/balans, generatorlar, testlar, build/run qo‘llanma, ikkala release, test/performance hisobotlari, third-party licenses, known issues ro‘yxati.
- [ ] Qabul: TZ 36-bo‘limidagi 12 mezon dalillar bilan yopilgan. Store qabul qilishi tashqi tekshiruv; egasining akkaunt/imzo/nashr tasdig‘i talab qilinadi.

## Xatarlar va tashqi bog‘liqliklar

Boshlang‘ich muhit tekshiruvi: .NET 10.0.13 runtime mavjud, lekin .NET SDK yo‘q; Java va adb PATHda topilmadi. Qurish boshlanishidan oldin SDK va Android toolchain o‘rnatilishi/aniqlanishi kerak. Runtime mavjudligi release yaratish imkoniyati deb hisoblanmaydi.

| Xatar/bog‘liqlik | Yondashuv |
|---|---|
| SDK/JDK/MonoGame Android mosligi, native talablari | Birinchi bosqichda ikki platforma spike; tekshirilgan versiyalar lock; keyin release qayta tekshiruv |
| Android qurilma/USB/emulyator va Windows GPU | Mavjud vosita inventari; real qurilma dalili yetishmasa mezon ochiq qoladi |
| Steamworks akkaunt/App ID/SDK, Google Play akkaunti | Egasi taqdim etadigan tashqi bog‘liqlik; soxta integratsiyani tayyor deb ko‘rsatmaslik |
| Android signing keystore/parol, store publication | Kalitni repoga yozmaslik; egasi alohida saqlaydi; nashr yakuniy tasdiq bilan |
| 8×3 xarita/qiyinlik va ikki 100-wave rejim balansi | Avtomatik stressdan tashqari real o‘ynash matritsasi va sozlanadigan kontent |
| Unicode shrift tarqatish huquqi | Mos litsenziyali shrift, o‘zbek belgilarini tekshirish, litsenziyani qo‘shish |
| Foizli upgrade narxining yaxlitlanishi TZda noaniq | Kontentda butun narxlarni aniq belgilash va hujjatlash; barcha sarf/refund bir manbadan |
| Faol jangning aniq tiklanishi | To‘liq deterministik snapshot, timer/ID/projectile/effect/seed serialization |
| Hostda GAN slash-command yoki native UI avtomatlashtirish yo‘q | Mavjud harness fayllari va generator/evaluator agentlarini ishlatish; native dalil bo‘lmasa cheklovni ochiq yozish |

## Skill darvozalari

Gate 1: ushbu reja foydalanuvchi tomonidan tasdiqlangach implementatsiya boshlanadi. Manba: `orch-pipeline/SKILL.md`: “Present the task_list; do not write implementation code until the user approves.”

Gate 2: scaffold va mantiqiy bo‘laklar uchun alohida `feat:` commit takliflari, diff va test natijalari ko‘rsatiladi; commit faqat foydalanuvchi tasdig‘idan keyin. Hozir commit qilinmaydi.
