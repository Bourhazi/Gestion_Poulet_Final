import { useWorkspace } from "../../app/AppContext";
import { Dialog } from "../../components/ui/Dialog";

export function DetailDialog() {
  const { t, detail, setDetail } = useWorkspace();
  if (!detail) return null;
  return (
    <Dialog title={t("detail")} close={() => setDetail(null)}>
      {detail}
    </Dialog>
  );
}
