import { AppProvider, useAppContext } from "./app/AppContext";
import { PageRouter } from "./app/PageRouter";
import { AppLayout } from "./layouts/AppLayout";
import { LoginPage } from "./pages/LoginPage";
import { EditorDialog } from "./features/forms/EditorDialog";
import { ConfirmDialog } from "./components/dialogs/ConfirmDialog";
import { DetailDialog } from "./components/dialogs/DetailDialog";
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
export default function App() {
  return (
    <AppProvider>
      <Workspace />
    </AppProvider>
  );
}
