import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";

import { money, qty, name } from "../utils/format";

export function PurchasesPage() {
  const { t, data, date, empty, openForm, purchaseDetail, matches, actions } =
    usePage();

  return (
    <Table
      heads={[
        t("date"),
        t("supplierId"),
        t("chickenType"),
        t("quantity"),
        t("unitPrice"),
        t("total"),
        "",
      ]}
      empty={empty}
      rows={data.purchases
        .filter(
          (p) =>
            (!date || p.date === date) &&
            matches(name(data.suppliers, p.supplierId) + " " + (p.notes || "")),
        )
        .sort((a, b) => b.id - a.id)
        .map((p) => [
          p.date,
          name(data.suppliers, p.supplierId),
          t(p.chickenType),
          qty(p.quantity),
          money(p.unitPrice),
          money((p.actualWeight ?? p.quantity) * p.unitPrice),
          actions(
            "purchase",
            p.id,
            () => openForm("purchases", p),
            <button className="quiet" onClick={() => purchaseDetail(p)}>
              {t("detail")}
            </button>,
          ),
        ])}
    />
  );
}
