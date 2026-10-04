import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";

import { qty } from "../utils/format";
import { clientAvailableQuantity } from "../utils/clientStock";

export function PeoplePage({ kind: page }: { kind: "suppliers" | "clients" }) {
  const { t, data, empty, openForm, matches, actions, clientDetail } = usePage();

  const list = page === "suppliers" ? data.suppliers : data.clients;
  return (
    <Table
      heads={[
        t("name"),
        t("phone"),
        ...(page === "clients" ? [t("type")] : []),
        t("quantity"),
        "",
      ]}
      empty={empty}
      rows={list
        .filter((p) => matches(p.name + " " + (p.phone || "")))
        .map((p) => [
          p.name,
          p.phone || "—",
          ...(page === "clients" ? [t(p.type || "")] : []),
          qty(
            page === "suppliers"
              ? data.purchases
                  .filter((a) => a.supplierId === p.id)
                  .reduce((n, a) => n + a.quantity, 0)
              : clientAvailableQuantity(data.purchases, p.id),
          ),
          actions(
            page === "suppliers" ? "supplier" : "client",
            p.id,
            () => openForm(page, p),
            page === "clients" ? (
              <button className="quiet" onClick={() => clientDetail(p)}>
                {t("detail")}
              </button>
            ) : undefined,
          ),
        ])}
    />
  );
}
