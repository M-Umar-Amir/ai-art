/* Converts curated per-project manifests (manifests/<slug>.json) into compressed,
   web-ready assets under assets/projects/<slug>/{img,vid,poster} + a shipped manifest.json.
   Run: node scripts/build-project-assets.js [slug ...]   (no args = all manifests found) */

const ffmpeg = require('fluent-ffmpeg');
const ffmpegPath = require('ffmpeg-static');
const path = require('path');
const fs = require('fs');

ffmpeg.setFfmpegPath(ffmpegPath);

const ROOT = path.join(__dirname, '..');
const MANIFESTS_DIR = path.join(ROOT, 'manifests');
const OUT_DIR = path.join(ROOT, 'assets', 'projects');

function pad(n){return String(n).padStart(2,'0')}

function needsBuild(srcPath, outPath){
  if(!fs.existsSync(outPath))return true;
  try{
    return fs.statSync(srcPath).mtimeMs > fs.statSync(outPath).mtimeMs;
  }catch{return true}
}

function convertImage(src, out){
  return new Promise((resolve,reject)=>{
    ffmpeg(src)
      .outputOptions(['-vf','scale=1600:-2','-quality','78'])
      .output(out)
      .on('end',resolve)
      .on('error',reject)
      .run();
  });
}

function convertVideo(src, out){
  return new Promise((resolve,reject)=>{
    ffmpeg(src)
      .videoCodec('libx264')
      .audioCodec('aac')
      .size('?x720')
      .outputOptions(['-crf 30','-preset slow','-movflags +faststart','-t 12'])
      .audioBitrate('96k')
      .output(out)
      .on('end',resolve)
      .on('error',reject)
      .run();
  });
}

function grabPoster(src, out){
  return new Promise((resolve,reject)=>{
    ffmpeg(src)
      .on('end',resolve)
      .on('error',reject)
      .screenshots({timestamps:['00:00:01'], filename:path.basename(out), folder:path.dirname(out), size:'?x720'});
  });
}

async function buildProject(slug){
  const manifestPath = path.join(MANIFESTS_DIR, `${slug}.json`);
  if(!fs.existsSync(manifestPath)){
    console.log(`[${slug}] no manifest found, skipping`);
    return;
  }
  const raw = fs.readFileSync(manifestPath,'utf8');
  const picks = JSON.parse(raw.charCodeAt(0) === 0xFEFF ? raw.slice(1) : raw);
  const imgDir = path.join(OUT_DIR, slug, 'img');
  const vidDir = path.join(OUT_DIR, slug, 'vid');
  const posterDir = path.join(OUT_DIR, slug, 'poster');
  fs.mkdirSync(imgDir,{recursive:true});
  fs.mkdirSync(vidDir,{recursive:true});
  fs.mkdirSync(posterDir,{recursive:true});

  const shipped = [];
  let imgN=0, vidN=0;
  for(const pick of picks){
    if(!fs.existsSync(pick.src)){
      console.warn(`[${slug}] missing source, skipping: ${pick.src}`);
      continue;
    }
    if(pick.type==='img'){
      imgN++;
      const outName = `${pad(imgN)}.webp`;
      const outPath = path.join(imgDir, outName);
      if(needsBuild(pick.src, outPath)){
        console.log(`[${slug}] image ${imgN}: ${path.basename(pick.src)} -> ${outName}`);
        await convertImage(pick.src, outPath);
      }
      shipped.push({type:'img', src:`/assets/projects/${slug}/img/${outName}`, title:pick.title||'', tag:'Image'});
    } else if(pick.type==='vid'){
      vidN++;
      const outName = `${pad(vidN)}.mp4`;
      const posterName = `${pad(vidN)}.jpg`;
      const outPath = path.join(vidDir, outName);
      const posterPath = path.join(posterDir, posterName);
      if(needsBuild(pick.src, outPath)){
        console.log(`[${slug}] video ${vidN}: ${path.basename(pick.src)} -> ${outName}`);
        await convertVideo(pick.src, outPath);
      }
      if(needsBuild(pick.src, posterPath)){
        await grabPoster(pick.src, posterPath);
      }
      shipped.push({type:'vid', src:`/assets/projects/${slug}/vid/${outName}`, poster:`/assets/projects/${slug}/poster/${posterName}`, title:pick.title||'', tag:'Video'});
    }
  }

  fs.writeFileSync(path.join(OUT_DIR, slug, 'manifest.json'), JSON.stringify(shipped,null,2));
  console.log(`[${slug}] done — ${imgN} images, ${vidN} videos`);
}

async function main(){
  const argSlugs = process.argv.slice(2);
  let slugs = argSlugs;
  if(!slugs.length){
    if(!fs.existsSync(MANIFESTS_DIR)){
      console.error('No manifests/ directory found — nothing to build.');
      process.exit(1);
    }
    slugs = fs.readdirSync(MANIFESTS_DIR).filter(f=>f.endsWith('.json')).map(f=>f.replace(/\.json$/,''));
  }
  if(!slugs.length){
    console.log('No manifest files found in manifests/.');
    return;
  }
  for(const slug of slugs){
    await buildProject(slug);
  }
}

main().catch(err=>{console.error(err);process.exit(1)});
