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
  try {
    const { MsEdgeTTS, OUTPUT_FORMAT } = await import('msedge-tts');
    const tts = new MsEdgeTTS();
    await tts.setMetadata(voice, OUTPUT_FORMAT.AUDIO_24KHZ_96KBITRATE_MONO_MP3);
    const { audioStream } = tts.toStream(text);
    const chunks = [];
    await new Promise((resolve, reject) => {
      audioStream.on('data', (c) => chunks.push(c));
      audioStream.on('close', resolve);
      audioStream.on('error', reject);
    });
    const mp3 = Buffer.concat(chunks);
    res.setHeader('Content-Type', 'audio/mpeg');
    res.setHeader('Cache-Control', 'no-store');
    res.status(200).send(mp3);
  } catch (e) {
    res.status(502).json({ error: 'Voice service unavailable, try again' });
  }
};
