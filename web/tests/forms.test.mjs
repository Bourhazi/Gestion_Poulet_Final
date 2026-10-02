import assert from "node:assert/strict";
import { test } from "node:test";
import { createEditor } from "../src/features/forms/createEditor.ts";
import { serializeEditor } from "../src/features/forms/serializeEditor.ts";

const data = {
  suppliers: [{ id: 1, name: "Supplier" }],
  clients: [
    { id: 2, name: "Wholesale", type: "grossiste" },
    { id: 3, name: "Retail", type: "detail" },
  ],
  chambers: [{ id: 4, name: "SOUK", isSouk: true }],
  purchases: [],
  feed: [],
  sales: [],
  productPurchases: [],
  productSales: [],
  productStocks: [],
  users: [],
};
const dependencies = { data, lotId: 12, t: (key) => key };
const form = (kind, values) => ({
  ...createEditor(kind, undefined, dependencies),
  values,
});

test("every editable workflow has its endpoint and fields", () => {
  const endpoints = {
    suppliers: "suppliers",
    clients: "clients",
    chambers: "chambers",
    purchases: "purchases",
    feed: "feed",
    sales: "sales",
    "monday-lines": "monday-lines",
    users: "users",
    passwords: "passwords",
    "olive-purchase": "product-purchases",
    "egg-purchase": "product-purchases",
    "olive-sale": "product-sales",
    "egg-sale": "product-sales",
  };
  for (const [kind, endpoint] of Object.entries(endpoints)) {
    const editor = createEditor(kind, undefined, dependencies);
    assert.equal(editor.endpoint, endpoint, kind);
    assert.ok(editor.fields.length > 0, kind);
  }
});

test("editing a purchase preserves independent allocation values", () => {
  const purchase = {
    id: 1,
    date: "2026-10-02",
    quantity: 100,
    unitPrice: 10,
    chickenType: "normal",
    allocations: [{ chamberId: 4, quantity: 100 }],
  };
  const editor = createEditor("purchases", purchase, dependencies);
  editor.allocations[0].quantity = 75;
  assert.equal(purchase.allocations[0].quantity, 100);
  const payload = serializeEditor(editor);
  assert.equal(payload.quantity, 100);
  assert.deepEqual(payload.allocations, [
    { chamberId: 4, clientId: null, quantity: 75 },
  ]);
});

test("Monday lines use the selected lot and serialize identifiers and payment", () => {
  const editor = createEditor("monday-lines", undefined, dependencies);
  assert.equal(editor.values.saleId, "12");
  Object.assign(editor.values, {
    quantity: "5",
    unitPrice: "20",
    numbers: " A1, B2, , ",
    paid: "true",
  });
  const payload = serializeEditor(editor);
  assert.equal(payload.saleId, 12);
  assert.deepEqual(payload.numbers, ["A1", "B2"]);
  assert.equal(payload.paid, true);
});

test("Monday lots clear fields that belong to direct sales", () => {
  const payload = serializeEditor(
    form("sales", {
      id: "0",
      type: "lundi",
      chamberId: "4",
      clientId: "2",
      clientName: "Old client",
      unitPrice: "99",
      quantity: "50",
      pieces: "20",
    }),
  );
  assert.equal(payload.chamberId, null);
  assert.equal(payload.clientId, null);
  assert.equal(payload.clientName, null);
  assert.equal(payload.unitPrice, 0);
  assert.equal(payload.pieces, 20);
});

test("egg tray quantities and prices convert to the API unit representation", () => {
  const payload = serializeEditor(
    form("egg-sale", {
      quantity: "0.5",
      unitPrice: "300",
      eggsPerTray: "30",
      mode: "plateau",
    }),
  );
  assert.equal(payload.quantity, 15);
  assert.equal(payload.unitPrice, 10);
  const unit = serializeEditor(
    form("egg-sale", {
      quantity: "15",
      unitPrice: "10",
      eggsPerTray: "30",
      mode: "unite",
    }),
  );
  assert.equal(unit.quantity, 15);
  assert.equal(unit.unitPrice, 10);
});

test("editing an egg sale round-trips the saved tray quantity and price", () => {
  const sale = {
    id: 9,
    product: "egg",
    variety: "egg",
    date: "2026-10-02",
    quantity: 15,
    unitPrice: 10,
    eggsPerTray: 30,
    mode: "plateau",
    costOfGoods: 100,
  };
  const editor = createEditor("egg-sale", sale, dependencies);
  assert.equal(editor.values.quantity, "0.5");
  assert.equal(editor.values.unitPrice, "300");
  assert.equal(serializeEditor(editor).quantity, sale.quantity);
  assert.equal(serializeEditor(editor).unitPrice, sale.unitPrice);
});

test("admin users have no linked client; wholesale choices exclude retail clients", () => {
  const editor = createEditor("users", undefined, dependencies);
  assert.deepEqual(editor.fields.find((f) => f.key === "clientId").options, [
    { value: "2", label: "Wholesale" },
  ]);
  Object.assign(editor.values, { role: "admin", clientId: "2" });
  assert.equal(serializeEditor(editor).clientId, null);
});
