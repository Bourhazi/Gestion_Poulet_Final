import { useEffect, useState, type ReactNode } from "react";
type Row = (string | number | ReactNode)[];
export function Table({
  heads,
  rows,
  empty,
  pageSize = 10,
}: {
  heads: string[];
  rows: Row[];
  empty: string;
  pageSize?: number;
}) {
  const [page, setPage] = useState(1);
  const pages = Math.max(1, Math.ceil(rows.length / pageSize));
  useEffect(() => setPage((current) => Math.min(current, pages)), [pages]);
  const visibleRows = rows.slice((page - 1) * pageSize, page * pageSize);
  return (
    <>
      <div className="table-wrap">
        <table>
        <thead>
          <tr>
            {heads.map((h, i) => (
              <th key={i}>{h}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length ? (
            visibleRows.map((row, i) => (
              <tr key={i}>
                {row.map((cell, j) => (
                  <td key={j}>{cell}</td>
                ))}
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={heads.length} className="empty">
                {empty}
              </td>
            </tr>
          )}
        </tbody>
        </table>
      </div>
      {rows.length > pageSize && (
        <nav className="pagination" aria-label="Pagination">
          <button className="quiet" disabled={page === 1} onClick={() => setPage(page - 1)} aria-label="Previous page">←</button>
          <span>{page} / {pages}</span>
          <button className="quiet" disabled={page === pages} onClick={() => setPage(page + 1)} aria-label="Next page">→</button>
        </nav>
      )}
    </>
  );
}
