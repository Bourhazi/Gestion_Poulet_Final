import type { Chamber, Snapshot } from "../../types/models";
import { Cards } from "../../components/ui/Cards";
import { Table } from "../../components/ui/Table";
import { name, qty, money } from "../../utils/format";
export type ChamberProfit = {
  revenue: number;
  purchaseCost: number;
  costOfGoods: number;
  feedCost: number;
  feedSoldCost?: number;
  feedStockCost?: number;
  feedQuantityKg?: number;
  feedQuantityBags?: number;
  netProfit: number;
  weightDifference: number;
};
export function ChamberDetails({
  chamber: c,
  profit,
  data,
  t,
}: {
  chamber: Chamber;
  profit: ChamberProfit;
  data: Snapshot;
  t: (key: string) => string;
}) {
  const empty = t("empty");
  const purchases = data.purchases.filter((p) =>
    p.allocations.some((a) => a.chamberId === c.id),
  );
  const sales = data.sales.filter((s) =>
    s.allocations.some((a) => a.chamberId === c.id),
  );
  const feedSoldCost = profit.feedSoldCost ?? profit.feedCost ?? 0;
  const feedStockCost = profit.feedStockCost ?? 0;
  const feedQuantityKg = profit.feedQuantityKg ?? 0;
  const feedQuantityBags = profit.feedQuantityBags ?? 0;

  return (
    <>
      <h2>{c.name}</h2>
      <Cards
        items={[
          [t("stock"), qty(c.total) + " kg"],
          [t("normal"), qty(c.normal) + " kg"],
          [t("bibi"), qty(c.bibi) + " kg"],
          [t("revenue"), money(profit.revenue)],
          [t("purchasesCost"), money(profit.costOfGoods)],
          [t("feedSoldCost"), money(feedSoldCost)],
          [t("feedStockCost"), money(feedStockCost)],
          [
            t("feedQuantity"),
            `${qty(feedQuantityKg)} kg · ${qty(feedQuantityBags)} ${t("sac")}`,
          ],
          [t("profit"), money(profit.netProfit)],
        ]}
      />
      <p>
        {t("purchases")}: {money(profit.purchaseCost)} · LKOSSOURE:{" "}
        {qty(profit.weightDifference)} kg
      </p>
      <h3>{t("purchases")}</h3>
      <Table
        heads={[t("date"), t("supplierId"), t("quantity")]}
        rows={purchases.map((p) => [
          p.date,
          name(data.suppliers, p.supplierId),
          qty(
            p.allocations
              .filter((a) => a.chamberId === c.id)
              .reduce((n, a) => n + a.quantity, 0),
          ),
        ])}
        empty={empty}
      />
      <h3>{t("sales")}</h3>
      <Table
        heads={[t("date"), t("type"), t("clientId"), t("quantity")]}
        rows={sales.map((s) => [
          s.date,
          t(s.type),
          s.clientName || name(data.clients, s.clientId),
          qty(
            s.allocations
              .filter((a) => a.chamberId === c.id)
              .reduce((n, a) => n + a.quantity, 0),
          ),
        ])}
        empty={empty}
      />
    </>
  );
}
