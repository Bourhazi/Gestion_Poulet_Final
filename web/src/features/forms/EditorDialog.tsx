import { useWorkspace } from "../../app/AppContext";
import { Dialog } from "../../components/ui/Dialog";
import { monday, qty, options } from "../../utils/format";
export function EditorDialog() {
  const { t, data, error, busy, editor, setEditor, submitEditor } =
    useWorkspace();
  if (!editor) return null;
  return (
    <Dialog
      title={editor.title}
      close={() => {
        if (!busy) setEditor(null);
      }}
    >
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      <form onSubmit={submitEditor}>
        <div className="form-grid">
          {editor.fields
            .filter(
              (f) =>
                !(
                  editor.kind === "sales" &&
                  ((editor.values.type === "lundi" &&
                    [
                      "chamberId",
                      "clientId",
                      "clientName",
                      "unitPrice",
                      "mode",
                    ].includes(f.key)) ||
                    (editor.values.type !== "lundi" &&
                      ["pieces", "crateCost"].includes(f.key)))
                ) &&
                !(
                  editor.kind === "users" &&
                  editor.values.role === "admin" &&
                  f.key === "clientId"
                ),
            )
            .map((f) => {
              const egg = editor.kind.startsWith("egg");
              const unit = editor.values.mode === "unite";
              const label =
                egg && f.key === "quantity"
                  ? t(unit ? "eggCount" : "trays")
                  : egg && f.key === "unitPrice"
                    ? t(unit ? "eggPrice" : "trayPrice")
                    : t(f.key);
              const value = editor.values[f.key] || "";
              let opts = f.options;
              if (editor.kind === "sales" && f.key === "clientId")
                opts = options(
                  data.clients.filter((c) => c.type === editor.values.type),
                );
              const change = (value: string) =>
                setEditor({
                  ...editor,
                  values: {
                    ...editor.values,
                    [f.key]: value,
                    ...(f.key === "type" && editor.kind === "sales"
                      ? {
                          clientId: "",
                          date:
                            value === "lundi" ? monday() : editor.values.date,
                        }
                      : {}),
                    ...(f.key === "mode" && egg
                      ? { quantity: "", unitPrice: "0" }
                      : {}),
                  },
                });
              return (
                <label key={f.key}>
                  {label}
                  {opts ? (
                    <select
                      required={!f.optional}
                      value={value}
                      onChange={(e) => change(e.target.value)}
                    >
                      <option value="">—</option>
                      {opts.map((o) => (
                        <option key={o.value} value={o.value}>
                          {o.label}
                        </option>
                      ))}
                    </select>
                  ) : f.type === "textarea" ? (
                    <textarea
                      required={!f.optional}
                      value={value}
                      onChange={(e) => change(e.target.value)}
                    />
                  ) : (
                    <input
                      type={f.type || "text"}
                      required={!f.optional}
                      value={value}
                      min={
                        egg &&
                        editor.kind === "egg-sale" &&
                        f.key === "quantity" &&
                        !unit
                          ? 0.001
                          : f.min
                      }
                      step={f.step}
                      pattern={f.key === "phone" ? "[0-9]{10}" : undefined}
                      minLength={f.type === "password" ? 8 : undefined}
                      onChange={(e) => change(e.target.value)}
                    />
                  )}
                </label>
              );
            })}
        </div>
        {editor.allocations && (
          <div className="allocation-editor">
            <h3>{t("distributions")}</h3>
            {editor.allocations.map((a, i) => (
              <div className="allocation-row" key={i}>
                <select
                  aria-label={t("type")}
                  value={a.clientId != null ? "external" : "chamber"}
                  onChange={(e) => {
                    const list = editor.allocations!.map((x, j) =>
                      j === i
                        ? {
                            quantity: x.quantity,
                            chamberId:
                              e.target.value === "chamber"
                                ? data.chambers[0]?.id
                                : null,
                            clientId:
                              e.target.value === "external"
                                ? (data.clients[0]?.id ?? 0)
                                : null,
                          }
                        : x,
                    );
                    setEditor({ ...editor, allocations: list });
                  }}
                >
                  <option value="chamber">{t("chamberId")}</option>
                  <option value="external">{t("external")}</option>
                </select>
                <select
                  aria-label={t("name")}
                  required
                  value={a.clientId || a.chamberId || ""}
                  onChange={(e) =>
                    setEditor({
                      ...editor,
                      allocations: editor.allocations!.map((x, j) =>
                        j === i
                          ? {
                              ...x,
                              ...(x.clientId != null
                                ? { clientId: Number(e.target.value) }
                                : { chamberId: Number(e.target.value) }),
                            }
                          : x,
                      ),
                    })
                  }
                >
                  <option value="">—</option>
                  {(a.clientId != null ? data.clients : data.chambers).map(
                    (c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ),
                  )}
                </select>
                <input
                  aria-label={t("quantity")}
                  type="number"
                  min="0.001"
                  step="any"
                  required
                  value={a.quantity}
                  onChange={(e) =>
                    setEditor({
                      ...editor,
                      allocations: editor.allocations!.map((x, j) =>
                        j === i
                          ? { ...x, quantity: Number(e.target.value) }
                          : x,
                      ),
                    })
                  }
                />
                <button
                  type="button"
                  className="quiet danger"
                  onClick={() =>
                    setEditor({
                      ...editor,
                      allocations: editor.allocations!.filter(
                        (_, j) => j !== i,
                      ),
                    })
                  }
                >
                  ×
                </button>
              </div>
            ))}
            <p>
              {t("total")}:{" "}
              {qty(editor.allocations.reduce((n, a) => n + a.quantity, 0))} /{" "}
              {editor.values.quantity || 0} kg
            </p>
            <button
              type="button"
              className="quiet"
              onClick={() =>
                setEditor({
                  ...editor,
                  allocations: [
                    ...editor.allocations!,
                    { chamberId: data.chambers[0]?.id, quantity: 0 },
                  ],
                })
              }
            >
              + {t("add")}
            </button>
          </div>
        )}
        <footer>
          <button
            type="button"
            className="quiet"
            disabled={busy}
            onClick={() => setEditor(null)}
          >
            {t("cancel")}
          </button>
          <button disabled={busy}>{busy ? t("loading") : t("save")}</button>
        </footer>
      </form>
    </Dialog>
  );
}
