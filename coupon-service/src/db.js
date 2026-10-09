/**
 * The one thing the service needs from a database: query(text, params) and
 * transaction(fn). PGlite (tests) has both natively; node-postgres / Neon get
 * this small adapter.
 */

export function pgAdapter(pool) {
  return {
    query: (text, params) => pool.query(text, params),
    async transaction(fn) {
      const client = await pool.connect();
      try {
        await client.query("BEGIN");
        const result = await fn({ query: (t, p) => client.query(t, p) });
        await client.query("COMMIT");
        return result;
      } catch (err) {
        await client.query("ROLLBACK").catch(() => {});
        throw err;
      } finally {
        client.release();
      }
    },
  };
}
