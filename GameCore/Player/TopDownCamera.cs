namespace DefaultNamespace;

public class TopDownCamera
{
   <!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Алексей и Ирина — 3 сентября 2026</title>
<!-- ШРИФТЫ: Cormorant (дисплейный, кириллица) + Marck Script (рукописный) + Jost (текстовый) -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Cormorant:ital,wght@0,400;0,500;0,600;1,400;1,500&family=Jost:wght@300;400;500&family=Marck+Script&display=swap" rel="stylesheet">
<style>
:root{
  --bg:#faf4e9;                      /* кремовый фон */
  --bg2:#f3eadb;
  --ink:#4b3d30;                     /* основной текст — тёплый коричневый */
  --ink-soft:#8a7b69;                /* приглушённый текст */
  --gold:#b08d57;                    /* золото, читаемое на светлом */
  --gold-soft:rgba(176,141,87,.35);
  --blush:#c98d84;                   /* пудрово-розовый акцент */
  --card:rgba(255,255,255,.55);      /* подложки карточек */
}
*{margin:0;padding:0;box-sizing:border-box}
html{scroll-behavior:smooth}
html,body{overflow-x:hidden}
body{background:radial-gradient(140% 100% at 50% 0%,#fdf9f1 0%,var(--bg) 45%,var(--bg2) 100%) fixed;
  color:var(--ink);font-family:"Jost",sans-serif;font-weight:300;line-height:1.7}
body.locked{overflow:hidden}
::selection{background:var(--gold);color:#fff}

/* зерно поверх всего */
body::before{content:"";position:fixed;inset:0;z-index:40;pointer-events:none;opacity:.05;
  background:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.9' numOctaves='2'/%3E%3C/filter%3E%3Crect width='160' height='160' filter='url(%23n)'/%3E%3C/svg%3E")}

#petals{position:fixed;inset:0;z-index:0;pointer-events:none}
main{position:relative;z-index:1}

/* пометки-заполнители: убрать класс .todo, когда впишете реальные данные */
.todo{outline:2px dashed rgba(201,141,132,.65);outline-offset:5px;border-radius:2px}

/* ---------- ЭКРАН-КОНВЕРТ ---------- */
.scene{position:fixed;inset:0;z-index:50;display:grid;place-items:center;gap:26px;
  background:radial-gradient(120% 120% at 50% 30%,#fdf8ee 0%,#ecdfc9 70%);
  transition:opacity .7s ease,visibility 0s .7s}
.scene.gone{opacity:0;visibility:hidden}
.scene-script{font-family:"Marck Script";font-size:clamp(1.5rem,4vw,2rem);color:var(--blush)}
.scene-hint{font-size:11px;letter-spacing:.35em;text-transform:uppercase;color:var(--ink-soft)}
.envelope{width:min(86vw,340px);aspect-ratio:34/22;position:relative;perspective:1000px;cursor:pointer;background:none;border:0}
.env-back{position:absolute;inset:0;border-radius:8px;background:linear-gradient(160deg,#f0e4cd,#e0cfa9);box-shadow:0 34px 70px -20px rgba(120,95,55,.4)}
.env-card{position:absolute;left:5%;right:5%;top:32%;bottom:6%;background:#fbf6ea;border:1px solid rgba(176,141,87,.35);border-radius:4px;display:grid;place-items:center;z-index:3;transition:transform 1s cubic-bezier(.22,.8,.24,1) .45s}
.env-card b{font-family:"Marck Script";font-weight:400;font-size:32px;color:var(--gold)}
.env-flap{position:absolute;left:0;right:0;top:0;height:56%;background:linear-gradient(180deg,#eadcc0,#d9c69e);clip-path:polygon(0 0,100% 0,50% 100%);transform-origin:top center;z-index:6;transition:transform .85s cubic-bezier(.6,.05,.3,1)}
.env-pocket{position:absolute;inset:0;border-radius:8px;background:linear-gradient(20deg,#f2e7d2,#e3d2b0);clip-path:polygon(0 100%,0 26%,50% 64%,100% 26%,100% 100%);z-index:4}
.env-seal{position:absolute;left:50%;top:54%;translate:-50% -50%;width:66px;height:66px;border-radius:50%;z-index:7;
  background:radial-gradient(circle at 35% 28%,#d9a29a,#b9766c 72%);color:#fdf6ec;display:grid;place-items:center;
  font-family:"Marck Script";font-size:23px;box-shadow:0 10px 20px rgba(150,90,80,.35),inset 0 0 0 3px rgba(255,255,255,.4);
  transition:opacity .35s,scale .35s;animation:seal 2.6s ease-in-out infinite}
@keyframes seal{50%{box-shadow:0 10px 20px rgba(150,90,80,.35),0 0 0 12px rgba(201,141,132,.18),inset 0 0 0 3px rgba(255,255,255,.45)}}
.scene.open .env-flap{transform:rotateX(180deg);z-index:1}
.scene.open .env-seal{opacity:0;scale:.5}
.scene.open .env-card{transform:translateY(-80%)}

/* ---------- ОБЩЕЕ ---------- */
section{padding:clamp(72px,12vh,120px) 24px}
.wrap{max-width:1000px;margin:0 auto}
.eyebrow{font-family:"Marck Script";font-size:1.7rem;color:var(--gold);margin-bottom:14px}
h2{font-family:"Cormorant",serif;font-weight:500;font-size:clamp(1.7rem,4vw,2.4rem);letter-spacing:.14em;text-transform:uppercase;margin-bottom:14px}
.divider{display:flex;align-items:center;justify-content:center;gap:14px;margin:16px auto 0;color:var(--gold)}
.divider i{display:block;width:56px;height:1px;background:var(--gold-soft)}
.leaf{width:26px;height:13px;fill:currentColor;opacity:.9}
.reveal{opacity:0;translate:0 28px;transition:opacity .9s ease,translate .9s cubic-bezier(.2,.7,.2,1)}
.reveal.in{opacity:1;translate:0 0}
.center{text-align:center}

/* ---------- ГЛАВНЫЙ ЭКРАН ---------- */
.hero{min-height:100svh;display:grid;place-items:center;text-align:center;position:relative;overflow:hidden}
.halo{position:absolute;inset:0;display:grid;place-items:center;pointer-events:none}
.halo i{position:absolute;border-radius:50%;border:1px solid rgba(176,141,87,.24)}
.halo i:nth-child(1){width:min(72vw,540px);aspect-ratio:1;animation:spin 80s linear infinite}
.halo i:nth-child(1)::before{content:"";position:absolute;top:-4px;left:50%;width:7px;height:7px;background:var(--gold);transform:rotate(45deg)}
.halo i:nth-child(2){width:min(88vw,660px);aspect-ratio:1;border-style:dashed;border-color:rgba(176,141,87,.18);animation:spin 110s linear infinite reverse}
@keyframes spin{to{transform:rotate(360deg)}}
.hero-inner{position:relative;padding:24px}
.monogram{width:76px;height:76px;border:1px solid var(--gold-soft);border-radius:50%;display:grid;place-items:center;margin:0 auto 30px;font-family:"Marck Script";font-size:26px;color:var(--gold)}
.mask{display:block;overflow:hidden;padding:.06em 0}
.mask>span{display:block;transform:translateY(115%);transition:transform 1.05s cubic-bezier(.19,.8,.22,1);transition-delay:var(--d,0s)}
body.opened .mask>span{transform:none}
.hero-names{font-family:"Cormorant",serif;font-weight:500;font-size:clamp(2.6rem,9vw,5rem);line-height:1.08;letter-spacing:.18em;text-transform:uppercase;color:var(--ink)}
.hero-names .amp{font-family:"Marck Script";font-size:.62em;color:var(--gold);letter-spacing:0;margin-right:.3em}
.hero-script{margin-top:26px;font-family:"Marck Script";font-size:clamp(1.4rem,3.4vw,1.9rem);color:var(--blush)}
.hero-date{margin-top:26px;display:flex;align-items:center;justify-content:center;gap:16px;font-family:"Cormorant",serif;font-size:clamp(1.1rem,2.6vw,1.4rem);letter-spacing:.3em;text-transform:uppercase;color:var(--gold)}
.hero-date span{width:44px;height:1px;background:var(--gold-soft)}
.hero-place{margin-top:10px;font-size:12px;letter-spacing:.3em;text-transform:uppercase;color:var(--ink-soft)}
.cue{position:absolute;bottom:26px;left:50%;translate:-50%;width:1px;height:56px;background:var(--gold-soft);overflow:hidden}
.cue::after{content:"";position:absolute;left:0;top:-40%;width:100%;height:40%;background:var(--gold);animation:cue 2.2s ease-in-out infinite}
@keyframes cue{to{top:110%}}

/* ---------- ТАЙМЕР ---------- */
.count{display:flex;justify-content:center;gap:clamp(10px,3vw,26px);flex-wrap:wrap;margin-top:40px}
.cell{min-width:88px;padding:18px 10px;border:1px solid var(--gold-soft);background:var(--card);transition:.35s}
.cell:hover{border-color:var(--gold);transform:translateY(-4px);box-shadow:0 14px 26px -16px rgba(150,120,70,.5)}
.cell b{display:block;font-family:"Cormorant",serif;font-weight:500;font-size:clamp(2rem,5vw,2.8rem);color:var(--ink)}
.cell small{font-size:10px;letter-spacing:.3em;text-transform:uppercase;color:var(--ink-soft)}

/* ---------- ПРОГРАММА ДНЯ ---------- */
.timeline{position:relative;max-width:660px;margin:44px auto 0;padding-left:56px}
.timeline::before{content:"";position:absolute;left:20px;top:8px;bottom:8px;width:1px;background:linear-gradient(var(--gold-soft),rgba(176,141,87,.06))}
.t-item{position:relative;padding:0 0 44px;transition:transform .4s}
.t-item:hover{transform:translateX(8px)}
.t-item:last-child{padding-bottom:0}
.t-dot{position:absolute;left:-56px;top:2px;width:41px;height:41px;border:1px solid var(--gold-soft);border-radius:50%;display:grid;place-items:center;color:var(--gold);background:var(--bg);transition:.35s}
.t-item:hover .t-dot{border-color:var(--gold);box-shadow:0 0 0 6px rgba(176,141,87,.12)}
.t-dot svg{width:20px;height:20px;stroke:currentColor;fill:none;stroke-width:1.3;stroke-linecap:round;stroke-linejoin:round}
.t-time{font-family:"Cormorant",serif;font-size:1.5rem;letter-spacing:.1em;color:var(--gold)}
.t-item h3{font-family:"Cormorant",serif;font-weight:600;font-size:1.25rem;letter-spacing:.06em;margin:2px 0 6px}
.t-item p{color:var(--ink-soft);max-width:46ch}

/* ---------- МЕСТО ---------- */
.venue{border:1px solid var(--gold-soft);padding:clamp(34px,6vw,56px);text-align:center;position:relative;background:var(--card)}
.venue .corner{position:absolute;width:26px;height:26px;border-color:var(--gold);border-style:solid;border-width:0}
.c-tl{top:-1px;left:-1px;border-top-width:1px;border-left-width:1px}
.c-br{bottom:-1px;right:-1px;border-bottom-width:1px;border-right-width:1px}
.venue h3{font-family:"Cormorant",serif;font-weight:500;font-size:clamp(1.5rem,3.4vw,2rem);letter-spacing:.08em;margin:14px 0 6px}
.venue .addr{color:var(--ink-soft)}
.btn{display:inline-block;margin-top:28px;padding:15px 36px;border:1px solid var(--gold);color:var(--gold);text-decoration:none;font-size:12px;letter-spacing:.28em;text-transform:uppercase;transition:.35s;background:transparent;cursor:pointer;font-family:"Jost",sans-serif}
.btn:hover{background:var(--gold);color:#fff;transform:translateY(-2px);box-shadow:0 12px 28px -12px rgba(176,141,87,.7)}
.transfer{margin-top:22px;font-size:13px;letter-spacing:.06em;color:var(--ink-soft)}

/* ---------- ДРЕСС-КОД ---------- */
.swatches{display:flex;justify-content:center;gap:clamp(14px,3vw,26px);flex-wrap:wrap;margin-top:36px}
.sw{display:grid;justify-items:center;gap:10px}
.sw i{width:56px;height:56px;border-radius:50%;background:var(--c);box-shadow:inset -6px -8px 14px rgba(0,0,0,.12),0 0 0 1px rgba(0,0,0,.06);transition:.35s}
.sw:hover i{transform:scale(1.15) rotate(6deg);box-shadow:inset -6px -8px 14px rgba(0,0,0,.12),0 0 0 4px rgba(176,141,87,.3)}
.sw small{font-size:11px;letter-spacing:.2em;text-transform:uppercase;color:var(--ink-soft)}

/* ---------- ДЕТАЛИ ---------- */
.details-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:18px;margin-top:40px}
.d-card{border:1px solid rgba(176,141,87,.22);border-top:2px solid var(--gold);padding:28px 24px;background:var(--card);transition:.35s}
.d-card:hover{transform:translateY(-6px);border-top-color:var(--blush)}
.d-card .em{font-size:26px}
.d-card h3{font-family:"Cormorant",serif;font-weight:600;font-size:1.2rem;margin:10px 0 6px;letter-spacing:.04em}
.d-card p{font-size:.95rem;color:var(--ink-soft)}

/* ---------- RSVP ---------- */
.rsvp-card{max-width:620px;margin:40px auto 0;border:1px solid var(--gold-soft);padding:clamp(28px,5vw,48px);background:var(--card);position:relative;overflow:hidden}
.field{margin-bottom:26px}
label.lbl{display:block;font-size:11px;letter-spacing:.28em;text-transform:uppercase;color:var(--ink-soft);margin-bottom:10px}
input[type=text]{width:100%;background:transparent;border:0;border-bottom:1px solid rgba(176,141,87,.35);color:var(--ink);font:300 1.05rem "Jost",sans-serif;padding:10px 2px;transition:border-color .3s}
input[type=text]::placeholder{color:#bdae9a}
input[type=text]:focus{outline:none;border-color:var(--gold)}
.chips{display:flex;flex-wrap:wrap;gap:10px}
.chip input{position:absolute;opacity:0}
.chip span{display:inline-block;padding:9px 18px;border:1px solid rgba(176,141,87,.35);border-radius:999px;font-size:13px;letter-spacing:.06em;cursor:pointer;transition:.3s;user-select:none;color:var(--ink)}
.chip input:checked+span{background:var(--gold);color:#fff;border-color:var(--gold)}
.chip input:focus-visible+span{outline:2px solid var(--gold);outline-offset:2px}
.chip span:hover{border-color:var(--gold)}
.rsvp-note{margin-top:26px;font-size:13px;color:var(--ink-soft);text-align:center}
.rsvp-done{text-align:center;padding:20px 0;position:relative}
.rsvp-done .big{font-family:"Marck Script";font-size:2rem;color:var(--gold);margin:14px 0 6px}
.petal-bit{position:absolute;left:50%;top:50%;width:10px;height:6px;border-radius:60% 40% 60% 40%;pointer-events:none;animation:fly 1s ease-out forwards}
@keyframes fly{to{transform:translate(var(--dx),var(--dy)) rotate(var(--r));opacity:0}}

/* ---------- ФИНАЛ ---------- */
.finale{text-align:center;padding-bottom:90px}
.finale .script{font-family:"Marck Script";font-size:clamp(2rem,5vw,2.8rem);color:var(--gold)}
.finale .names{margin-top:12px;letter-spacing:.3em;text-transform:uppercase;font-size:12px;color:var(--ink-soft)}

@media (max-width:560px){
  .timeline{padding-left:50px}.timeline::before{left:16px}
  .t-dot{left:-50px;width:34px;height:34px}
  .cell{min-width:70px}
}

/* ---------- REDUCED MOTION ---------- */
@media (prefers-reduced-motion:reduce){
  *,*::before,*::after{animation:none!important;transition:none!important}
  .mask>span{transform:none}
  .reveal{opacity:1;translate:0 0}
  html{scroll-behavior:auto}
}
</style>
</head>
<body class="locked">

<canvas id="petals" aria-hidden="true"></canvas>

<!-- ЭКРАН С КОНВЕРТОМ -->
<div class="scene" id="scene">
  <p class="scene-script">Для самых близких</p>
  <button class="envelope" id="envelope" aria-label="Открыть приглашение">
    <span class="env-back"></span>
    <span class="env-card"><b>А ♥ И</b></span>
    <span class="env-flap"></span>
    <span class="env-pocket"></span>
    <span class="env-seal">А·И</span>
  </button>
  <p class="scene-hint">нажмите на печать, чтобы открыть</p>
</div>

<main>
  <!-- ГЛАВНЫЙ ЭКРАН -->
  <section class="hero" id="top">
    <div class="halo" aria-hidden="true"><i></i><i></i></div>
    <div class="hero-inner">
      <div class="monogram">А·И</div>
      <p class="hero-script mask"><span style="--d:.1s">Мы решили пожениться и зовём вас разделить этот день</span></p>
      <h1 class="hero-names">
        <span class="mask"><span style="--d:.25s">Алексей</span></span>
        <span class="mask"><span style="--d:.4s"><span class="amp">и</span>Ирина</span></span>
      </h1>
      <div class="hero-date"><span></span>03 · 09 · 2026<span></span></div>
      <!-- ЗАПОЛНИТЕЛЬ: впишите город/площадку и уберите класс todo -->
      <p class="hero-place todo">ЗАМЕНИ МЕНЯ НА АКТУАЛЬНОЕ</p>
    </div>
    <div class="cue" aria-hidden="true"></div>
  </section>

  <!-- ТАЙМЕР -->
  <section class="center">
    <div class="wrap reveal">
      <p class="eyebrow">считаем дни…</p>
      <h2>До встречи осталось</h2>
      <div class="count" id="count" role="timer" aria-live="off">
        <div class="cell"><b id="cd-d">—</b><small>дней</small></div>
        <div class="cell"><b id="cd-h">—</b><small>часов</small></div>
        <div class="cell"><b id="cd-m">—</b><small>минут</small></div>
        <div class="cell"><b id="cd-s">—</b><small>секунд</small></div>
      </div>
    </div>
  </section>

  <!-- ПРОГРАММА ДНЯ (будем менять — присылайте новый сценарий) -->
  <section>
    <div class="wrap reveal">
      <p class="eyebrow">как пройдёт день</p>
      <h2>Программа праздника</h2>
      <div class="divider"><i></i>
        <svg class="leaf" viewBox="0 0 26 13" aria-hidden="true"><path d="M13 13C9 8 9 3 13 0c4 3 4 8 0 13z"/></svg>
        <i></i></div>
    </div>
    <div class="timeline">
      <div class="t-item reveal">
        <span class="t-dot"><svg viewBox="0 0 24 24"><path d="M7 3h4l-.5 6a2.6 2.6 0 0 1-3 0L7 3zm2.2 10V21M6 21h6M14.5 3H19l-.6 6a2.4 2.4 0 0 1-3.4 0L14.5 3z"/></svg></span>
        <div class="t-time">15:30</div><h3>Сбор гостей</h3>
        <p>Велком-фуршет и лимонады на террасе, лёгкая музыка и первые объятия.</p>
      </div>
      <div class="t-item reveal">
        <span class="t-dot"><svg viewBox="0 0 24 24"><circle cx="9" cy="14" r="5"/><circle cx="15" cy="14" r="5"/><path d="M15 5l1.6-2M15 5l-1.6-2"/></svg></span>
        <div class="t-time">16:30</div><h3>Церемония</h3>
        <p>Самые важные слова — под открытым небом, у старой липовой аллеи.</p>
      </div>
      <div class="t-item reveal">
        <span class="t-dot"><svg viewBox="0 0 24 24"><path d="M6 3v6a2 2 0 0 0 4 0V3M8 3v18M16 3c-2 0-2 3-2 5s1 3 2 3v10"/></svg></span>
        <div class="t-time">18:00</div><h3>Банкет</h3>
        <p>Ужин в оранжерее, тосты, истории и немного слёз счастья.</p>
      </div>
      <div class="t-item reveal">
        <span class="t-dot"><svg viewBox="0 0 24 24"><path d="M5 21h14M6 21v-8h12v8M9 13V9h6v4M12 9V6M12 6c0-1 .8-1.6.8-2.4"/></svg></span>
        <div class="t-time">22:00</div><h3>Торт и танцы</h3>
        <p>Сладкий финал вечера и танцы до последней песни.</p>
      </div>
    </div>
  </section>

  <!-- МЕСТО: все заполнители помечены классом todo -->
  <section>
    <div class="wrap reveal">
      <p class="eyebrow center">куда приезжать</p>
      <h2 class="center">Место проведения</h2>
      <div class="venue">
        <i class="corner c-tl"></i><i class="corner c-br"></i>
        <p class="eyebrow" style="margin:0">площадка</p>
        <!-- ЗАПОЛНИТЕЛЬ -->
        <h3 class="todo">«ЗАМЕНИ МЕНЯ НА АКТУАЛЬНОЕ»</h3>
        <!-- ЗАПОЛНИТЕЛЬ -->
        <p class="addr todo">ЗАМЕНИ МЕНЯ: город, улица, дом</p>
        <!-- ЗАМЕНИТЕ И ТЕКСТ ЗАПРОСА В ССЫЛКЕ НА РЕАЛЬНЫЙ АДРЕС -->
        <a class="btn" target="_blank" rel="noopener" href="https://yandex.ru/maps/?text=%D0%97%D0%B0%D0%BC%D0%B5%D0%BD%D0%B8%20%D0%BC%D0%B5%D0%BD%D1%8F">Открыть на карте</a>
        <!-- ЗАПОЛНИТЕЛЬ -->
        <p class="transfer todo">Трансфер и парковка: ЗАМЕНИ МЕНЯ НА АКТУАЛЬНОЕ</p>
      </div>
    </div>
  </section>

  <!-- ДРЕСС-КОД -->
  <section class="center">
    <div class="wrap reveal">
      <p class="eyebrow">что надеть</p>
      <h2>Дресс-код</h2>
      <p style="max-width:52ch;margin:0 auto;color:var(--ink-soft)">Мы будем рады, если ваши наряды поддержат палитру вечера — так фотографии станут ещё красивее.</p>
      <div class="swatches">
        <div class="sw"><i style="--c:#f0e6d2"></i><small>крем</small></div>
        <div class="sw"><i style="--c:#d8b98b"></i><small>золото</small></div>
        <div class="sw"><i style="--c:#9aa88a"></i><small>шалфей</small></div>
        <div class="sw"><i style="--c:#e3b3a9"></i><small>пудра</small></div>
        <div class="sw"><i style="--c:#43594e"></i><small>хвоя</small></div>
      </div>
    </div>
  </section>

  <!-- ДЕТАЛИ -->
  <section>
    <div class="wrap">
      <div class="reveal"><p class="eyebrow">мелочи, которые важны</p><h2>Вопросы и ответы</h2></div>
      <div class="details-grid">
        <div class="d-card reveal"><div class="em">🍾</div><h3>Цветы</h3><p>Не тратьте деньги на букеты — лучше принесите бутылку вина с тёплыми словами, мы соберём семейный погреб.</p></div>
        <div class="d-card reveal"><div class="em">🎁</div><h3>Подарки</h3><p>Главный подарок — вы с нами. Если хочется большего, мы будем рады вкладу в наше свадебное путешествие.</p></div>
        <div class="d-card reveal"><div class="em">🥂</div><h3>Формат</h3><p>Вечер взрослый: детей в этот день мы доверим бабушкам и дедушкам.</p></div>
        <div class="d-card reveal"><div class="em">🚕</div><h3>Логистика</h3><p>Организуем трансфер туда и обратно. Нужно место в машине или номер — отметьте в анкете ниже.</p></div>
      </div>
    </div>
  </section>

  <!-- RSVP -->
  <section class="center">
    <div class="wrap reveal">
      <p class="eyebrow">скажите «да»</p>
      <h2>Подтвердите участие</h2>
      <div class="rsvp-card" id="rsvpCard">
        <form id="rsvp" novalidate>
          <div class="field">
            <label class="lbl" for="name">Имя и фамилия</label>
            <input type="text" id="name" name="name" placeholder="Например: Мария и Павел Ивановы" autocomplete="name">
          </div>
          <div class="field">
            <span class="lbl">Будете с нами?</span>
            <div class="chips">
              <label class="chip"><input type="radio" name="attend" value="yes" checked><span>Конечно, буду!</span></label>
              <label class="chip"><input type="radio" name="attend" value="pair"><span>Придём вдвоём</span></label>
              <label class="chip"><input type="radio" name="attend" value="no"><span>Увы, не смогу</span></label>
            </div>
          </div>
          <div class="field">
            <span class="lbl">Предпочтения по напиткам</span>
            <div class="chips">
              <label class="chip"><input type="checkbox" name="drink" value="Шампанское"><span>Шампанское</span></label>
              <label class="chip"><input type="checkbox" name="drink" value="Белое вино"><span>Белое вино</span></label>
              <label class="chip"><input type="checkbox" name="drink" value="Красное вино"><span>Красное вино</span></label>
              <label class="chip"><input type="checkbox" name="drink" value="Безалкогольное"><span>Безалкогольное</span></label>
            </div>
          </div>
          <div class="field">
            <span class="lbl">Нужен трансфер?</span>
            <div class="chips">
              <label class="chip"><input type="radio" name="transfer" value="yes"><span>Да, туда и обратно</span></label>
              <label class="chip"><input type="radio" name="transfer" value="no" checked><span>Приеду сам(а)</span></label>
            </div>
          </div>
          <button class="btn" type="submit">Отправить ответ</button>
          <p class="rsvp-note">Пожалуйста, ответьте до 20 августа 2026 🤍</p>
        </form>
      </div>
    </div>
  </section>

  <!-- ФИНАЛ -->
  <section class="finale">
    <div class="reveal">
      <p class="script">Ждём вас — самые любимые и родные!</p>
      <p class="names">Алексей и Ирина · 3 сентября 2026</p>
    </div>
  </section>
</main>

<script>
const REDUCED = matchMedia('(prefers-reduced-motion: reduce)').matches;
const $ = s => document.querySelector(s);

/* ---------- КОНВЕРТ ---------- */
let opened = false;
function finishOpen(){
  $('#scene').classList.add('gone');
  document.body.classList.remove('locked');
  document.body.classList.add('opened');
}
$('#envelope').addEventListener('click', () => {
  if (opened) return; opened = true;
  if (REDUCED) { finishOpen(); return; }
  $('#scene').classList.add('open');
  setTimeout(finishOpen, 1150);
});

/* ---------- ТАЙМЕР ОБРАТНОГО ОТСЧЁТА ---------- */
const TARGET = new Date('2026-09-03T16:30:00+03:00');
const pad = n => String(n).padStart(2, '0');
function tick(){
  const diff = TARGET - Date.now();
  if (diff <= 0) { $('#count').innerHTML = '<div class="cell" style="min-width:auto;padding:18px 30px"><b>Сегодня!</b></div>'; return; }
  $('#cd-d').textContent = Math.floor(diff / 864e5);
  $('#cd-h').textContent = pad(Math.floor(diff / 36e5) % 24);
  $('#cd-m').textContent = pad(Math.floor(diff / 6e4) % 60);
  $('#cd-s').textContent = pad(Math.floor(diff / 1e3) % 60);
}
tick(); setInterval(tick, 1000);

/* ---------- ПОЯВЛЕНИЕ ПРИ СКРОЛЛЕ ---------- */
const els = document.querySelectorAll('.reveal');
if (REDUCED || !('IntersectionObserver' in window)) {
  els.forEach(el => el.classList.add('in'));
} else {
  const io = new IntersectionObserver(entries => entries.forEach(e => {
    if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
  }), { threshold: .12 });
  els.forEach(el => io.observe(el));
}

/* ---------- ЛЕТЯЩИЕ ЛЕПЕСТКИ ---------- */
(function(){
  const c = $('#petals');
  if (REDUCED) { c.remove(); return; }
  const ctx = c.getContext('2d');
  const colors = ['#e5b1a5', '#d9b98b', '#eacdb7', '#d69c90'];
  let W, H, list = [];
  function size(){ W = c.width = innerWidth; H = c.height = innerHeight; }
  function make(anywhere){
    return { x: Math.random()*W, y: anywhere ? Math.random()*H : -12,
      s: 3.5 + Math.random()*5.5, vy: .35 + Math.random()*.7,
      ph: Math.random()*6.28, sp: .004 + Math.random()*.01,
      rot: Math.random()*6.28, vr: (Math.random()-.5)*.02,
      col: colors[Math.random()*colors.length|0], a: .35 + Math.random()*.45 };
  }
  size(); addEventListener('resize', size);
  for (let i = 0; i < Math.min(44, W/26); i++) list.push(make(true));
  (function loop(){
    ctx.clearRect(0, 0, W, H);
    for (let p of list) {
      p.ph += p.sp; p.y += p.vy; p.x += Math.sin(p.ph)*.5; p.rot += p.vr;
      if (p.y > H + 14) Object.assign(p, make(false));
      ctx.save(); ctx.translate(p.x, p.y); ctx.rotate(p.rot);
      ctx.globalAlpha = p.a; ctx.fillStyle = p.col;
      ctx.beginPath(); ctx.ellipse(0, 0, p.s, p.s*.55, 0, 0, 6.28); ctx.fill();
      ctx.restore();
    }
    requestAnimationFrame(loop);
  })();
  document.addEventListener('visibilitychange', () => ctx.clearRect(0,0,W,H));
})();

/* ---------- RSVP-ФОРМА ----------
   Сейчас форма работает без сервера. Чтобы реально собирать ответы,
   отправляйте данные на Formspree / Google Sheets / в Telegram-бота. */
$('#rsvp').addEventListener('submit', e => {
  e.preventDefault();
  const name = $('#name').value.trim();
  if (!name) { $('#name').focus(); $('#name').style.borderColor = '#c98d84'; return; }
  const card = $('#rsvpCard');
  card.innerHTML = '<div class="rsvp-done"><div style="font-size:40px">💌</div>' +
    '<p class="big">Спасибо, ' + name.replace(/[<>]/g,'') + '!</p>' +
    '<p style="color:var(--ink-soft)">Ваш ответ улетел к нам. До встречи 3 сентября!</p></div>';
  if (REDUCED) return;
  for (let i = 0; i < 26; i++) {
    const b = document.createElement('i');
    b.className = 'petal-bit';
    b.style.setProperty('--dx', (Math.random()*260 - 130) + 'px');
    b.style.setProperty('--dy', -(60 + Math.random()*180) + 'px');
    b.style.setProperty('--r', (Math.random()*720 - 360) + 'deg');
    b.style.background = ['#e5b1a5','#d9b98b','#eacdb7'][Math.random()*3|0];
    card.appendChild(b);
    setTimeout(() => b.remove(), 1100);
  }
});
</script>
</body>
</html>
}