import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";
import { Cards } from "../components/ui/Cards";
import { money, qty } from "../utils/format";

export function ReportsPage() {
  const { t, report, empty } = usePage();

  // An API instance may be restarted after the web bundle is deployed. Until
  // then, older report responses do not contain the new Feed breakdown.
  const values = report && {
    feedSoldCost: report.feedCost ?? 0,
    feedPeriodCost: report.feedPeriodCost ?? report.feedCost ?? 0,
    feedStockCost: report.feedStockCost ?? 0,
    otherExpenses: report.otherExpenses ?? report.crateCost ?? 0,
    losses: report.losses ?? 0,
    grossProfit: report.grossProfit ?? report.revenue - report.costOfGoods,
  };

  return report && values ? (
    <>
      <Cards
        items={[
          [t("revenue"), money(report.revenue)],
          [t("purchasesCost"), money(report.costOfGoods)],
          [t("feedSoldCost"), money(values.feedSoldCost)],
          [t("feedPeriodCost"), money(values.feedPeriodCost)],
          [t("feedStockCost"), money(values.feedStockCost)],
          [t("otherExpenses"), money(values.otherExpenses)],
          [t("losses"), money(values.losses)],
          [t("grossProfit"), money(values.grossProfit)],
          [t("profit"), money(report.netProfit)],
          [t("sales") + " kg", qty(report.chickenKg)],
          [t("paidMonday"), money(report.paidMonday)],
          [t("unpaidMonday"), money(report.unpaidMonday)],
        ]}
      />
      <article className="chart">
        <h2>{t("revenue")}</h2>
        {report.byDay.length ? (
          report.byDay.map((d) => (
            <div className="bar-row" key={d.date}>
              <span>{d.date}</span>
              <div>
                <i
                  style={{
                    width:
                      Math.max(
                        1,
                        (d.revenue /
                          Math.max(...report.byDay.map((x) => x.revenue), 1)) *
                          100,
                      ) + "%",
                  }}
                />
              </div>
              <strong>{money(d.revenue)}</strong>
            </div>
          ))
        ) : (
          <p className="empty">{empty}</p>
        )}
      </article>
      <h2>{t("topClients")}</h2>
      <Table
        heads={[t("name"), t("quantity"), t("revenue")]}
        rows={report.topClients.map((c) => [
          c.name,
          qty(c.quantity),
          money(c.revenue),
        ])}
        empty={empty}
      />
      <h2>{t("suppliers")}</h2>
      <Table
        heads={[t("name"), t("purchases"), t("quantity"), t("cost")]}
        rows={report.suppliers.map((s) => [
          s.name,
          s.count,
          qty(s.quantity),
          money(s.cost),
        ])}
        empty={empty}
      />
    </>
  ) : (
    <p>{t("loading")}</p>
  );
}
