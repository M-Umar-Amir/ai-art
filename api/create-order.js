// POST /api/create-order — recomputes the total server-side so the amount
// handed to the Safepay Button isn't taken solely from client-editable state,
// then registers a payment session (tracker) with Safepay using the secret
// key, so the order is known to Safepay before the customer ever sees the
// Button. This is the real cross-check that both the public and secret
// sandbox keys are working end to end.
const Safepay = require('@sfpy/node-core');

const packPrices = { '6': 28, '12': 52, '24': 96 };

module.exports = async (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }

  let body = req.body;
  if (!body || typeof body === 'string') {
    try { body = JSON.parse(body || '{}'); } catch (e) { body = {}; }
  }

  const pack = packPrices[body.pack] ? body.pack : '6';
  const qty = Math.max(1, parseInt(body.qty, 10) || 1);
  const express = body.delivery === 'express';

  const subtotal = packPrices[pack] * qty;
  const shipping = express ? 12 : (subtotal >= 60 ? 0 : 6);
  const tax = +(subtotal * 0.08).toFixed(2);
  const total = +(subtotal + shipping + tax).toFixed(2);

  const orderId = 'ORV-' + Math.floor(100000 + Math.random() * 899999);
  const currency = 'USD';

  let tracker = null;
  let trackerError = null;

  const secretKey = process.env.SAFEPAY_SANDBOX_SECRET_KEY;
  const publicKey = process.env.SAFEPAY_SANDBOX_PUBLIC_KEY;

  if (secretKey && publicKey) {
    try {
      const safepay = Safepay(secretKey, {
        authType: 'secret',
        host: 'https://sandbox.api.getsafepay.com'
      });

      const session = await safepay.payments.session.setup({
        merchant_api_key: publicKey,
        intent: 'CYBERSOURCE',
        mode: 'payment',
        entry_mode: 'raw',
        currency,
        amount: Math.round(total * 100), // minor units
        metadata: { order_id: orderId }
      });

      const passport = await safepay.client.passport.create();

      tracker = {
        session: session && session.data ? session.data : session,
        token: passport && passport.data ? passport.data : passport
      };
    } catch (err) {
      trackerError = err.message || String(err);
      console.error('[safepay] session.setup failed:', trackerError);
    }
  } else {
    trackerError = 'SAFEPAY_SANDBOX_SECRET_KEY or SAFEPAY_SANDBOX_PUBLIC_KEY not set';
  }

  res.status(200).json({
    orderId,
    currency,
    amount: total,
    breakdown: { subtotal, shipping, tax },
    tracker,
    trackerError
  });
};
