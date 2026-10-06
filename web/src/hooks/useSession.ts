import { useEffect, useState } from "react";
import { api, refreshCsrf, refreshSession } from "../services/api";
import type { Snapshot, User } from "../types/models";
type Dependencies = {
  setPage: (page: string) => void;
  setError: (error: string) => void;
  setBusy: (busy: boolean) => void;
};
export function useSession({ setPage, setError, setBusy }: Dependencies) {
  const [user, setUser] = useState<User | null>(null);
  const [data, setData] = useState<Snapshot | null>(null);
  const [loading, setLoading] = useState(true);
  async function reload() {
    setData(await api<Snapshot>("/snapshot"));
  }
  useEffect(() => {
    let alive = true;
    const load = async () => {
      try {
        return await api<User>("/auth/me");
      } catch {
        if (!(await refreshSession())) throw new Error("No active session.");
        return api<User>("/auth/me");
      }
    };
    load()
      .then(async (u) => {
        const snapshot = await api<Snapshot>("/snapshot");
        if (alive) {
          setUser(u);
          setData(snapshot);
          setPage(u.role === "admin" ? "dashboard" : "sales");
        }
      })
      .catch(() => {})
      .finally(() => {
        if (alive) setLoading(false);
      });
    const expired = () => {
      setUser(null);
      setData(null);
    };
    window.addEventListener("session-expired", expired);
    return () => {
      alive = false;
      window.removeEventListener("session-expired", expired);
    };
  }, [setPage]);

  async function login(username: string, password: string) {
    setBusy(true);
    setError("");
    try {
      await refreshCsrf();
      const u = await api<User>("/auth/login", "POST", { username, password });
      setUser(u);
      await refreshCsrf();
      await reload();
      setPage(u.role === "admin" ? "dashboard" : "sales");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function logout() {
    try {
      await api("/auth/logout", "POST");
      setUser(null);
      setData(null);
      await refreshCsrf();
    } catch (e) {
      setError((e as Error).message);
    }
  }
  return { user, data, loading, reload, login, logout };
}
