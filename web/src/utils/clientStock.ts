import type { Purchase } from "../types/models";

export type ClientStockMovement = {
  date: string;
  quantity: number;
  chickenType: string;
};

export function clientStockMovements(
  purchases: Purchase[],
  clientId: number,
): ClientStockMovement[] {
  return purchases
    .flatMap((purchase) =>
      purchase.allocations
        .filter((allocation) => allocation.clientId === clientId)
        .map((allocation) => ({
          date: purchase.date,
          quantity: allocation.quantity,
          chickenType: purchase.chickenType,
        })),
    )
    .sort((a, b) => b.date.localeCompare(a.date));
}

export function clientAvailableQuantity(
  purchases: Purchase[],
  clientId: number,
): number {
  return clientStockMovements(purchases, clientId).reduce(
    (total, movement) => total + movement.quantity,
    0,
  );
}
