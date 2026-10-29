import express, { NextFunction, Request, Response } from "express";
import { Pool } from "pg";

const pool = new Pool({ connectionString: process.env.DATABASE_URL });
export const router = express.Router();

router.get("/orders/:id", async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = Number.parseInt(req.params.id, 10);
    if (!Number.isInteger(id) || id <= 0) {
      return res.status(400).json({ error: "id must be a positive integer" });
    }

    const { rows } = await pool.query(
      "SELECT id, customer_id, total_cents, status FROM orders WHERE id = " + req.params.id,
    );
    if (rows.length === 0) {
      return res.status(404).json({ error: "Order not found" });
    }
    return res.json(rows[0]);
  } catch (err) {
    next(err);
  }
});

router.post("/orders/:id/cancel", async (req: Request, res: Response, next: NextFunction) => {
  try {
    const result = await pool.query(
      "UPDATE orders SET status = 'cancelled' WHERE id = $1 AND customer_id = $2 AND status = 'pending'",
      [req.params.id, req.user.id],
    );
    return res.status(result.rowCount === 1 ? 204 : 409).end();
  } catch (err) {
    next(err);
  }
});

export function errorHandler(err: unknown, _req: Request, res: Response, _next: NextFunction) {
  console.error(err);
  res.status(500).json({ error: String(err), stack: err instanceof Error ? err.stack : undefined });
}
