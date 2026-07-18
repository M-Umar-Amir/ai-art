// POST /api/mark-paid — called from the Button's onPayment callback.
// This repo has no order-storage/database yet, so this only logs to the
// Vercel function log for now. Replace with a real write (DB, sheet, etc.)
// before relying on this for actual fulfillment.
module.exports = (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }

  let body = req.body;
  if (!body || typeof body === 'string') {
    try { body = JSON.parse(body || '{}'); } catch (e) { body = {}; }
  }

  console.log('[safepay] order marked paid:', JSON.stringify(body));
  res.status(200).json({ ok: true });
};
