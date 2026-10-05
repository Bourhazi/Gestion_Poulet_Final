import { useWorkspace } from "./AppContext";
import { DashboardPage } from "../pages/DashboardPage";
import { SuppliersPage } from "../pages/SuppliersPage";
import { ClientsPage } from "../pages/ClientsPage";
import { ChambersPage } from "../pages/ChambersPage";
import { PurchasesPage } from "../pages/PurchasesPage";
import { FeedPage } from "../pages/FeedPage";
import { SalesPage } from "../pages/SalesPage";
import { SoukPage } from "../pages/SoukPage";
import { ReportsPage } from "../pages/ReportsPage";
import { UsersPage } from "../pages/UsersPage";
import { MondaySalePage } from "../pages/MondaySalePage";
import { HistoryPage } from "../pages/HistoryPage";
const pages = {
  dashboard: DashboardPage,
  suppliers: SuppliersPage,
  clients: ClientsPage,
  chambers: ChambersPage,
  purchases: PurchasesPage,
  feed: FeedPage,
  sales: SalesPage,
  souk: SoukPage,
  reports: ReportsPage,
  users: UsersPage,
  history: HistoryPage,
};
export function PageRouter() {
  const { page, lot, admin } = useWorkspace();
  if (!admin) return <SalesPage />;
  if (page === "sales" && lot) return <MondaySalePage />;
  const Page = pages[page as keyof typeof pages] || DashboardPage;
  return <Page />;
}
