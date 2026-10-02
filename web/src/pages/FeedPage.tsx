import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";

import { money, qty, name } from "../utils/format";

export function FeedPage() {
  const { t, data, date, empty, matches, actions } = usePage();

  return (
    <Table
      heads={[
        t("date"),
        t("chamberId"),
        t("quantity"),
        t("cost"),
        t("notes"),
        "",
      ]}
      empty={empty}
      rows={data.feed
        .filter(
          (f) =>
            (!date || f.date === date) &&
            matches(name(data.chambers, f.chamberId) + " " + (f.notes || "")),
        )
        .sort((a, b) => b.id - a.id)
        .map((f) => [
          f.date,
          name(data.chambers, f.chamberId),
          qty(f.quantity),
          money(f.cost),
          f.notes,
          actions("feed", f.id),
        ])}
    />
  );
}
