/**
 * The room, running in the browser — for static hosting.
 *
 * The hosted build runs this room as a Cloudflare Durable Object (src/room.ts),
 * which is what makes the points ledger server-authoritative. A static host such
 * as Vercel has nowhere to put that, so this file implements the SAME wire
 * protocol locally: join / action / reset, seating, spectator refusal, and the
 * account ledger persisted in localStorage.
 *
 * The rules are not reimplemented here. `rules.js` is a verbatim copy of
 * src/logic.js, so validateAction, applyAction and viewFor — including the
 * replay check that decides whether a run is credited — are the same code that
 * runs on the server.
 *
 * Honest limit, and it should be said out loud: the ledger lives in the
 * visitor's browser, so a determined visitor can edit it. Points that must be
 * trustworthy have to come from the hosted build, where the ledger is
 * server-side, every run is replayed there, and a coupon cannot be fabricated.
 */

import * as logic from "./rules.js";

const KEY = "dash:ledger:v1";

export function startLocalRoom({ onState, onError, playerId, storage }) {
  const store = storage || window.localStorage;
  const meta = {
    game: logic.meta.game,
    minPlayers: logic.meta.minPlayers,
    maxPlayers: logic.meta.maxPlayers,
  };

  function blank() {
    return { v: 1, players: [], state: null, status: "waiting", result: null };
  }

  function load() {
    try {
      const raw = store.getItem(KEY);
      if (raw) {
        const parsed = JSON.parse(raw);
        if (parsed && parsed.v === 1 && Array.isArray(parsed.players) && parsed.state) {
          return parsed;
        }
      }
    } catch {
      // Storage can be blocked (private mode, locked-down frame). The session
      // still plays; it just will not resume.
    }
    return blank();
  }

  function save() {
    try {
      store.setItem(KEY, JSON.stringify(game));
    } catch {
      /* nothing to do */
    }
  }

  let game = load();

  function emit() {
    onState({
      type: "state",
      status: game.status,
      seats: game.players,
      you: playerId,
      connected: 1,
      view: game.state ? logic.viewFor(game.state, playerId) : null,
      result: game.result,
      meta,
    });
  }

  function seat() {
    if (!game.players.includes(playerId) && game.players.length < meta.maxPlayers) {
      game.players.push(playerId);
      if (game.status === "waiting" && game.players.length >= meta.minPlayers) {
        game.state = logic.setup(game.players);
        game.status = "playing";
      }
    }
  }

  seat();
  save();
  emit();

  return {
    send(msg) {
      if (!msg || typeof msg !== "object") return;

      if (msg.type === "join") {
        seat();
        save();
        emit();
        return;
      }

      if (msg.type === "action") {
        if (game.status !== "playing") return onError("game is not in progress");
        if (!game.players.includes(playerId)) return onError("spectators cannot act");
        const verdict = logic.validateAction(game.state, playerId, msg.action);
        if (!verdict.ok) return onError(verdict.error || "invalid action");
        game.state = logic.applyAction(game.state, playerId, msg.action);
        const end = logic.isGameOver(game.state);
        if (end.over) {
          game.status = "over";
          game.result = end;
        }
        save();
        emit();
        return;
      }

      if (msg.type === "reset") {
        game = blank();
        seat();
        save();
        emit();
      }
    },
  };
}
