/* ── SHARED SITE MODULE ── used by index.html and every /projects/*.html page ── */

export const REDUCED_MOTION = matchMedia('(prefers-reduced-motion: reduce)').matches;
export const IS_TOUCH = matchMedia('(hover:none),(pointer:coarse)').matches;

export function loadScript(src){return new Promise((res,rej)=>{const s=document.createElement('script');s.src=src;s.async=true;s.onload=res;s.onerror=rej;document.head.appendChild(s)})}
export function loadStyle(href){const l=document.createElement('link');l.rel='stylesheet';l.href=href;document.head.appendChild(l)}

/* ── CORE CHROME: nav, burger, WA float already in markup, music widget, ripple,
   shooting star, scroll progress, back-to-top, scroll-reveal, tilt loader ── */
export function initChrome(){
  loadStyle('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css');

  /* NAVBAR */
  const nav=document.getElementById('nav');
  if(nav) window.addEventListener('scroll',()=>nav.classList.toggle('stuck',scrollY>60),{passive:true});

  /* MOBILE HAMBURGER */
  const burger=document.getElementById('nav-burger');
  const mobileNav=document.getElementById('nav-mobile');
  const nmClose=document.getElementById('nm-close');
  if(burger&&mobileNav){
    const openMobileNav=()=>{burger.classList.add('open');mobileNav.classList.add('open');document.body.style.overflow='hidden'};
    const closeMobileNav=()=>{burger.classList.remove('open');mobileNav.classList.remove('open');document.body.style.overflow=''};
    burger.addEventListener('click',()=>mobileNav.classList.contains('open')?closeMobileNav():openMobileNav());
    nmClose?.addEventListener('click',closeMobileNav);
    mobileNav.querySelectorAll('a').forEach(a=>a.addEventListener('click',closeMobileNav));
  }

  /* SCROLL REVEAL */
  const srObs=new IntersectionObserver((entries,obs)=>{
    entries.forEach(e=>{if(e.isIntersecting){e.target.classList.add('visible');obs.unobserve(e.target)}});
  },{rootMargin:'0px 0px -80px 0px'});
  document.querySelectorAll('.sr-up').forEach(el=>srObs.observe(el));

  /* TILT (skip on touch) */
  if(!IS_TOUCH){
    loadScript('https://cdnjs.cloudflare.com/ajax/libs/vanilla-tilt/1.7.0/vanilla-tilt.min.js').then(()=>{
      window.VanillaTilt?.init(document.querySelectorAll('.project-card,.gallery-item'),{max:6,speed:400,glare:true,'max-glare':.08,scale:1.02});
    }).catch(()=>{});
  }

  /* SCROLL PROGRESS */
  const progBar=document.getElementById('scroll-prog');
  if(progBar){
    window.addEventListener('scroll',()=>{
      const pct=scrollY/(document.documentElement.scrollHeight-innerHeight)*100;
      progBar.style.width=Math.min(pct,100)+'%';
    },{passive:true});
  }

  /* BACK TO TOP */
  const backTop=document.getElementById('back-top');
  if(backTop){
    window.addEventListener('scroll',()=>backTop.classList.toggle('vis',scrollY>700),{passive:true});
    backTop.addEventListener('click',()=>window.scrollTo({top:0,behavior:'smooth'}));
  }

  /* CLICK RIPPLE + SHOOTING STARS (skip under reduced motion) */
  if(!REDUCED_MOTION){
    document.addEventListener('click',e=>{
      const r=document.createElement('div');
      r.className='click-ripple';
      r.style.left=e.clientX+'px';r.style.top=e.clientY+'px';
      document.body.appendChild(r);
      setTimeout(()=>r.remove(),700);
    });
    const hero=document.getElementById('hero')||document.querySelector('.proj-hero');
    if(hero){
      (function shootLoop(){
        const s=document.createElement('div');
        s.className='shooting-star';
        const w=Math.floor(60+Math.random()*80);
        const angle=-15+Math.random()*20;
        s.style.cssText=`top:${10+Math.random()*50}%;left:${Math.random()*55}%;width:${w}px;transform:rotate(${angle}deg)`;
        hero.appendChild(s);
        setTimeout(()=>s.remove(),1700);
        setTimeout(shootLoop,5000+Math.random()*9000);
      })();
    }
  }

  /* COUNTER ANIMATION */
  const counterObs=new IntersectionObserver((entries,obs)=>{
    entries.forEach(e=>{
      if(!e.isIntersecting)return;
      const el=e.target;
      const target=+el.dataset.target;
      const suffix=el.dataset.suffix||'';
      if(REDUCED_MOTION){el.textContent=target+suffix;obs.unobserve(el);return}
      const dur=1800;const start=Date.now();
      function tick(){
        const p=Math.min((Date.now()-start)/dur,1);
        const v=Math.round(target*(1-Math.pow(1-p,3)));
        el.textContent=v+suffix;
        if(p<1)requestAnimationFrame(tick);
      }
      tick();obs.unobserve(el);
    });
  },{rootMargin:'0px 0px -60px 0px'});
  document.querySelectorAll('.stat-num[data-target]').forEach(el=>counterObs.observe(el));

  /* MOUSE PARALLAX BLOBS */
  if(!REDUCED_MOTION && !IS_TOUCH){
    document.addEventListener('mousemove',e=>{
      const xp=(e.clientX/innerWidth-.5)*16;
      const yp=(e.clientY/innerHeight-.5)*16;
      document.querySelectorAll('.port-blob').forEach((b,i)=>{
        b.style.transform=`translate(${xp*(i+1)*.5}px,${yp*(i+1)*.5}px)`;
      });
    });
  }

  /* WA FLOAT ambient audio widget */
  initMusicWidget();
}

/* ── GENERATIVE AMBIENT AUDIO WIDGET ── */
function initMusicWidget(){
  const toggle=document.getElementById('m-toggle');
  if(!toggle)return;
  let audioCtx,masterGain,musicOn=false;
  async function startMusic(){
    if(audioCtx)return;
    audioCtx=new(window.AudioContext||window.webkitAudioContext)();
    await audioCtx.resume();
    const t=audioCtx.currentTime;
    const comp=audioCtx.createDynamicsCompressor();
    comp.threshold.value=-18;comp.knee.value=12;comp.ratio.value=4;comp.attack.value=.02;comp.release.value=.4;
    comp.connect(audioCtx.destination);
    const conv=audioCtx.createConvolver();
    const rLen=audioCtx.sampleRate*3;
    const rBuf=audioCtx.createBuffer(2,rLen,audioCtx.sampleRate);
    for(let c=0;c<2;c++){const d=rBuf.getChannelData(c);for(let i=0;i<rLen;i++)d[i]=(Math.random()*2-1)*Math.pow(1-i/rLen,4)}
    conv.buffer=rBuf;
    const dry=audioCtx.createGain();dry.gain.value=.80;
    const wet=audioCtx.createGain();wet.gain.value=.20;
    dry.connect(comp);conv.connect(wet);wet.connect(comp);
    masterGain=audioCtx.createGain();
    masterGain.gain.setValueAtTime(0,t);
    masterGain.gain.linearRampToValueAtTime(.10,t+10);
    masterGain.connect(dry);masterGain.connect(conv);
    const PAD=[261.63,329.63,392.00,493.88];
    const PAD_VOL=[.11,.09,.08,.06];
    PAD.forEach((freq,i)=>{
      const o=audioCtx.createOscillator(),g=audioCtx.createGain();
      o.type='sine';o.frequency.value=freq;
      g.gain.setValueAtTime(0,t);g.gain.linearRampToValueAtTime(PAD_VOL[i],t+5+i*2.5);
      o.connect(g);g.connect(masterGain);o.start(t);
    });
    const PENTA=[261.63,329.63,392.00,440.00,523.25,659.25];
    function melodNote(){
      const freq=PENTA[Math.floor(Math.random()*PENTA.length)];
      const now=audioCtx.currentTime;
      const atk=1.5+Math.random()*2;const hold=3+Math.random()*5;const rel=2+Math.random()*2.5;
      const vol=.05+Math.random()*.06;
      const o=audioCtx.createOscillator(),g=audioCtx.createGain();
      o.type='sine';o.frequency.value=freq;
      g.gain.setValueAtTime(0,now);g.gain.linearRampToValueAtTime(vol,now+atk);
      g.gain.setValueAtTime(vol,now+atk+hold);g.gain.linearRampToValueAtTime(0,now+atk+hold+rel);
      o.connect(g);g.connect(masterGain);o.start(now);o.stop(now+atk+hold+rel+.1);
      setTimeout(melodNote,(2.5+Math.random()*5)*1000);
    }
    setTimeout(melodNote,3000);
    musicOn=true;
    document.getElementById('m-icon').className='fas fa-pause';
    document.getElementById('m-eq').classList.remove('off');
  }
  const EVENTS=['click','mousemove','scroll','touchstart','keydown'];
  function onFirst(){
    EVENTS.forEach(e=>document.removeEventListener(e,onFirst,{capture:true}));
    startMusic();
  }
  EVENTS.forEach(e=>document.addEventListener(e,onFirst,{capture:true,once:false,passive:true}));
  toggle.addEventListener('click',()=>{
    if(!audioCtx){startMusic();return}
    const now=audioCtx.currentTime;
    masterGain.gain.cancelScheduledValues(now);
    masterGain.gain.setValueAtTime(masterGain.gain.value,now);
    if(musicOn){
      masterGain.gain.linearRampToValueAtTime(0,now+1.5);
      musicOn=false;
      document.getElementById('m-icon').className='fas fa-play';
      document.getElementById('m-eq').classList.add('off');
    } else {
      if(audioCtx.state==='suspended')audioCtx.resume();
      masterGain.gain.linearRampToValueAtTime(.10,now+2);
      musicOn=true;
      document.getElementById('m-icon').className='fas fa-pause';
      document.getElementById('m-eq').classList.remove('off');
    }
  });
}

/* ── PARTICLE CANVAS FACTORY (hero background specks) ── */
export function createParticles(container,{density=90}={}){
  if(!container||REDUCED_MOTION)return;
  const canvas=document.createElement('canvas');
  const ctx=canvas.getContext('2d');
  canvas.style.cssText='position:absolute;inset:0;width:100%;height:100%';
  container.appendChild(canvas);
  let W,H,pts=[];
  const C=['rgba(200,169,110,','rgba(168,158,140,','rgba(226,192,128,','rgba(255,250,240,'];
  function resize(){W=canvas.width=container.offsetWidth;H=canvas.height=container.offsetHeight}
  class P{
    constructor(){this.init();this.y=Math.random()*H}
    init(){this.x=Math.random()*W;this.y=H+5;this.r=.3+Math.random()*1.8;this.vy=.12+Math.random()*.32;this.vx=(Math.random()-.5)*.25;this.a=.03+Math.random()*.16;this.c=C[Math.floor(Math.random()*C.length)]}
    tick(){this.y-=this.vy;this.x+=this.vx;if(this.y<-5)this.init()}
    draw(){ctx.beginPath();ctx.arc(this.x,this.y,this.r,0,Math.PI*2);ctx.fillStyle=this.c+this.a+')';ctx.fill()}
  }
  resize();
  pts=Array.from({length:IS_TOUCH?Math.round(density*.4):density},()=>new P());
  function frame(){ctx.clearRect(0,0,W,H);pts.forEach(p=>{p.tick();p.draw()});requestAnimationFrame(frame)}
  window.addEventListener('resize',()=>{resize();pts.forEach(p=>{p.x=Math.random()*W})});
  frame();
}

/* ── LENS FLARE (cursor-follow radial highlight) ── */
export function initLensFlare(heroEl,flareEl){
  if(!heroEl||!flareEl||IS_TOUCH)return;
  heroEl.addEventListener('mousemove',e=>{
    const r=heroEl.getBoundingClientRect();
    const x=((e.clientX-r.left)/r.width*100).toFixed(1)+'%';
    const y=((e.clientY-r.top)/r.height*100).toFixed(1)+'%';
    flareEl.style.background=`radial-gradient(circle 340px at ${x} ${y},rgba(200,169,110,.07) 0%,transparent 70%)`;
  });
}

/* ── SIMPLE SCROLL PARALLAX (translateY a layer at a fraction of scroll) ── */
export function initParallaxLayer(el,{speed=.3,root=null}={}){
  if(!el||REDUCED_MOTION)return;
  const target=root||window;
  function onScroll(){
    const rect=el.parentElement.getBoundingClientRect();
    const offset=rect.top*speed;
    el.style.transform=`translateY(${offset}px)`;
  }
  target.addEventListener('scroll',onScroll,{passive:true});
  onScroll();
}

/* ── LIGHTBOX (single shared DOM instance, injected once) ── */
let lbState=null;
function ensureLightbox(){
  if(lbState)return lbState;
  const lb=document.createElement('div');
  lb.id='lightbox';
  lb.innerHTML=`<div id="lb-content"></div>
    <button id="lb-close" aria-label="Close"><i class="fas fa-times"></i></button>
    <button id="lb-prev" aria-label="Previous"><i class="fas fa-chevron-left"></i></button>
    <button id="lb-next" aria-label="Next"><i class="fas fa-chevron-right"></i></button>
    <div id="lb-caption"></div>`;
  document.body.appendChild(lb);
  const lbContent=lb.querySelector('#lb-content');
  const lbCap=lb.querySelector('#lb-caption');
  let activeGallery=null;
  function show(){
    const {media,idx}=activeGallery;
    const m=media[idx];
    lbContent.innerHTML='';
    if(m.type==='img'){const img=document.createElement('img');img.src=m.src;img.alt=m.title||'';lbContent.appendChild(img)}
    else{const v=document.createElement('video');v.src=m.src;v.controls=true;v.autoplay=true;v.loop=true;v.style.width='min(90vw,1200px)';lbContent.appendChild(v)}
    lbCap.textContent=[m.title,m.tag].filter(Boolean).join(' · ');
  }
  function open(media,idx){
    activeGallery={media,idx};
    show();
    lb.classList.add('open');
    document.body.style.overflow='hidden';
  }
  function close(){lb.classList.remove('open');document.body.style.overflow='';lbContent.innerHTML='';activeGallery=null}
  function nav(dir){
    if(!activeGallery)return;
    const {media}=activeGallery;
    activeGallery.idx=(activeGallery.idx+dir+media.length)%media.length;
    show();
  }
  lb.querySelector('#lb-close').addEventListener('click',close);
  lb.querySelector('#lb-prev').addEventListener('click',()=>nav(-1));
  lb.querySelector('#lb-next').addEventListener('click',()=>nav(1));
  lb.addEventListener('click',e=>{if(e.target===lb)close()});
  document.addEventListener('keydown',e=>{
    if(!lb.classList.contains('open'))return;
    if(e.key==='Escape')close();
    if(e.key==='ArrowLeft')nav(-1);
    if(e.key==='ArrowRight')nav(1);
  });
  lbState={open,close};
  return lbState;
}

/* ── OPEN LIGHTBOX (external entry point, e.g. hero "play showreel") ── */
export function openLightbox(media,idx=0){
  ensureLightbox().open(media,idx);
}

/* ── GALLERY FACTORY: independent instance per container, shared lightbox ── */
export function createGallery({container,media,shuffleOrder=false}){
  if(!container)return null;
  const lb=ensureLightbox();
  const imgObs=new IntersectionObserver((entries,obs)=>{
    entries.forEach(e=>{
      if(!e.isIntersecting)return;
      const el=e.target;
      if(el.dataset.src){el.src=el.dataset.src;delete el.dataset.src}
      obs.unobserve(el);
    });
  },{rootMargin:'300px'});

  function shuffle(arr){for(let i=arr.length-1;i>0;i--){const j=Math.floor(Math.random()*(i+1));[arr[i],arr[j]]=[arr[j],arr[i]]}return arr}
  const list = shuffleOrder ? shuffle([...media]) : media;

  container.innerHTML='';
  list.forEach((m,idx)=>{
    const el=document.createElement('div');
    el.className='gallery-item';el.dataset.idx=idx;
    const ov=`<div class="gallery-ov">${m.cat?`<div class="proj-cat" style="margin:0 0 4px;font-size:.52rem">${m.cat}</div>`:''}<h4>${m.title||''}</h4></div>`;
    if(m.type==='img'){
      const img=document.createElement('img');
      img.alt=m.title||'';img.loading='lazy';img.decoding='async';
      img.addEventListener('load',()=>img.classList.add('img-loaded'));
      img.dataset.src=m.src;
      imgObs.observe(img);
      el.appendChild(img);
      el.insertAdjacentHTML('beforeend',ov);
    } else {
      const thumb=document.createElement('video');
      thumb.className='vid-thumb img-loaded';thumb.poster=m.poster||'';
      thumb.muted=true;thumb.loop=true;thumb.setAttribute('playsinline','');
      el.appendChild(thumb);
      el.insertAdjacentHTML('beforeend',`<div class="vid-badge-play"><i class="fas fa-play"></i></div>${ov}`);
      let vid=null;
      el.addEventListener('mouseenter',()=>{
        if(!vid && !IS_TOUCH){
          vid=document.createElement('video');
          vid.src=m.src;vid.muted=true;vid.loop=true;vid.setAttribute('playsinline','');
          vid.className='vid-thumb img-loaded';
          vid.play().catch(()=>{});
          thumb.replaceWith(vid);
        }
      });
      el.addEventListener('mouseleave',()=>{if(vid){vid.pause();vid.currentTime=0}});
    }
    el.addEventListener('click',()=>lb.open(list,idx));
    container.appendChild(el);
  });

  if(!IS_TOUCH){
    loadScript('https://cdnjs.cloudflare.com/ajax/libs/vanilla-tilt/1.7.0/vanilla-tilt.min.js').then(()=>{
      window.VanillaTilt?.init(container.querySelectorAll('.gallery-item'),{max:6,speed:400,glare:true,'max-glare':.08,scale:1.02});
    }).catch(()=>{});
  }
  return {media:list};
}

/* ── ENTER OVERLAY (splash logo-animation video, once per browser session) ── */
const ENTERED_KEY='epicalEntered';
export function initEnterOverlay(onDone){
  let entryDone=false;
  function triggerEntry(skipFade){
    if(entryDone)return; entryDone=true;
    sessionStorage.setItem(ENTERED_KEY,'1');
    const ov=document.getElementById('enter-overlay');
    if(!ov){onDone();return}
    if(skipFade){ov.style.display='none';onDone();return}
    ov.classList.add('eclipse-out');
    onDone();
    setTimeout(()=>{ov.style.display='none'},REDUCED_MOTION?0:1300);
  }
  if(sessionStorage.getItem(ENTERED_KEY)==='1'){triggerEntry(true);return}
  function scheduleEntry(){
    const ov=document.getElementById('enter-overlay');
    const video=ov?.querySelector('.eo-logo');
    const skip=document.getElementById('eo-skip');
    skip?.addEventListener('click',()=>triggerEntry(false));
    if(REDUCED_MOTION||!video){triggerEntry(false);return}
    video.addEventListener('ended',()=>triggerEntry(false),{once:true});
    video.play?.().catch(()=>{});
    /* safety fallback in case the video fails to load/play or 'ended' never fires */
    setTimeout(()=>triggerEntry(false),16000);
  }
  if(document.readyState==='complete'){scheduleEntry()}
  else{window.addEventListener('load',scheduleEntry)}
}

/* ── PAGE TRANSITIONS (eclipse fade between internal page navigations) ── */
export function initPageTransitions(){
  let el=document.getElementById('page-fade');
  if(!el){
    el=document.createElement('div');
    el.id='page-fade';
    el.innerHTML='<span class="pf-eclipse"></span>';
    document.body.appendChild(el);
  }
  requestAnimationFrame(()=>requestAnimationFrame(()=>{
    el.classList.add('pf-anim');
    el.classList.add('pf-in');
  }));
  if(REDUCED_MOTION)return;
  document.addEventListener('click',e=>{
    if(e.defaultPrevented||e.button!==0||e.metaKey||e.ctrlKey||e.shiftKey||e.altKey)return;
    const a=e.target.closest('a[href]');
    if(!a||a.target==='_blank')return;
    const href=a.getAttribute('href');
    if(!href||href.startsWith('#')||href.startsWith('mailto:')||href.startsWith('tel:'))return;
    let url;
    try{url=new URL(href,location.href)}catch{return}
    if(url.origin!==location.origin||url.pathname===location.pathname)return;
    e.preventDefault();
    el.classList.remove('pf-in');
    setTimeout(()=>{location.href=url.href},480);
  });
}

/* ── NESTED HERO CAROUSEL: outer = projects, inner = per-project media loop ── */
export function initHeroCarousel(root,projects){
  if(!root||!projects?.length)return;
  const slides=[...root.querySelectorAll('.hero-slide')];
  const dotsWrap=root.querySelector('.hero-dots');
  const tagName=root.querySelector('.hero-slide-tag .hs-name');
  const tagNum=root.querySelector('.hero-slide-tag .hs-num');
  let active=0, outerTimer=null, innerTimer=null, manifestCache={};

  function dots(){
    if(!dotsWrap)return;
    dotsWrap.innerHTML='';
    projects.forEach((p,i)=>{
      const b=document.createElement('button');
      b.className='hero-dot'+(i===active?' active':'');
      b.setAttribute('aria-label','Show '+p.name);
      b.addEventListener('click',()=>goTo(i,true));
      dotsWrap.appendChild(b);
    });
  }

  async function loadManifest(slug){
    if(manifestCache[slug])return manifestCache[slug];
    try{
      const res=await fetch(`/assets/projects/${slug}/manifest.json`);
      manifestCache[slug]=res.ok?await res.json():[];
    }catch{manifestCache[slug]=[]}
    return manifestCache[slug];
  }

  async function activateInner(slideEl,slug,skip){
    clearInterval(innerTimer);
    const s=skip||0;
    const media=(await loadManifest(slug)).filter(m=>m.type==='img').slice(s,s+6);
    const layer=slideEl.querySelector('.hero-slide-inner');
    if(!layer)return;
    if(!media.length)return;
    layer.innerHTML=media.map((m,i)=>`<img class="hero-slide-media${i===0?' active':''}" src="${m.src}" alt="${m.title||''}">`).join('');
    if(media.length<2||REDUCED_MOTION)return;
    let i=0;
    innerTimer=setInterval(()=>{
      const imgs=layer.querySelectorAll('.hero-slide-media');
      imgs[i].classList.remove('active');
      i=(i+1)%imgs.length;
      imgs[i].classList.add('active');
    },2600);
  }

  function goTo(i,manual){
    if(i===active && slides[active].classList.contains('active'))return;
    slides[active]?.classList.remove('active');
    active=i;
    const slide=slides[active];
    slide.classList.add('active');
    const p=projects[active];
    if(tagName)tagName.textContent=p.name;
    if(tagNum)tagNum.textContent=String(active+1).padStart(2,'0')+' / '+String(projects.length).padStart(2,'0');
    dots();
    activateInner(slide,p.slug,p.heroSkip);
    /* preload next project's manifest */
    loadManifest(projects[(active+1)%projects.length].slug);
    if(manual)restartAutoplay();
  }

  function restartAutoplay(){
    clearInterval(outerTimer);
    if(REDUCED_MOTION)return;
    outerTimer=setInterval(()=>goTo((active+1)%projects.length),7000);
  }

  document.addEventListener('visibilitychange',()=>{
    if(document.hidden)clearInterval(outerTimer);
    else restartAutoplay();
  });
  root.querySelector('.hero-arrow.prev')?.addEventListener('click',()=>goTo((active-1+projects.length)%projects.length,true));
  root.querySelector('.hero-arrow.next')?.addEventListener('click',()=>goTo((active+1)%projects.length,true));

  goTo(0,false);
  restartAutoplay();
}
