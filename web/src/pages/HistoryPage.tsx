import { useEffect, useState } from "react";
import { api } from "../services/api";
import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";
import { name } from "../utils/format";
import type { AuditLog } from "../types/models";

const privateKeys = new Set(["password", "passwordHash"]);
function values(json?: string) {
  if (!json) return [] as [string, unknown][];
  try { return Object.entries(JSON.parse(json)).filter(([key]) => !privateKeys.has(key.toLowerCase())); }
  catch { return [["value", json]]; }
}
function display(value: unknown) {
  if (value === null || value === undefined || value === "") return "—";
  return typeof value === "object" ? JSON.stringify(value) : String(value);
}
function Details({ log }: { log: AuditLog }) {
  const before = values(log.oldValues), after = values(log.newValues);
  return <details><summary>{log.description}</summary>{log.entityId != null && <p>Entity ID: {log.entityId}</p>}{log.ipAddress && <p>IP: {log.ipAddress}</p>}{before.length > 0 && <p>Before: {before.map(([key, value]) => `${key}: ${display(value)}`).join(" · ")}</p>}{after.length > 0 && <p>After: {after.map(([key, value]) => `${key}: ${display(value)}`).join(" · ")}</p>}</details>;
}

export function HistoryPage() {
  const { t, data, empty } = usePage();
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [date, setDate] = useState("");
  const [action, setAction] = useState("");
  useEffect(() => { api<AuditLog[]>(`/audit?date=${date}&action=${action}`).then(setLogs); }, [date, action]);
  return <><div className="filters"><input type="date" value={date} onChange={e => setDate(e.target.value)} /><select value={action} onChange={e => setAction(e.target.value)}><option value="">{t("all")}</option><option>Created</option><option>Saved</option><option>Deleted</option><option>Transferred</option><option>Updated</option></select></div><Table heads={[t("date"), t("user"), t("action"), t("module"), t("type"), t("detail")]} empty={empty} rows={logs.map(log => [new Date(log.date).toLocaleString(), log.userId ? name(data.users.map(u => ({ id: u.id, name: u.username })), log.userId) : "—", log.action, log.module, log.entityType, <Details key={log.id} log={log} />])} /></>;
}
