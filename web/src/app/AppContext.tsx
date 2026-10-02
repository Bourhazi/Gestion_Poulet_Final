import { createContext, useContext, type ReactNode } from "react";
import { useAppController } from "../hooks/useAppController";
type AppState = ReturnType<typeof useAppController>;
const AppContext = createContext<AppState | null>(null);
export function AppProvider({ children }: { children: ReactNode }) {
  const state = useAppController();
  return <AppContext.Provider value={state}>{children}</AppContext.Provider>;
}
export function useAppContext() {
  const state = useContext(AppContext);
  if (!state) throw new Error("AppProvider is required");
  return state;
}
export function useWorkspace() {
  const state = useAppContext();
  if (!state.data || !state.user)
    throw new Error("An authenticated workspace is required");
  return { ...state, data: state.data, user: state.user };
}
