import { usePage } from "../hooks/usePage";
import { Table } from "../components/ui/Table";

import { name } from "../utils/format";

export function UsersPage() {
  const { t, data, empty, openForm, matches, actions } = usePage();

  return (
    <Table
      heads={[t("username"), t("role"), t("clientId"), ""]}
      empty={empty}
      rows={data.users
        .filter((u) => matches(u.username))
        .map((u) => [
          u.username,
          t(u.role),
          name(data.clients, u.clientId),
          actions(
            "user",
            u.id,
            undefined,
            <button className="quiet" onClick={() => openForm("passwords", u)}>
              {t("reset")}
            </button>,
          ),
        ])}
    />
  );
}
