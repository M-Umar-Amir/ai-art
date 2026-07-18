// GET /api/config — hands the browser the Safepay public/client key.
// The secret key never leaves the server; this key is designed by Safepay
// to be exposed client-side (used directly in their Button component).
module.exports = (req, res) => {
  const publicKey = process.env.SAFEPAY_SANDBOX_PUBLIC_KEY || '';
  if (!publicKey) {
    res.status(500).json({ error: 'SAFEPAY_SANDBOX_PUBLIC_KEY is not set' });
    return;
  }
  res.status(200).json({ env: 'sandbox', publicKey });
};
