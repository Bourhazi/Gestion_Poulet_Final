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
        t("unit"),
        t("unitPrice"),
        t("totalCost"),
        t("feedType"),
        t("notes"),
        "",
      ]}
      empty={empty}
      rows={data.feed
        .filter(
          (f) =>
            (!date || f.date === date) &&
            matches(
              name(data.chambers, f.chamberId) +
                " " +
                (f.feedType || "") +
                " " +
                (f.notes || ""),
            ),
        )
        .sort((a, b) => b.id - a.id)
        .map((f) => [
          f.date,
          name(data.chambers, f.chamberId),
          qty(f.quantity),
          t(f.unit),
          money(f.unitPrice),
          money(f.cost),
          f.feedType || "—",
          f.notes,
          actions("feed", f.id),
        ])}
    />
  );
}
