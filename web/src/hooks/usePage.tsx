import { useWorkspace } from "../app/AppContext";
import { useRecordActions } from "../components/RecordActions";
import { SalesTable } from "../components/SalesTable";
import type { Sale } from "../types/models";
export function usePage() {
  const state = useWorkspace();
  const actions = useRecordActions();
  const salesTable = (list: Sale[]) => <SalesTable list={list} />;
  return { ...state, actions, salesTable };
}
