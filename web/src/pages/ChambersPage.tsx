import { usePage } from "../hooks/usePage";

import { qty } from "../utils/format";

export function ChambersPage() {
  const { t, data, openForm, chamberDetail, matches, actions } = usePage();

  return (
    <div className="chamber-grid">
      {data.chambers
        .filter((c) => matches(c.name))
        .map((c) => (
          <article key={c.id}>
            <h2>{c.name}</h2>
            <strong>
              {qty(c.total)} <small>kg</small>
            </strong>
            <p>
              {t("capacity")}: {c.capacity || "∞"}
            </p>
            <p>
              {t("normal")}: {qty(c.normal)} · BIBI: {qty(c.bibi)}
            </p>
            <progress
              max={c.capacity || Math.max(c.total, 1)}
              value={c.total}
            />
            {actions(
              "chamber",
              c.id,
              () => openForm("chambers", c),
              <button className="quiet" onClick={() => chamberDetail(c)}>
                {t("detail")}
              </button>,
            )}
          </article>
        ))}
    </div>
  );
}
