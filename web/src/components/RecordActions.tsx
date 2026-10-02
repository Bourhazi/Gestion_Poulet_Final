import type { ReactNode } from "react";
import { useAppContext } from "../app/AppContext";
export function useRecordActions() {
  const { admin, t, setError, setConfirmation } = useAppContext();
  function actions(
    kind: string,
    id: number,
    edit?: () => void,
    other?: ReactNode,
    parentId?: number,
  ) {
    return (
      <div className="actions">
        {other}
        {admin && (
          <>
            {edit && (
              <button className="quiet" onClick={edit}>
                {t("edit")}
              </button>
            )}
            <button
              className="danger quiet"
              onClick={() => {
                setError("");
                setConfirmation({ kind, id, parentId });
              }}
            >
              {t("delete")}
            </button>
          </>
        )}
      </div>
    );
  }

  return actions;
}
