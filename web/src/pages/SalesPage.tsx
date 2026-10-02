import { usePage } from "../hooks/usePage";

import { name } from "../utils/format";

export function SalesPage() {
  const { data, date, type, matches, salesTable } = usePage();

  return salesTable(
    data.sales
      .filter(
        (s) =>
          (!date || s.date === date) &&
          (!type || s.type === type) &&
          matches(
            (s.clientName || "") +
              " " +
              name(data.clients, s.clientId) +
              " " +
              (s.notes || ""),
          ),
      )
      .sort((a, b) => b.id - a.id),
  );
}
