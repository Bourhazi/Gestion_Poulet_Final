import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";
import { Cards } from "../components/ui/Cards";
import { money, qty, revenue, name } from "../utils/format";

export function MondaySalePage() {
  const {
    t,
    data,
    busy,
    setSearch,
    payment,
    setPayment,
    setLotId,
    empty,
    mutate,
    openForm,
    receipt,
    matches,
    lot,
    actions,
  } = usePage();
  if (!lot) return null;
  const kg = lot.lines.reduce((n, l) => n + l.quantity, 0);
  const pieces = lot.lines.reduce((n, l) => n + l.pieces.length, 0);
  const paid = lot.lines
    .filter((l) => l.paid)
    .reduce((n, l) => n + l.quantity * l.unitPrice, 0);
  return (
    <>
      <div className="section-head">
        <button
          className="quiet"
          onClick={() => {
            setLotId(null);
            setSearch("");
            setPayment("");
          }}
        >
          ← {t("sales")}
        </button>
        <h2>
          {t("lundi")} · {lot.date} · #{lot.id}
        </h2>
        <button onClick={() => openForm("monday-lines")}>+ {t("add")}</button>
      </div>
      <Cards
        items={[
          [t("remaining") + " · kg", qty(lot.quantity - kg)],
          [t("remaining") + " · " + t("pieces"), String(lot.pieces - pieces)],
          [t("revenue"), money(revenue(lot))],
          [
            t("profit"),
            money(
              revenue(lot) -
                (lot.costOfGoods * kg) / lot.quantity -
                lot.crateCost,
            ),
          ],
          [t("paid"), money(paid)],
          [t("unpaid"), money(revenue(lot) - paid)],
        ]}
      />
      <p>
        {t("distributions")}:{" "}
        {lot.allocations
          .map(
            (a) =>
              name(data.chambers, a.chamberId) + " " + qty(a.quantity) + " kg",
          )
          .join(" · ")}
      </p>
      <Table
        heads={[
          t("clientName"),
          t("numbers"),
          t("quantity"),
          t("unitPrice"),
          t("total"),
          t("paid"),
          "",
        ]}
        empty={empty}
        rows={lot.lines
          .filter(
            (l) =>
              (!payment || String(l.paid) === payment) &&
              matches(
                (l.clientName || "") +
                  " " +
                  l.pieces.map((p) => p.number).join(" "),
              ),
          )
          .map((l) => [
            l.clientName || "—",
            l.pieces.map((p) => p.number).join(", "),
            qty(l.quantity),
            money(l.unitPrice),
            money(l.quantity * l.unitPrice),
            <button
              disabled={busy}
              className={"badge " + (l.paid ? "green" : "amber")}
              onClick={() =>
                mutate("/payments", { saleId: lot.id, lineId: l.id })
              }
            >
              {t(l.paid ? "paid" : "unpaid")}
            </button>,
            actions(
              "line",
              l.id,
              undefined,
              <button className="quiet" onClick={() => receipt(lot.id, l.id)}>
                {t("receipt")}
              </button>,
              lot.id,
            ),
          ])}
      />
    </>
  );
}
