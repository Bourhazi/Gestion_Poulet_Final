import type { Sale } from "../types/models";
import { useWorkspace } from "../app/AppContext";
import { useRecordActions } from "./RecordActions";
import { Table } from "../components/ui/Table";

import { money, qty, revenue, name } from "../utils/format";

export function SalesTable({ list }: { list: Sale[] }) {
  const {
    t,
    data,
    setPage,
    setSearch,
    setDate,
    setType,
    setLotId,
    admin,
    empty,
    receipt,
  } = useWorkspace();
  const actions = useRecordActions();
  return (
    <Table
      heads={[
        t("date"),
        t("clientId"),
        t("type"),
        t("chickenType"),
        t("quantity"),
        t("revenue"),
        "",
      ]}
      empty={empty}
      rows={list.map((s) => [
        s.date,
        name(data!.clients, s.clientId) === "—"
          ? s.clientName || "—"
          : name(data!.clients, s.clientId),
        t(s.type),
        t(s.chickenType) + " · " + t(s.mode),
        qty(s.quantity),
        money(revenue(s)),
        actions(
          "sale",
          s.id,
          undefined,
          <>
            {s.type === "lundi" && admin ? (
              <button
                className="quiet"
                onClick={() => {
                  setLotId(s.id);
                  setPage("sales");
                  setSearch("");
                  setDate("");
                  setType("");
                }}
              >
                {t("detail")}
              </button>
            ) : (
              <button className="quiet" onClick={() => receipt(s.id)}>
                {t("receipt")}
              </button>
            )}
          </>,
        ),
      ])}
    />
  );
}
