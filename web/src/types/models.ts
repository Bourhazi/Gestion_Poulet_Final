export type Person = {
  id: number;
  name: string;
  phone?: string;
  type?: string;
};
export type User = {
  id: number;
  username: string;
  role: string;
  clientId?: number;
};
export type Chamber = {
  id: number;
  name: string;
  capacity: number;
  isSouk: boolean;
  normal: number;
  bibi: number;
  total: number;
};
export type Allocation = {
  id?: number;
  chamberId?: number | null;
  clientId?: number | null;
  quantity: number;
};
export type Purchase = {
  id: number;
  supplierId?: number;
  date: string;
  quantity: number;
  unitPrice: number;
  chickenType: string;
  departureWeight?: number;
  actualWeight?: number;
  notes?: string;
  allocations: Allocation[];
};
export type Feed = {
  id: number;
  chamberId: number;
  date: string;
  quantity: number;
  cost: number;
  notes?: string;
};
export type MondayLine = {
  id: number;
  clientName?: string;
  quantity: number;
  unitPrice: number;
  paid: boolean;
  mode: string;
  notes?: string;
  pieces: { id: number; number: string }[];
};
export type Sale = {
  id: number;
  date: string;
  type: string;
  clientId?: number;
  clientName?: string;
  chickenType: string;
  mode: string;
  quantity: number;
  unitPrice: number;
  pieces: number;
  crateCost: number;
  costOfGoods: number;
  notes?: string;
  allocations: Allocation[];
  lines: MondayLine[];
};
export type Snapshot = {
  suppliers: Person[];
  clients: Person[];
  chambers: Chamber[];
  purchases: Purchase[];
  feed: Feed[];
  sales: Sale[];
  users: User[];
};
export type Report = {
  chickenKg: number;
  revenue: number;
  costOfGoods: number;
  feedCost: number;
  crateCost: number;
  grossProfit: number;
  netProfit: number;
  paidMonday: number;
  unpaidMonday: number;
  byDay: { date: string; revenue: number }[];
  topClients: { name: string; quantity: number; revenue: number }[];
  suppliers: { name: string; count: number; quantity: number; cost: number }[];
};
