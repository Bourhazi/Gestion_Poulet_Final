import type { Editor } from "../../types/editor";
export function serializeEditor(editor: Editor): Record<string, unknown> {
  const body: Record<string, unknown> = {};
  const v = editor.values;
  for (const [key, value] of Object.entries(v)) body[key] = value || null;
  for (const key of [
    "id",
    "saleId",
    "supplierId",
    "clientId",
    "chamberId",
    "quantity",
    "unitPrice",
    "pieces",
    "crateCost",
    "departureWeight",
    "actualWeight",
    "cost",
    "capacity",
  ])
    if (key in v) body[key] = v[key] === "" ? null : Number(v[key]);
  if (editor.allocations)
    body.allocations = editor.allocations.map((a) => ({
      chamberId: a.chamberId ?? null,
      clientId: a.clientId ?? null,
      quantity: a.quantity,
    }));
  if (editor.kind === "monday-lines") {
    body.numbers = (v.numbers || "")
      .split(",")
      .map((n) => n.trim())
      .filter(Boolean);
    body.paid = v.paid === "true";
  }
  if (editor.kind === "sales" && v.type === "lundi") {
    body.chamberId = null;
    body.clientId = null;
    body.clientName = null;
    body.unitPrice = 0;
  }
  if (editor.kind === "users" && v.role === "admin") body.clientId = null;

  return body;
}
