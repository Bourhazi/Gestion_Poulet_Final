import type { Person, Snapshot } from "../../types/models";
import { Cards } from "../../components/ui/Cards";
import { Table } from "../../components/ui/Table";
import { qty } from "../../utils/format";
import {
  clientAvailableQuantity,
  clientStockMovements,
} from "../../utils/clientStock";

export function ClientDetails({
  client,
  data,
  t,
}: {
  client: Person;
  data: Snapshot;
  t: (key: string) => string;
}) {
  const movements = clientStockMovements(data.purchases, client.id);
  const available = clientAvailableQuantity(data.purchases, client.id);

  return (
    <>
      <h2>{client.name}</h2>
      <Cards items={[[t("stock"), qty(available) + " " + t("kg")]]} />
      <h3>{t("stockMovements")}</h3>
      <Table
        heads={[t("date"), t("type"), t("quantity")]}
        rows={movements.map((movement) => [
          movement.date,
          t("externalAllocation") + " · " + t(movement.chickenType),
          "+" + qty(movement.quantity),
        ])}
        empty={t("empty")}
      />
    </>
  );
}
