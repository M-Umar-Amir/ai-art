/**
 * HTTP surface, as web-standard (Request) => Response handlers, so the same
 * code mounts on Vercel Functions, Hono or Express (via a Request adapter).
 *
 *   POST /coupons/issue      game app     { issueKey }                     → coupon
 *   GET  /coupons            game + app   —                                → my coupons
 *   POST /coupons/validate   KFC app      { code, subtotalCents }          → discount preview
 *   POST /coupons/reserve    KFC app      { code, orderId, subtotalCents } → lock to order
 *   POST /coupons/redeem     order svc    { orderId }   (payment captured)
 *   POST /coupons/release    order svc    { orderId }   (payment failed / abandoned)
 *   POST /coupons/restore    order svc    { orderId }   (restaurant cancelled)
 *
 * `auth(request)` must return the signed-in user's id from the shared identity
 * provider's token (both apps use the same accounts), or null. Service-to-
 * service calls (redeem/release/restore) are authorised by `isService`.
 */

import { CouponError } from "./service.js";

const STATUS = {
  malformed: 400,
  not_found: 404,
  used: 409,
  in_use: 409,
  inactive: 410,
  expired: 410,
  min_order: 422,
  insufficient_points: 402,
  limit: 409,
  rate_limited: 429,
  not_reserved: 409,
};

const json = (body, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json", "cache-control": "no-store" } });

export function createHandler({ service, auth, isService }) {
  const routes = {
    "POST /coupons/issue": async (req, user) => service.issue({ userId: user, issueKey: (await req.json()).issueKey }),
    "GET /coupons": async (_req, user) => ({ coupons: await service.list(user) }),
    "POST /coupons/validate": async (req, user) => {
      const b = await req.json();
      return service.validate({ userId: user, code: b.code, subtotalCents: b.subtotalCents | 0 });
    },
    "POST /coupons/reserve": async (req, user) => {
      const b = await req.json();
      return service.reserve({ userId: user, code: b.code, orderId: String(b.orderId), subtotalCents: b.subtotalCents | 0 });
    },
  };
  const serviceRoutes = {
    "POST /coupons/redeem": async (req) => service.redeem({ orderId: String((await req.json()).orderId) }),
    "POST /coupons/release": async (req) => service.release({ orderId: String((await req.json()).orderId) }),
    "POST /coupons/restore": async (req) => service.restore({ orderId: String((await req.json()).orderId) }),
  };

  return async function handle(req) {
    const url = new URL(req.url);
    const key = `${req.method} ${url.pathname.replace(/\/$/, "")}`;
    try {
      if (serviceRoutes[key]) {
        if (!(await isService(req))) return json({ error: "forbidden" }, 403);
        return json(await serviceRoutes[key](req));
      }
      if (routes[key]) {
        const user = await auth(req);
        if (!user) return json({ error: "sign in required" }, 401);
        return json(await routes[key](req, user));
      }
      return json({ error: "not found" }, 404);
    } catch (err) {
      if (err instanceof CouponError) return json({ error: err.code, message: err.message }, STATUS[err.code] || 400);
      console.error(err);
      return json({ error: "server_error" }, 500);
    }
  };
}
