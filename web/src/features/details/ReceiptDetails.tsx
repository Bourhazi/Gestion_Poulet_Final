import type { Person, Sale } from "../../types/models";
import { Table } from "../../components/ui/Table";
import { money, qty, revenue } from "../../utils/format";
export type ReceiptData = {
  sale: Sale;
  line?: Sale["lines"][number];
  client?: Person;
};
export function ReceiptDetails({
  receipt: r,
  t,
}: {
  receipt: ReceiptData;
  t: (key: string) => string;
}) {
  const empty = t("empty");
  const line = r.line;
  const s = r.sale;
  const quantity = line?.quantity ?? s.quantity;
  const price = line?.unitPrice ?? s.unitPrice;

  return (
    <div className="receipt">
      <p className="eyebrow">
        POULET · {t("receipt")} #{s.id}
        {line ? ` / ${line.id}` : ""}
      </p>
      <h2>{t("receipt")}</h2>
      <p>
        {s.date} · {t(s.chickenType)} · {t(line?.mode ?? s.mode)}
      </p>
      <h3>{line?.clientName ?? r.client?.name ?? s.clientName ?? "—"}</h3>
      {r.client?.phone && <p>{r.client.phone}</p>}
      <Table
        heads={[t("quantity"), t("unitPrice"), t("total")]}
        rows={[
          [
            qty(quantity),
            money(price),
            money(line ? quantity * price : revenue(s)),
          ],
        ]}
        empty={empty}
      />
      {line && (
        <>
          <p>
            {t("numbers")}: {line.pieces.map((p) => p.number).join(", ")}
          </p>
          <span className={"badge " + (line.paid ? "green" : "amber")}>
            {t(line.paid ? "paid" : "unpaid")}
          </span>
        </>
      )}
      <p>{line?.notes ?? s.notes}</p>
      <button className="no-print" onClick={() => window.print()}>
        {t("print")}
      </button>
    </div>
  );
}
