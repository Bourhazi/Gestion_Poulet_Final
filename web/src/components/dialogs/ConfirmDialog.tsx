import { useWorkspace } from "../../app/AppContext";
import { Dialog } from "../../components/ui/Dialog";

export function ConfirmDialog() {
  const { t, error, busy, confirmation, setConfirmation, mutate } =
    useWorkspace();
  if (!confirmation) return null;
  return (
    <Dialog
      title={t("confirm")}
      close={() => {
        if (!busy) setConfirmation(null);
      }}
    >
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      <p>
        {t("delete")} #{confirmation.id}
      </p>
      <footer>
        <button
          disabled={busy}
          className="quiet"
          onClick={() => setConfirmation(null)}
        >
          {t("cancel")}
        </button>
        <button
          disabled={busy}
          className="danger"
          onClick={() =>
            mutate(
              `/records/${confirmation.kind}/${confirmation.id}${confirmation.parentId ? `?parentId=${confirmation.parentId}` : ""}`,
              undefined,
              "DELETE",
            )
          }
        >
          {t("delete")}
        </button>
      </footer>
    </Dialog>
  );
}
