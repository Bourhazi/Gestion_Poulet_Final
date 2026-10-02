import { useEffect, useState } from "react";
import { api } from "../services/api";
import type { Report, Snapshot } from "../types/models";
type Dependencies = {
  page: string;
  admin: boolean;
  from: string;
  to: string;
  data: Snapshot | null;
  setError: (error: string) => void;
};
export function useReports({
  page,
  admin,
  from,
  to,
  data,
  setError,
}: Dependencies) {
  const [report, setReport] = useState<Report | null>(null);
  useEffect(() => {
    if (page !== "reports" || !admin) return;
    let alive = true;
    setReport(null);
    setError("");
    api<Report>(`/reports?from=${from}&to=${to}`)
      .then((r) => {
        if (alive) setReport(r);
      })
      .catch((e) => {
        if (alive) setError(e.message);
      });
    return () => {
      alive = false;
    };
  }, [page, from, to, data, admin]);

  return report;
}
