import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";
import { Cards } from "../components/ui/Cards";
import { money, qty, name } from "../utils/format";

export function ProductsPage({ kind: page }: { kind: "eggs" | "olives" }) {
  const { t, data, date, type, empty, openForm, matches, actions } = usePage();

  const product = page === "eggs" ? "egg" : "olive";
  const pa = data.productPurchases.filter((p) => p.product === product);
  const ps = data.productSales.filter((s) => s.product === product);
  const stock = data.productStocks.filter((s) => s.product === product);
  return (
    <>
      <Cards
        items={stock.map((s) => [
          t(s.variety),
          qty(s.quantity) +
            (product === "egg"
              ? ` · ${qty(s.quantity / 30)} ${t("plateau")}`
              : " kg"),
        ])}
      />
      <Cards
        items={[
          [
            t("revenue"),
            money(ps.reduce((n, s) => n + s.quantity * s.unitPrice, 0)),
          ],
          [
            t("profit"),
            money(
              ps.reduce(
                (n, s) => n + s.quantity * s.unitPrice - s.costOfGoods,
                0,
              ),
            ),
          ],
        ]}
      />
      <div className="section-head">
        <h2>{t("purchases")}</h2>
        <button onClick={() => openForm(product + "-purchase")}>
          + {t("add")}
        </button>
      </div>
      <Table
        heads={[
          t("date"),
          t("supplierId"),
          t("variety"),
          product === "egg" ? t("eggCount") : t("quantity"),
          t("total"),
          "",
        ]}
        empty={empty}
        rows={pa
          .filter(
            (p) =>
              (!date || p.date === date) &&
              (!type || p.variety === type) &&
              matches(
                name(data.suppliers, p.supplierId) + " " + (p.notes || ""),
              ),
          )
          .sort((a, b) => b.id - a.id)
          .map((p) => [
            p.date,
            name(data.suppliers, p.supplierId),
            t(p.variety),
            qty(p.quantity),
            money(p.quantity * p.unitPrice),
            actions("product-purchase", p.id),
          ])}
      />
      <div className="section-head">
        <h2>{t("sales")}</h2>
        <button onClick={() => openForm(product + "-sale")}>
          + {t("add")}
        </button>
      </div>
      <Table
        heads={[
          t("date"),
          t("variety"),
          t("mode"),
          product === "egg" ? t("eggCount") : t("quantity"),
          t("total"),
          "",
        ]}
        empty={empty}
        rows={ps
          .filter(
            (s) =>
              (!date || s.date === date) &&
              (!type || s.variety === type) &&
              matches(s.notes || s.variety),
          )
          .sort((a, b) => b.id - a.id)
          .map((s) => [
            s.date,
            t(s.variety),
            t(s.mode),
            qty(s.quantity),
            money(s.quantity * s.unitPrice),
            actions(
              "product-sale",
              s.id,
              product === "egg" ? () => openForm("egg-sale", s) : undefined,
            ),
          ])}
      />
    </>
  );
}
