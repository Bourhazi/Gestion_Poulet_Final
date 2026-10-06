import { useAppContext } from "../app/AppContext";
import { LanguageSelect } from "../components/LanguageSelect";
import { BrandLogo } from "../components/BrandLogo";
export function LoginPage() {
  const { t, error, busy, login } = useAppContext();

  return (
    <main className="login">
      <div className="login-card">
        <BrandLogo className="login-logo" />
        <p className="eyebrow">POULTRY BUSINESS MANAGEMENT</p>
        <h1>{t("login")}</h1>
        <LanguageSelect />
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <form
          onSubmit={async (e) => {
            e.preventDefault();
            const fd = new FormData(e.currentTarget);
            await login(
              String(fd.get("username") || ""),
              String(fd.get("password") || ""),
            );
          }}
        >
          <label>
            {t("username")}
            <input name="username" autoComplete="username" required autoFocus />
          </label>
          <label>
            {t("password")}
            <input
              name="password"
              type="password"
              autoComplete="current-password"
              required
            />
          </label>
          <button disabled={busy}>{busy ? t("loading") : t("login")}</button>
        </form>
      </div>
    </main>
  );
}
