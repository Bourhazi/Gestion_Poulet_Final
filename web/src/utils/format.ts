import type { Sale } from "../types/models";
export const today = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
};
export const monday = () => {
  const d = new Date();
  d.setDate(d.getDate() + ((8 - d.getDay()) % 7));
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
};
export const money = (n?: number | null) =>
  `${(n ?? 0).toLocaleString(undefined, { maximumFractionDigits: 2 })} DA`;
export const qty = (n?: number | null) =>
  (n ?? 0).toLocaleString(undefined, { maximumFractionDigits: 3 });
export const revenue = (s: Sale) =>
  s.type === "lundi"
    ? s.lines.reduce((n, l) => n + l.quantity * l.unitPrice, 0)
    : s.quantity * s.unitPrice;

export const name = (
  list: { id: number; name: string }[],
  id?: number | null,
) => list.find((p) => p.id === id)?.name || "—";
export const options = (list: { id: number; name: string }[]) =>
  list.map((p) => ({ value: String(p.id), label: p.name }));
