import type { ReactNode } from "react";
import { useWorkspace } from "../app/AppContext";
import { LanguageSelect } from "../components/LanguageSelect";
import { PageToolbar } from "./PageToolbar";
import { AssistantChat } from "../components/AssistantChat";
import { BrandLogo } from "../components/BrandLogo";
export function AppLayout({
  children,
  dialogs,
}: {
  children: ReactNode;
  dialogs: ReactNode;
}) {
  const {
    t,
    dark,
    setDark,
    user,
    page,
    error,
    message,
    busy,
    logout,
    admin,
    navigate,
    openForm,
    pages,
    lot,
  } = useWorkspace();

  const canAdd =
    admin &&
    !lot &&
    [
      "suppliers",
      "clients",
      "chambers",
      "purchases",
      "feed",
      "sales",
      "users",
    ].includes(page);
  return (
    <div className="app">
      <aside>
        <div className="sidebar-brand">
          <BrandLogo />
        </div>
        <p className="eyebrow">POULTRY BUSINESS MANAGEMENT</p>
        <nav>
          {pages.map((p, i) => (
            <button
              key={p}
              className={page === p ? "active" : ""}
              onClick={() => navigate(p)}
            >
              <span>{String(i + 1).padStart(2, "0")}</span>
              {t(p)}
            </button>
          ))}
        </nav>
        <div className="sidebar-footer">
          <strong>{user.username}</strong>
          <small>{t(user.role)}</small>
          <button className="quiet" disabled={busy} onClick={logout}>
            {t("logout")}
          </button>
        </div>
      </aside>
      <main>
        <header className="topbar">
          <div className="topbar-context">
            <strong>DajajPro</strong>
            <span>{t(page)}</span>
          </div>
          <div className="topbar-actions">
            <LanguageSelect />
            <button className="quiet" onClick={() => setDark(!dark)}>
              {dark ? "☀" : "◐"} {t("dark")}
            </button>
          </div>
        </header>
        <div className="workspace">
          <div className="page-title">
            <div>
              <p className="eyebrow">DAJAJPRO / {t(page)}</p>
              <h1>{t(page)}</h1>
            </div>
            {canAdd && (
              <button onClick={() => openForm(page)}>+ {t("add")}</button>
            )}
          </div>
          {error && (
            <div className="error" role="alert">
              {error}
            </div>
          )}
          {message && (
            <div className="success" role="status">
              {message}
            </div>
          )}
          <PageToolbar />
          {children}
        </div>
      </main>

      {dialogs}
      {user.role === "admin" && <AssistantChat />}
    </div>
  );
}
