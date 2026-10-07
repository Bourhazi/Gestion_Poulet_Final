import { CssBaseline, ThemeProvider } from "@mui/material";
import { useMemo } from "react";
import { AppProvider, useAppContext } from "./app/AppContext";
import { PageRouter } from "./app/PageRouter";
import { AppLayout } from "./layouts/AppLayout";
import { LoginPage } from "./pages/LoginPage";
import { EditorDialog } from "./features/forms/EditorDialog";
import { ConfirmDialog } from "./components/dialogs/ConfirmDialog";
import { DetailDialog } from "./components/dialogs/DetailDialog";
import { createAppTheme } from "./theme";
function Workspace() {
  const { loading, user, data, t, error, reload, setError } = useAppContext();
  if (loading)
    return (
      <div className="login">
        <p>{t("loading")}</p>
      </div>
    );
  if (!user) return <LoginPage />;
  if (!data)
    return (
      <main className="login">
        <p role="alert">{error || t("loading")}</p>
        <button onClick={() => reload().catch((e) => setError(e.message))}>
          {t("refresh")}
        </button>
      </main>
    );
  return (
    <AppLayout
      dialogs={
        <>
          <EditorDialog />
          <ConfirmDialog />
          <DetailDialog />
        </>
      }
    >
      <PageRouter />
    </AppLayout>
  );
}
function ThemedApp() {
  const { dark } = useAppContext();
  const theme = useMemo(() => createAppTheme(dark ? "dark" : "light"), [dark]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Workspace />
    </ThemeProvider>
  );
}

export default function App() {
  return (
    <AppProvider>
      <ThemedApp />
    </AppProvider>
  );
}
