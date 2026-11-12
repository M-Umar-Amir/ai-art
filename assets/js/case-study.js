import {openLightbox} from '/assets/js/site.js';

/* Lite YouTube: show the thumbnail, swap in the iframe only on click. */
export function initYouTubeLite(root=document){
  root.querySelectorAll('.yt-lite[data-yt]').forEach(el=>{
    el.addEventListener('click',()=>{
      if(el.classList.contains('playing'))return;
      const id=el.dataset.yt;
      const f=document.createElement('iframe');
      f.src=`https://www.youtube-nocookie.com/embed/${id}?autoplay=1&rel=0&modestbranding=1&playsinline=1`;
      f.title=el.getAttribute('aria-label')||'YouTube video';
      f.allow='accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
      f.allowFullscreen=true;
      el.appendChild(f);
      el.classList.add('playing');
      /* pause the ambient music so audio doesn't overlap */
      const a=document.getElementById('m-audio');
      if(a&&!a.paused)document.getElementById('m-toggle')?.click();
    });
    /* maxres thumbnail is missing on some uploads: fall back to hq */
    const img=el.querySelector('img');
    img?.addEventListener('error',()=>{if(!img.src.includes('hqdefault'))img.src=`https://i.ytimg.com/vi/${el.dataset.yt}/hqdefault.jpg`},{once:true});
  });
}

/* Every .cs-figure on the page opens in the shared lightbox, in page order. */
export function initFigureLightbox(root=document){
  const figs=[...root.querySelectorAll('.cs-figure')];
  const media=figs.map(f=>{const img=f.querySelector('img');return{type:'img',src:img.dataset.full||img.src,title:f.querySelector('figcaption')?.textContent||img.alt}});
  figs.forEach((f,i)=>{
    f.tabIndex=0;f.setAttribute('role','button');
    const go=()=>openLightbox(media,i);
    f.addEventListener('click',go);
    f.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();go()}});
  });
}

/* Inline videos play only while on screen (saves data and CPU on phones). */
export function initInlineVideos(root=document){
  const vids=root.querySelectorAll('video[data-autoplay]');
  const io=new IntersectionObserver(es=>es.forEach(e=>{
    const v=e.target;
    if(e.isIntersecting){if(v.preload==='none')v.preload='auto';v.play().catch(()=>{})}
    else v.pause();
  }),{threshold:.25});
  vids.forEach(v=>io.observe(v));
}
