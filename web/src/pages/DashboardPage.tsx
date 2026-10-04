import { usePage } from "../hooks/usePage";
import { Cards } from "../components/ui/Cards";
import { money, qty, revenue, today } from "../utils/format";

export function DashboardPage({
  variant: page = "dashboard",
}: {
  variant?: "dashboard" | "souk";
}) {
  const { t, data, navigate, chamberDetail, salesTable } = usePage();

  const total = data.chambers.reduce((n, c) => n + c.total, 0);
  const todaySales = data.sales.filter((s) => s.date === today());
  const monthSales = data.sales.filter(
    (s) => s.date.slice(0, 7) === today().slice(0, 7),
  );
  return (
    <>
      <Cards
        items={
          page === "dashboard"
            ? [
                [t("stock"), qty(total) + " kg"],
                [
                  t("sales") + " · " + today(),
                  qty(todaySales.reduce((n, s) => n + s.quantity, 0)) + " kg",
                ],
                [
                  t("revenue") + " · " + today().slice(0, 7),
                  money(monthSales.reduce((n, s) => n + revenue(s), 0)),
                ],
                [t("purchases"), String(data.purchases.length)],
              ]
            : [
                [
                  t("stock") + " SOUK",
                  qty(data.chambers.find((c) => c.isSouk)?.total ?? 0) + " kg",
                ],
                [
                  t("bibi") + " SOUK",
                  qty(data.chambers.find((c) => c.isSouk)?.bibi ?? 0) + " kg",
                ],
              ]
        }
      />
      <div className="section-head">
        <h2>{t("chambers")}</h2>
        <button className="quiet" onClick={() => navigate("chambers")}>
          {t("detail")} →
        </button>
      </div>
      <div className="chamber-grid">
        {data.chambers
          .filter((c) => page !== "souk" || c.isSouk)
          .map((c) => (
            <article key={c.id}>
              <div className="section-head">
                <h3>{c.name}</h3>
                <span
                  className={"badge " + (c.total < 200 ? "amber" : "green")}
                >
                  {c.total < 200 ? t("low") : t("available")}
                </span>
              </div>
              <strong>
                {qty(c.total)} <small>kg</small>
              </strong>
              <p>
                {t("normal")}: {qty(c.normal)} · BIBI: {qty(c.bibi)}
              </p>
              <progress
                max={c.capacity || Math.max(c.total, 1)}
                value={c.total}
              />
              <button className="quiet" onClick={() => chamberDetail(c)}>
                {t("detail")}
              </button>
            </article>
          ))}
      </div>
      <h2>{t("sales")}</h2>
      {salesTable(
        data.sales
          .filter((s) => page !== "souk" || s.type === "lundi")
          .sort((a, b) => b.id - a.id)
          .slice(0, 8),
      )}
    </>
  );
}
