// POST /api/automation-tts — text → MP3 voice-over for the Campaign Studio AI
// demo at /automation. Uses Microsoft Edge's free neural voices (Urdu ur-PK and
// English). Small, stateless and size-capped: one VO script per request.
const VOICES = new Set([
  'ur-PK-UzmaNeural',
  'ur-PK-AsadNeural',
  'en-US-AvaNeural',
  'en-US-AndrewNeural',
  'en-GB-SoniaNeural',
  'en-IN-NeerjaNeural',
]);

async function synth(text, voice) {
  const { MsEdgeTTS, OUTPUT_FORMAT } = await import('msedge-tts');
  const tts = new MsEdgeTTS();
  await tts.setMetadata(voice, OUTPUT_FORMAT.AUDIO_24KHZ_96KBITRATE_MONO_MP3);
  const { audioStream } = tts.toStream(text);
  const chunks = [];
  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('timeout')), 45000);
    audioStream.on('data', (c) => chunks.push(c));
    audioStream.on('end', () => { clearTimeout(timer); resolve(); });
    audioStream.on('close', () => { clearTimeout(timer); resolve(); });
    audioStream.on('error', (e) => { clearTimeout(timer); reject(e); });
  });
  try { tts.close(); } catch (e) {}
  return Buffer.concat(chunks);
}

module.exports = async (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }
  let body = req.body;
  if (!body || typeof body === 'string') {
    try { body = JSON.parse(body || '{}'); } catch (e) { body = {}; }
  }
  const text = String(body.text || '').trim().slice(0, 2500);
  const voice = VOICES.has(body.voice) ? body.voice : 'ur-PK-UzmaNeural';
  if (!text) {
    res.status(400).json({ error: 'No text' });
    return;
  }
  let lastError = 'empty audio';
  for (let attempt = 0; attempt < 2; attempt++) {
    try {
      const mp3 = await synth(text, voice);
      if (mp3.length > 1000) {
        res.setHeader('Content-Type', 'audio/mpeg');
        res.setHeader('Cache-Control', 'no-store');
        res.status(200).send(mp3);
        return;
      }
    } catch (e) {
      lastError = String((e && e.message) || e).slice(0, 200);
    }
  }
  res.status(502).json({ error: 'Voice service unavailable, try again', detail: lastError });
};
