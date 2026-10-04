import type { Feed, Purchase, Sale } from "../types/models";

export function soldChickenCost(sales: Sale[]): number {
  return sales.reduce(
    (total, sale) =>
      total +
      (sale.type === "lundi" && sale.quantity > 0
        ? (sale.costOfGoods * sale.lines.reduce((sum, line) => sum + line.quantity, 0)) /
          sale.quantity
        : sale.costOfGoods),
    0,
  );
}

export function feedCostForSales(
  purchases: Purchase[],
  feed: Feed[],
  sales: Sale[],
): number {
  const received = new Map<number, number>();
  for (const purchase of purchases)
    for (const allocation of purchase.allocations)
      if (allocation.chamberId != null) {
        const chamberId = allocation.chamberId;
        received.set(
          chamberId,
          (received.get(chamberId) || 0) + allocation.quantity,
        );
      }
  const costs = new Map<number, number>();
  for (const item of feed)
    costs.set(item.chamberId, (costs.get(item.chamberId) || 0) + item.cost);
  return sales.flatMap((sale) => sale.allocations).reduce((total, allocation) => {
    if (allocation.chamberId == null) return total;
    const chamberId = allocation.chamberId;
    const receivedQuantity = received.get(chamberId) || 0;
    return total +
      (receivedQuantity > 0
        ? (allocation.quantity * (costs.get(chamberId) || 0)) /
          receivedQuantity
        : 0);
  }, 0);
}
