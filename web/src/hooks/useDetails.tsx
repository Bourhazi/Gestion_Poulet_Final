import {
  ReceiptDetails,
  type ReceiptData,
} from "../features/details/ReceiptDetails";
import { PurchaseDetails } from "../features/details/PurchaseDetails";
import {
  ChamberDetails,
  type ChamberProfit,
} from "../features/details/ChamberDetails";
import { ClientDetails } from "../features/details/ClientDetails";
import { useState, type ReactNode } from "react";
import { api } from "../services/api";
import type { Chamber, Person, Purchase, Snapshot } from "../types/models";

type Dependencies = {
  data: Snapshot | null;
  t: (key: string) => string;
  setError: (value: string) => void;
};
export function useDetails({ data, t, setError }: Dependencies) {
  const [detail, setDetail] = useState<ReactNode | null>(null);
  async function receipt(saleId: number, lineId?: number) {
    setError("");
    try {
      const r = await api<ReceiptData>(
        `/sales/${saleId}/receipt${lineId ? `?lineId=${lineId}` : ""}`,
      );
      setDetail(<ReceiptDetails receipt={r} t={t} />);
    } catch (e) {
      setError((e as Error).message);
    }
  }
  function purchaseDetail(p: Purchase) {
    if (!data) return;
    setDetail(<PurchaseDetails purchase={p} data={data} t={t} />);
  }
  function clientDetail(client: Person) {
    if (!data) return;
    setDetail(<ClientDetails client={client} data={data} t={t} />);
  }
  async function chamberDetail(c: Chamber) {
    if (!data) return;
    setError("");
    try {
      const profit = await api<ChamberProfit>(`/chambers/${c.id}/profit`);
      setDetail(
        <ChamberDetails chamber={c} profit={profit} data={data} t={t} />,
      );
    } catch (e) {
      setError((e as Error).message);
    }
  }

  return {
    detail,
    setDetail,
    receipt,
    purchaseDetail,
    chamberDetail,
    clientDetail,
  };
}
