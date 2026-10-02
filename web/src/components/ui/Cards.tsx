export function Cards({ items }: { items: [string, string][] }) {
  return (
    <div className="cards">
      {items.map(([label, value]) => (
        <article key={label}>
          <small>{label}</small>
          <strong>{value}</strong>
        </article>
      ))}
    </div>
  );
}
