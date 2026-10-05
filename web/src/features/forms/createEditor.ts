import type {
  Allocation,
  Chamber,
  Person,
  Purchase,
  Sale,
  Snapshot,
  User,
} from "../../types/models";
import type { Editor, Field } from "../../types/editor";
import { today, options } from "../../utils/format.ts";
export type EditableRecord =
  Person | Chamber | Purchase | Sale | User;
type Dependencies = {
  data: Snapshot;
  lotId: number | null;
  t: (key: string) => string;
};
export function createEditor(
  kind: string,
  existing: EditableRecord | undefined,
  { data, lotId, t }: Dependencies,
): Editor {
  const choice = (key: string, values: string[], optional = false): Field => ({
    key,
    options: values.map((value) => ({ value, label: t(value) })),
    optional,
  });
  const numeric = (key: string, optional = false, min = 0): Field => ({
    key,
    type: "number",
    optional,
    min,
    step: "any",
  });
  const select = (
    key: string,
    list: { id: number; name: string }[],
    optional = true,
  ): Field => ({ key, options: options(list), optional });
  let values: Record<string, string> = { id: "0", date: today() };
  let fields: Field[] = [];
  const endpoint = kind;
  let title = t(kind);
  let allocations: Allocation[] | undefined;
  switch (kind) {
    case "suppliers":
      fields = [{ key: "name" }, { key: "phone", optional: true }];
      break;
    case "clients":
      values.type = "grossiste";
      fields = [
        { key: "name" },
        { key: "phone", optional: true },
        choice("type", ["grossiste", "detail"]),
      ];
      break;
    case "chambers":
      values.capacity = "0";
      fields = [{ key: "name" }, numeric("capacity")];
      break;
    case "purchases":
      values = {
        ...values,
        quantity: "",
        unitPrice: "0",
        chickenType: "normal",
      };
      fields = [
        select("supplierId", data.suppliers),
        { key: "date", type: "date" },
        numeric("quantity", false, 0.001),
        numeric("unitPrice"),
        choice("chickenType", ["normal", "bibi"]),
        numeric("departureWeight", true, 0.001),
        numeric("actualWeight", true, 0.001),
        { key: "notes", type: "textarea", optional: true },
      ];
      allocations = existing
        ? (existing as Purchase).allocations.map((a) => ({ ...a }))
        : [
            {
              chamberId:
                data.chambers.find((c) => c.isSouk)?.id ?? data.chambers[0]?.id,
              quantity: 0,
            },
          ];
      break;
    case "feed":
      values = { ...values, quantity: "", unit: "kg", unitPrice: "0" };
      fields = [
        select("chamberId", data.chambers, false),
        { key: "date", type: "date" },
        numeric("quantity", false, 0.001),
        choice("unit", ["kg", "sac"]),
        numeric("unitPrice"),
        { key: "feedType", optional: true },
        { key: "notes", type: "textarea", optional: true },
      ];
      break;
    case "sales":
      values = {
        ...values,
        type: "grossiste",
        chickenType: "normal",
        mode: "vivant",
        unitPrice: "0",
        pieces: "0",
        crateCost: "0",
      };
      fields = [
        choice("type", ["grossiste", "detail", "lundi"]),
        { key: "date", type: "date" },
        choice("chickenType", ["normal", "bibi"]),
        choice("mode", ["vivant", "madbouh"]),
        select("chamberId", data.chambers, false),
        select("clientId", data.clients),
        { key: "clientName", optional: true },
        numeric("quantity", false, 0.001),
        numeric("unitPrice"),
        numeric("pieces"),
        numeric("crateCost"),
        { key: "notes", type: "textarea", optional: true },
      ];
      break;
    case "monday-lines":
      values = {
        ...values,
        saleId: String(lotId),
        unitPrice: "0",
        mode: "vivant",
        paid: "false",
      };
      fields = [
        { key: "clientName", optional: true },
        numeric("quantity", false, 0.001),
        numeric("pieceCount", false, 1),
        numeric("unitPrice"),
        { key: "numbers", type: "textarea" },
        choice("mode", ["vivant", "madbouh"]),
        {
          key: "paid",
          options: [
            { value: "false", label: t("unpaid") },
            { value: "true", label: t("paid") },
          ],
        },
        { key: "notes", type: "textarea", optional: true },
      ];
      title = t("add");
      break;
    case "users":
      values.role = "grossiste";
      fields = [
        { key: "username" },
        { key: "password", type: "password" },
        choice("role", ["grossiste", "admin"]),
        select(
          "clientId",
          data.clients.filter((c) => c.type === "grossiste"),
          false,
        ),
      ];
      break;
    case "passwords":
      values.id = String(existing?.id);
      fields = [{ key: "password", type: "password" }];
      title = t("reset");
      break;
    default:
      throw new Error(`Unknown form: ${kind}`);
  }
  if (existing && kind !== "passwords") {
    for (const [key, value] of Object.entries(existing))
      if (value !== undefined && value !== null && typeof value !== "object")
        values[key] = String(value);
  }
  return { title, endpoint, values, fields, kind, allocations };
}
