import { useState } from "react";
import { api } from "../services/api";
import { usePreferences } from "./usePreferences";
import { useReports } from "./useReports";
import { useSession } from "./useSession";
import { today } from "../utils/format";
import { useEditor } from "../features/forms/useEditor";
import { useDetails } from "./useDetails";
export function useAppController() {
  const { lang, setLang, t, dark, setDark } = usePreferences();
  const [page, setPage] = useState("dashboard");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [search, setSearch] = useState("");
  const [date, setDate] = useState("");
  const [type, setType] = useState("");
  const [payment, setPayment] = useState("");
  const [confirmation, setConfirmation] = useState<{
    kind: string;
    id: number;
    parentId?: number;
  } | null>(null);
  const [lotId, setLotId] = useState<number | null>(null);
  const [from, setFrom] = useState(today().slice(0, 8) + "01");
  const [to, setTo] = useState(today());
  const { user, data, loading, reload, login, logout } = useSession({
    setPage,
    setError,
    setBusy,
  });
  const admin = user?.role === "admin";
  const empty = t("empty");
  const report = useReports({ page, admin, from, to, data, setError });
  async function mutate(endpoint: string, body: unknown, method = "POST") {
    setBusy(true);
    setError("");
    setMessage("");
    try {
      await api(endpoint, method, body);
      await reload();
      setEditor(null);
      setConfirmation(null);
      setMessage(t("success"));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  function navigate(next: string) {
    setPage(next);
    setLotId(null);
    setSearch("");
    setDate("");
    setType("");
    setPayment("");
    setMessage("");
    setError("");
  }

  const { editor, setEditor, openForm, submitEditor } = useEditor({
    data,
    lotId,
    t,
    setError,
    mutate,
  });
  const { detail, setDetail, receipt, purchaseDetail, chamberDetail } =
    useDetails({ data, t, setError });
  const matches = (s: string) => s.toLowerCase().includes(search.toLowerCase());
  const pages = admin
    ? [
        "dashboard",
        "suppliers",
        "clients",
        "chambers",
        "purchases",
        "feed",
        "sales",
        "souk",
        "reports",
        "users",
      ]
    : ["sales"];
  const lot = data?.sales.find((s) => s.id === lotId);

  return {
    lang,
    setLang,
    t,
    dark,
    setDark,
    user,
    data,
    loading,
    login,
    logout,
    page,
    setPage,
    error,
    setError,
    message,
    busy,
    setBusy,
    search,
    setSearch,
    date,
    setDate,
    type,
    setType,
    payment,
    setPayment,
    confirmation,
    setConfirmation,
    lotId,
    setLotId,
    from,
    setFrom,
    to,
    setTo,
    report,
    admin,
    empty,
    reload,
    mutate,
    navigate,
    editor,
    setEditor,
    openForm,
    submitEditor,
    detail,
    setDetail,
    receipt,
    purchaseDetail,
    chamberDetail,
    matches,
    pages,
    lot,
  };
}
