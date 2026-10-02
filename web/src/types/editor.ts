import type { Allocation } from "./models";
export type Field = {
  key: string;
  type?: string;
  options?: { value: string; label: string }[];
  optional?: boolean;
  min?: number;
  step?: string;
};
export type Editor = {
  title: string;
  endpoint: string;
  values: Record<string, string>;
  fields: Field[];
  kind: string;
  allocations?: Allocation[];
};
