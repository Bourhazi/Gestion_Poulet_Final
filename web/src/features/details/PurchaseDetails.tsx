import type { Purchase, Snapshot } from "../../types/models";
import { Cards } from "../../components/ui/Cards";
import { Table } from "../../components/ui/Table";
import { name, qty, money } from "../../utils/format";
export function PurchaseDetails({
  purchase: p,
  data,
  t,
}: {
  purchase: Purchase;
  data: Snapshot;
  t: (key: string) => string;
}) {
  const empty = t("empty");
  return (
    <>
      <Cards
        items={[
          [t("quantity"), qty(p.quantity) + " kg"],
          [t("cost"), money((p.actualWeight ?? p.quantity) * p.unitPrice)],
          [t("type"), t(p.chickenType)],
        ]}
      />
      <p>
        {p.date} · {name(data.suppliers, p.supplierId)}
      </p>
      <p>{p.notes}</p>
      <Table
        heads={[t("chamberId") + " / " + t("external"), t("quantity")]}
        rows={p.allocations.map((a) => [
          a.chamberId
            ? name(data.chambers, a.chamberId)
            : name(data.clients, a.clientId),
          qty(a.quantity),
        ])}
        empty={empty}
      />
    </>
  );
}
