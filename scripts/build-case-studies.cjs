// Generates /case-studies/*.html from the data below. Run: node scripts/build-case-studies.cjs
// Shared chrome (nav, footer, music, WhatsApp) matches projects/*.html.
const fs = require('fs');
const path = require('path');
const OUT = path.join(__dirname, '..', 'case-studies');
fs.mkdirSync(OUT, { recursive: true });

const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const SUB = 'a division of Filmworks'.split('').map(c => `<span>${c === ' ' ? '&nbsp;' : c}</span>`).join('');

const fig = (src, cap, alt = cap) => `<figure class="cs-figure sr-up"><img src="${src}" alt="${esc(alt)}" loading="lazy" decoding="async"><figcaption>${esc(cap)}</figcaption></figure>`;
const vid = (src, poster, cap, { controls = false } = {}) => `<figure class="cs-video sr-up"><video src="${src}" poster="${poster}" muted loop playsinline preload="none" data-autoplay${controls ? ' controls' : ''}></video>${cap ? `<figcaption>${esc(cap)}</figcaption>` : ''}</figure>`;
const step = (n, title, text, body) => `
<section class="cs-step">
  <div class="wrap">
    <div class="cs-step-head sr-up"><div class="cs-step-num">${n}</div><div><h3>${title}</h3><p>${text}</p></div></div>
    ${body}
  </div>
</section>`;
const tags = list => list.map(t => `<span class="cs-tag">${esc(t)}</span>`).join('');

function page(cs, prev, next) {
  const url = `https://www.epical.art/case-studies/${cs.slug}.html`;
  const heroMedia = cs.heroVideo
    ? `<video class="proj-hero-bg" src="${cs.heroVideo}" poster="${cs.heroImg}" autoplay muted loop playsinline aria-hidden="true"></video>`
    : `<img class="proj-hero-bg" src="${cs.heroImg}" alt="">`;
  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>${esc(cs.name)} — Case Study | Epical Studio</title>
<meta name="description" content="${esc(cs.meta)}">
<meta name="robots" content="index, follow">
<link rel="canonical" href="${url}">
<meta property="og:type" content="article">
<meta property="og:url" content="${url}">
<meta property="og:title" content="${esc(cs.name)} — Case Study | Epical Studio">
<meta property="og:description" content="${esc(cs.meta)}">
<meta property="og:image" content="https://www.epical.art${cs.ogImg || cs.heroImg}">
<link rel="icon" type="image/x-icon" href="/favicon.ico">
<link rel="icon" type="image/png" sizes="192x192" href="/favicon-192.png">
<script type="application/ld+json">
${JSON.stringify({ '@context': 'https://schema.org', '@type': 'CreativeWork', name: `${cs.name} — Case Study`, about: cs.meta, creator: { '@id': 'https://www.epical.art/#organization' }, image: `https://www.epical.art${cs.ogImg || cs.heroImg}` })}
</script>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Orbitron:wght@500;600;700;800;900&display=swap">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300;400;500&display=swap">
<link rel="stylesheet" href="/assets/css/site.css">
<link rel="stylesheet" href="/assets/css/case-study.css">
</head>
<body>
<div id="scroll-prog"></div>
<div id="grain"></div>

<nav id="nav" aria-label="Main navigation">
  <a href="/#hero" class="nav-brand">
    <img class="nav-brand-mark" src="/assets/logo-icon.jpg" alt="" aria-hidden="true">
    <span class="nav-brand-text">
      <span class="nav-brand-name">Epical Studio</span>
      <span class="nav-brand-sub" aria-label="a division of Filmworks">${SUB}</span>
    </span>
  </a>
  <ul class="nav-links">
    <li><a href="/#hero">Home</a></li>
    <li><a href="/#portfolio">Work</a></li>
    <li><a href="/#case-studies" class="active">Case Studies</a></li>
    <li><a href="/#services">Services</a></li>
    <li><a href="/#founder">Team</a></li>
    <li><a href="/#contact">Contact</a></li>
  </ul>
  <div class="nav-cta">
    <div class="nav-socials">
      <a class="nav-soc" href="https://www.instagram.com/epical_studio/" target="_blank" aria-label="Instagram"><i class="fab fa-instagram"></i></a>
      <a class="nav-soc" href="https://www.youtube.com/@EpicalStudio2026" target="_blank" aria-label="YouTube"><i class="fab fa-youtube"></i></a>
      <a class="nav-soc" href="https://x.com/Epical_Studio" target="_blank" aria-label="Twitter"><i class="fab fa-x-twitter"></i></a>
    </div>
    <button class="nav-burger" id="nav-burger" aria-label="Menu"><span></span><span></span><span></span></button>
    <a href="/#contact" class="nav-talk">Let's Talk</a>
  </div>
</nav>
<div id="nav-mobile" role="dialog" aria-label="Navigation menu">
  <button class="nm-close" id="nm-close"><i class="fas fa-times"></i></button>
  <a href="/#hero">Home</a><a href="/#portfolio">Work</a><a href="/#case-studies">Case Studies</a><a href="/#services">Services</a><a href="/#founder">Team</a><a href="/#contact">Contact</a>
</div>

<section class="proj-hero cs-hero">
  ${heroMedia}
  <div class="proj-hero-vignette"></div>
  <div class="proj-hero-content">
    <a href="/#case-studies" class="proj-back sr-up"><i class="fas fa-arrow-left"></i>All Case Studies</a>
    <div class="proj-cat sr-up">Case Study · ${esc(cs.category)}</div>
    <h1 class="proj-title sr-up">${esc(cs.name)}</h1>
    <p class="proj-desc sr-up">${cs.lede}</p>
    <div class="cs-hero-tags sr-up">${tags(cs.services)}</div>
  </div>
</section>

<section class="cs-overview">
  <div class="wrap">
    <div class="cs-overview-grid">
      <div class="cs-prose sr-up">${cs.overview}</div>
      <dl class="cs-facts sr-up">${cs.facts.map(([k, v]) => `<div class="cs-fact"><dt>${esc(k)}</dt><dd>${v}</dd></div>`).join('')}</dl>
    </div>
  </div>
</section>
${cs.body}

<section id="proj-nav">
  <div class="proj-nav-grid">
    <a class="proj-nav-link" href="/case-studies/${prev.slug}.html"><span class="proj-nav-label"><i class="fas fa-arrow-left"></i> Previous Case Study</span><span class="proj-nav-name">${esc(prev.name)}</span></a>
    <a class="proj-nav-link next" href="/case-studies/${next.slug}.html"><span class="proj-nav-label">Next Case Study <i class="fas fa-arrow-right"></i></span><span class="proj-nav-name">${esc(next.name)}</span></a>
  </div>
</section>

<section id="contact">
  <div class="wrap" style="text-align:center">
    <div class="sec-eyebrow sr-up" style="justify-content:center">Your Brand Next?</div>
    <h2 class="sec-title sr-up">Start Your Project</h2>
    <p class="sec-sub sr-up" style="margin:0 auto 30px">Tell us about your brand and we'll plan the visuals, the film and the digital groundwork around it.</p>
    <a href="/#contact" class="btn-gold sr-up"><i class="fas fa-envelope-open-text"></i>Get In Touch</a>
  </div>
</section>

<footer>
  <div class="wrap">
    <div class="foot-inner">
      <img src="/assets/e.jpg" class="foot-logo-img" alt="Epical Studio" width="1280" height="853">
      <div class="socials">
        <a class="soc" href="https://www.instagram.com/epical_studio/" target="_blank" aria-label="Instagram"><i class="fab fa-instagram"></i></a>
        <a class="soc" href="https://www.youtube.com/@EpicalStudio2026" target="_blank" aria-label="YouTube"><i class="fab fa-youtube"></i></a>
        <a class="soc" href="https://www.facebook.com/profile.php?id=61590392766722" target="_blank" aria-label="Facebook"><i class="fab fa-facebook-f"></i></a>
        <a class="soc" href="https://x.com/Epical_Studio" target="_blank" aria-label="Twitter"><i class="fab fa-x-twitter"></i></a>
        <a class="soc" href="https://www.linkedin.com/company/smarthireai2026" target="_blank" aria-label="LinkedIn"><i class="fab fa-linkedin-in"></i></a>
        <a class="soc" href="https://wa.me/923002166620" target="_blank" aria-label="WhatsApp"><i class="fab fa-whatsapp"></i></a>
        <a class="soc" href="mailto:contact@epical.art" aria-label="Email"><i class="fas fa-envelope"></i></a>
      </div>
      <div class="foot-links"><a href="/#portfolio">Work</a><a href="/#case-studies">Case Studies</a><a href="/#services">Services</a><a href="/#founder">Team</a><a href="/#contact">Contact</a></div>
      <div class="foot-copy">© 2026 Epical Studio. All Rights Reserved. — The Art of Cinematic Production</div>
      <div class="foot-copy">Ambient score: "Floating Cities" by Kevin MacLeod (incompetech.com), licensed under <a href="https://creativecommons.org/licenses/by/4.0/" target="_blank" rel="noopener">CC BY 4.0</a></div>
    </div>
  </div>
</footer>

<button id="back-top" aria-label="Back to top"><i class="fas fa-chevron-up"></i></button>
<div id="music-widget">
  <audio id="m-audio" src="/assets/audio/floating-cities.mp3" loop preload="auto"></audio>
  <div class="m-eq off" id="m-eq"><div class="m-bar"></div><div class="m-bar"></div><div class="m-bar"></div><div class="m-bar"></div><div class="m-bar"></div></div>
  <div class="m-label"><div class="m-name">Epical Ambient</div><div class="m-sub">Floating Cities</div></div>
  <button class="m-toggle" id="m-toggle"><i class="fas fa-play" id="m-icon"></i></button>
</div>
<a id="wa-float" href="https://wa.me/923002166620?text=${encodeURIComponent(`Hi! I saw the ${cs.name} case study and I'd like to discuss a project.`)}" target="_blank" aria-label="WhatsApp"><i class="fab fa-whatsapp"></i></a>

<script type="module">
import {initChrome, createGallery, initParallaxLayer, initPageTransitions} from '/assets/js/site.js';
import {initFigureLightbox, initInlineVideos, initYouTubeLite} from '/assets/js/case-study.js';
const l=document.createElement('link');l.rel='stylesheet';l.href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css';document.head.appendChild(l);
${cs.galleries ? `const GALLERIES=${JSON.stringify(cs.galleries)};
for(const [id,media] of Object.entries(GALLERIES))createGallery({container:document.getElementById(id),media});` : ''}
initChrome();
initPageTransitions();
initFigureLightbox();
initInlineVideos();
initYouTubeLite();
initParallaxLayer(document.querySelector('.proj-hero-bg'),{speed:.25});
</script>
</body>
</html>
`;
}

/* ───────────────────────── case studies ───────────────────────── */
const ZN = '/assets/case-studies/zahra-noor';
const HIC = '/assets/case-studies/happy-ice-cream';
const IG = '/assets/case-studies/igec-global';

const CASES = [
  {
    slug: 'zahra-noor',
    name: 'Zahra Noor',
    category: 'Fashion · AI Brand Film',
    meta: 'Zahra Noor — "A Garden Above the Clouds". How Epical Studio turned one floral three-piece suit into a 15-second AI fashion film with vertical edits and social cutdowns.',
    heroImg: `${ZN}/poster/01.jpg`,
    heroVideo: `${ZN}/vid/01.mp4`,
    ogImg: `${ZN}/img/storyboard.webp`,
    lede: 'A Garden Above the Clouds — one floral three-piece suit, turned into a dreamlike 15-second brand film and a full set of vertical social edits, without a location shoot.',
    services: ['Creative Direction', 'AI Talent & Styling', 'Location Design', 'Storyboard', 'Brand Film', 'Social Cutdowns'],
    overview: `
      <h2>The Brief</h2>
      <p>Zahra Noor needed a launch film for a new floral printed three-piece — kurta, trousers and dupatta — that felt premium, feminine and aspirational. The garment had to stay exactly true to the real product: the print, the scalloped lace hems, the flower buttons and the tasselled dupatta.</p>
      <h2>Our Approach</h2>
      <p>We treated it like a real fashion production, just without the logistics. The actual garment was the source of truth. We locked a consistent model, designed a world around the print's palette, storyboarded every second, and then generated and edited the film in both landscape and vertical formats.</p>
      <p>The result is one campaign world — a marble terrace floating above a sea of clouds — that works on YouTube, Instagram Reels, Stories and paid social.</p>`,
    facts: [
      ['Client', 'Zahra Noor'],
      ['Industry', 'Womenswear · Pret'],
      ['Concept', '“A Garden Above the Clouds”'],
      ['Deliverables', '15s hero film (16:9) · 2 vertical 15s edits (9:16) · 3 × 4s social cutdowns · stills'],
      ['Pipeline', 'Product reference → AI talent → location sheet → storyboard → Cinema Studio / Seedance 2.5 → edit &amp; grade'],
    ],
    body:
      step('01', 'The Product Is the Source of Truth', 'Everything starts from a clean studio flat of the real garment. Every later frame is checked against it, so the print scale, lace trims, button clusters and dupatta border never drift.',
        `<div class="cs-pair">${fig(`${ZN}/img/product.webp`, 'Product reference — kurta, trousers and dupatta')}${fig(`${ZN}/img/model.webp`, 'Talent lock — the same model, styled in the exact garment')}</div>`) +
      step('02', 'Designing the World', 'A location sheet defines the set before a single shot is generated: a white marble terrace with neoclassical arches, blossom canopies and flowering urns, floating above the clouds in soft golden daylight. The palette is pulled straight from the print.',
        `<div class="cs-narrow">${fig(`${ZN}/img/location.webp`, 'Location sheet — @ZahraGarden, “A Garden Above the Clouds”')}</div>
        <div class="cs-swatches sr-up" style="justify-content:center">
          <span class="cs-swatch"><i style="background:#e8b4b8"></i>Blush Pink</span>
          <span class="cs-swatch"><i style="background:#f6f0e6"></i>Ivory</span>
          <span class="cs-swatch"><i style="background:#f3e3bf"></i>Warm Cream</span>
          <span class="cs-swatch"><i style="background:#a3b98a"></i>Soft Green</span>
          <span class="cs-swatch"><i style="background:#a9c8ec"></i>Sky Blue</span>
          <span class="cs-swatch"><i style="background:#e7d9c6"></i>Marble Beige</span>
        </div>`) +
      step('03', 'Every Second Storyboarded', 'Fifteen seconds, five beats. Each beat has one job — from the first glimpse of wonder to the closing brand portrait with the Zahra Noor mark.',
        `<div class="cs-narrow">${fig(`${ZN}/img/storyboard.webp`, 'Storyboard — five beats across 15 seconds')}</div>
        <div class="cs-beats sr-up">
          <div class="cs-beat"><b>01 · 0–3s</b><span>A hint of wonder</span></div>
          <div class="cs-beat"><b>02 · 3–6s</b><span>Floral poetry</span></div>
          <div class="cs-beat"><b>03 · 6–10s</b><span>Above the clouds</span></div>
          <div class="cs-beat"><b>04 · 10–13s</b><span>Connection</span></div>
          <div class="cs-beat"><b>05 · 13–15s</b><span>Brand portrait</span></div>
        </div>`) +
      step('04', 'The Film', 'The 15-second hero film in 16:9 for YouTube and web, with sound. Tap to play with audio.',
        `<div class="cs-narrow" style="max-width:1100px">${vid(`${ZN}/vid/01.mp4`, `${ZN}/poster/01.jpg`, 'Hero film · 15s · 16:9', { controls: true })}</div>`) +
      step('05', 'Built for Vertical', 'Two full 15-second vertical edits and three 4-second cutdowns for Reels, Stories and paid social — the same world, recut for the feed.',
        `<div class="cs-vertical-row">
          ${vid(`${ZN}/vid/02.mp4`, `${ZN}/poster/02.jpg`, 'Vertical edit A · 15s', { controls: true })}
          ${vid(`${ZN}/vid/03.mp4`, `${ZN}/poster/03.jpg`, 'Vertical edit B · 15s', { controls: true })}
          ${vid(`${ZN}/vid/04.mp4`, `${ZN}/poster/04.jpg`, 'Cutdown · 4s')}
          ${vid(`${ZN}/vid/05.mp4`, `${ZN}/poster/05.jpg`, 'Cutdown · 4s')}
          ${vid(`${ZN}/vid/06.mp4`, `${ZN}/poster/06.jpg`, 'Cutdown · 4s')}
        </div>`),
  },
  {
    slug: 'happy-ice-cream',
    name: 'Happy Ice Cream',
    category: 'Food & Beverage · Digital Foundation',
    meta: 'Happy Ice Cream — a Karachi favourite since 1982. Epical Studio planned a digital foundation sprint: product photography, heritage campaign concepts, a website redesign mockup, Google, delivery platforms, social and ads.',
    heroImg: `${HIC}/shoot/03.webp`,
    lede: 'Four decades of Karachi heritage, brought up to speed online — product photography, heritage campaign concepts, a website redesign and a sprint plan covering Google, delivery apps, social and paid ads.',
    services: ['Product Photography', 'AI-Enhanced Visuals', 'Campaign Concepts', 'Website Redesign', 'Google &amp; Delivery Apps', 'Social &amp; Ads'].map(s => s.replace('&amp;', '&')),
    overview: `
      <h2>The Challenge</h2>
      <p>Happy Ice Cream has been part of Karachi since 1982, with several established branches, a proven menu of ice creams, faloodas and shakes, and a loyal customer base built over generations. The brand was never the problem — its digital presence simply hadn't kept pace.</p>
      <p>The website looked dated, there was little professional photography of the products, Google listings were under-developed, there was no paid advertising, and social posting was inconsistent next to newer competitors.</p>
      <h2>Our Approach</h2>
      <p>We designed a focused digital foundation sprint, with photography as the anchor. Every downstream channel — Google, Foodpanda, Indolj, social and ads — is built from one consistent set of visuals. We then restructured the plan around the Sharfabad branch to put the budget where it drives visibility and orders most directly.</p>`,
    facts: [
      ['Client', 'Happy Ice Cream, Karachi'],
      ['Heritage', 'Since 1982 · multiple branches'],
      ['Engagement', 'Digital foundation sprint, revised to 7 phases around Sharfabad'],
      ['Visual scope', '40 hero images · 15 short videos · 8 cinematic reels'],
      ['Channels', 'Google Business · Foodpanda · Indolj · Instagram · Facebook · Meta &amp; Google Ads'],
    ],
    galleries: {
      'hic-shoot': Array.from({ length: 16 }, (_, i) => ({ type: 'img', src: `${HIC}/shoot/${String(i + 1).padStart(2, '0')}.webp`, title: 'Happy Ice Cream — product photography' })),
    },
    body:
      step('01', 'Product Photography', 'Signature sundaes, faloodas, shakes and scoops, shot and AI-enhanced into a consistent library of hero visuals: rich chocolate on dark backdrops, strawberry on blush pink, and colourful falooda in motion.',
        `<div class="gallery-grid cs-gallery-grid" id="hic-shoot"></div>`) +
      step('02', 'Campaign Concepts', 'Theme explorations built on what makes the brand irreplaceable: its heritage. “Happiness Since 1982”, “The Taste You Grew Up With” and “Some Memories Never Melt” — three generations sharing the same ice cream, alongside bold product-led social creatives.',
        `<div class="cs-pair">${fig(`${HIC}/campaign/13.webp`, 'The Taste You Grew Up With — 1980s Karachi to today')}${fig(`${HIC}/campaign/03.webp`, 'Some Memories Never Melt — heritage key visual')}</div>
        <div class="cs-trio" style="margin-top:16px">
          ${fig(`${HIC}/campaign/02.webp`, 'A Karachi Tradition of Joy')}
          ${fig(`${HIC}/campaign/04.webp`, 'The Taste You Grew Up With — 1984 to 2024')}
          ${fig(`${HIC}/campaign/05.webp`, 'A Karachi landmark at dusk')}
          ${fig(`${HIC}/campaign/06.webp`, 'Happiness Since 1982 — social creative')}
          ${fig(`${HIC}/campaign/07.webp`, 'Indulgence, Elevated — product creative')}
          ${fig(`${HIC}/campaign/09.webp`, 'Your Signature Chocolate Joy')}
        </div>
        <div class="cs-pair" style="margin-top:16px">${fig(`${HIC}/campaign/01.webp`, 'Happiness Since 1982 — premium print layout')}${fig(`${HIC}/campaign/11.webp`, 'Strawberry Cheesecake — flavour launch post')}</div>`) +
      step('03', 'Website Redesign', 'A full redesign mockup for the existing Indolj ordering site: heritage storytelling up front, the menu presented as appetising product cards, WhatsApp ordering and a branch locator — handed over so the live site could be rebuilt to match.',
        `<div class="cs-pair">${fig(`${HIC}/web/01.webp`, 'Homepage — “The Taste You Grew Up With”')}${fig(`${HIC}/web/02.webp`, 'Menu page — “Made With Love. Enjoyed For Generations.”')}</div>`) +
      step('04', 'The Sprint Plan', 'Seven phases, each with a clear, checkable output — and a week-by-week roadmap so the client always knows what is happening next.',
        `<ul class="cs-list sr-up">
          <li><strong>01 · Photography</strong>One on-site day at Sharfabad: 40 hero images, 15 short videos, 8 cinematic reels.</li>
          <li><strong>02 · Google Business</strong>Profile optimisation, weekly posts and review management.</li>
          <li><strong>03 · Foodpanda</strong>Menu optimisation with new photography, descriptions and offers.</li>
          <li><strong>04 · Indolj</strong>Ordering-site optimisation: images, descriptions and offers.</li>
          <li><strong>05 · Social</strong>12 posts, 8 reels and stories across Instagram and Facebook.</li>
          <li><strong>06 · Ads</strong>Meta and Google campaigns with geo-targeting around each branch.</li>
          <li><strong>07 · Reporting</strong>Weekly updates and a final performance report.</li>
          <li><strong>Measured, not assumed</strong>Analytics and Search Console set up on day one, with a joint review at the end of the sprint.</li>
        </ul>
        <div class="cs-trio" style="margin-top:24px">
          ${fig(`${HIC}/plan/02.webp`, 'Where Happy Ice Cream stands today')}
          ${fig(`${HIC}/plan/03.webp`, 'What the sprint delivers')}
          ${fig(`${HIC}/plan/04.webp`, 'Week-by-week roadmap')}
        </div>`),
  },
  {
    slug: 'igec-global',
    name: 'IGEC Global',
    category: 'Education · Website Redesign',
    meta: 'IGEC Global — a full website redesign for an Australia-focused education and student visa consultancy with almost 30 years of experience. Course finder, bilingual English/Arabic, accessibility tools and a consultation-first journey.',
    heroImg: `${IG}/hero.webp`,
    lede: 'A complete redesign for an Australia-focused education and student visa consultancy — almost 30 years of experience, finally presented with the clarity and confidence it deserves.',
    services: ['UX Strategy', 'Website Redesign', 'Front-End Build', 'AI-Stylised Imagery', 'Bilingual EN / AR', 'Accessibility'],
    overview: `
      <h2>The Challenge</h2>
      <p>IGEC Global has guided students to Australian universities for almost three decades, with recognised certifications and a strong track record. Its website needed to communicate that trust instantly and turn visitors — many of them students and parents comparing several agents — into booked consultations.</p>
      <h2>Our Approach</h2>
      <p>We rebuilt the site around a single journey: understand why Australia, see how IGEC helps, find a course, and book a free consultation. A course finder sits right in the hero, trust signals come immediately after, and the consultation call-to-action is always one tap away.</p>
      <p>The redesign also adds an English / Arabic toggle, an accessibility toolbar, a live chat entry point, and AI-stylised campus and city imagery that gives the site a consistent, premium look.</p>`,
    facts: [
      ['Client', 'IGEC Global — International Group for Educational Consultancy'],
      ['Industry', 'Education &amp; student visa consultancy'],
      ['Focus', 'Study in Australia'],
      ['Scope', 'UX, visual design, responsive front-end build, imagery'],
      ['Live preview', '<a href="/igecglobal-redesign/" target="_blank" rel="noopener" style="color:var(--gold);text-decoration:underline">epical.art/igecglobal-redesign</a>'],
    ],
    body:
      step('01', 'Lead With Trust and Action', 'The hero answers the three questions every visitor has — who are you, why should I trust you, what do I do next — and puts a course finder right on the first screen.',
        `${fig(`${IG}/hero.webp`, 'Hero — 30 years badge, free consultation offer and course finder')}
        <div class="cs-live sr-up" style="justify-content:center"><a href="/igecglobal-redesign/" target="_blank" rel="noopener" class="btn-gold"><i class="fas fa-arrow-up-right-from-square"></i>View Live Redesign</a></div>`) +
      step('02', 'A Clear, Guided Journey', 'Why Australia, what IGEC handles, and a simple six-step path from first conversation to landing in Australia — so the process feels manageable, not intimidating.',
        `<div class="cs-pair">${fig(`${IG}/why-australia.webp`, 'Why Australia')}${fig(`${IG}/services.webp`, 'Everything you need, under one roof')}</div>
        <div style="margin-top:24px">${fig(`${IG}/how-it-works.webp`, 'How it works — six steps')}</div>`) +
      step('03', 'Destinations and Social Proof', 'City cards with AI-stylised imagery help students picture where they could live, followed by real student testimonials and a named counsellor as a dedicated point of contact.',
        `<div class="cs-pair">${fig(`${IG}/destinations.webp`, 'Australian cities to call home')}${fig(`${IG}/testimonials.webp`, 'What our students say')}</div>
        <div class="cs-pair" style="margin-top:24px">${fig(`${IG}/counsellor.webp`, 'Meet your counsellor')}${fig(`${IG}/consultation.webp`, 'Book your free consultation')}</div>`) +
      step('04', 'Mobile First', 'Most students browse on their phone. The mobile layout keeps the headline, the promise and the booking button above the fold, with a sticky consultation bar.',
        `<div class="cs-split">
          <div class="cs-prose sr-up"><h2>Built for the phone</h2><p>Large touch targets, a collapsible menu, the language toggle in the header and a persistent “Book a Free Consultation” bar — tested from small phones up to iPad Pro widths.</p></div>
          <div class="cs-phone sr-up"><img src="${IG}/mobile-01.webp" alt="IGEC Global redesign on mobile" loading="lazy"></div>
        </div>`),
  },
];

CASES.forEach((cs, i) => {
  const prev = CASES[(i - 1 + CASES.length) % CASES.length];
  const next = CASES[(i + 1) % CASES.length];
  fs.writeFileSync(path.join(OUT, `${cs.slug}.html`), page(cs, prev, next));
  console.log('wrote case-studies/' + cs.slug + '.html');
});
