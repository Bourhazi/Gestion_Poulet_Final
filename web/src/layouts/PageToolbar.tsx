import { useWorkspace } from "../app/AppContext";
export function PageToolbar() {
  const {
    t,
    page,
    setError,
    search,
    setSearch,
    date,
    setDate,
    type,
    setType,
    payment,
    setPayment,
    from,
    setFrom,
    to,
    setTo,
    reload,
    lot,
  } = useWorkspace();
  return (
    <div className="toolbar">
      {!["dashboard", "souk", "reports"].includes(page) && (
        <input
          aria-label={t("search")}
          placeholder={t("search") + "…"}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      )}{" "}
      {!lot &&
        ["purchases", "feed", "sales"].includes(page) && (
          <input
            aria-label={t("date")}
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        )}{" "}
      {page === "sales" && !lot && (
        <select
          aria-label={t("type")}
          value={type}
          onChange={(e) => setType(e.target.value)}
        >
          <option value="">{t("all")}</option>
          {["grossiste", "detail", "lundi"].map((v) => (
            <option key={v} value={v}>
              {t(v)}
            </option>
          ))}
        </select>
      )}{" "}
      {lot && (
        <select
          aria-label={t("paid")}
          value={payment}
          onChange={(e) => setPayment(e.target.value)}
        >
          <option value="">{t("all")}</option>
          <option value="true">{t("paid")}</option>
          <option value="false">{t("unpaid")}</option>
        </select>
      )}{" "}
      {page === "reports" && (
        <>
          <label>
            {t("from")}
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              required
            />
          </label>
          <label>
            {t("to")}
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              required
            />
          </label>
        </>
      )}
      <button
        className="quiet"
        onClick={() => reload().catch((e) => setError(e.message))}
      >
        {t("refresh")} ↻
      </button>
    </div>
  );
}
