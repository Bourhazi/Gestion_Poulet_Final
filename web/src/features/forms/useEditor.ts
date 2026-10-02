import { useState, type FormEvent } from "react";
import type { Snapshot } from "../../types/models";
import type { Editor } from "../../types/editor";
import { createEditor, type EditableRecord } from "./createEditor";
import { serializeEditor } from "./serializeEditor";
type Dependencies = {
  data: Snapshot | null;
  lotId: number | null;
  t: (key: string) => string;
  setError: (value: string) => void;
  mutate: (endpoint: string, body: unknown, method?: string) => Promise<void>;
};
export function useEditor({ data, lotId, t, setError, mutate }: Dependencies) {
  const [editor, setEditor] = useState<Editor | null>(null);
  function openForm(kind: string, existing?: EditableRecord) {
    if (!data) return;
    setError("");
    setEditor(createEditor(kind, existing, { data, lotId, t }));
  }
  async function submitEditor(e: FormEvent) {
    e.preventDefault();
    if (!editor) return;
    await mutate("/" + editor.endpoint, serializeEditor(editor));
  }
  return { editor, setEditor, openForm, submitEditor };
}
