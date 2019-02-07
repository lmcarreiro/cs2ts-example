
export interface ListExamplesResponse {
  someBool: boolean;
  nullableInteger: number|null;
  nestedContent: NestedClass;
  nestedArray: AnotherClass[];
}

export interface NestedClass {
  id: number;
  valuesByName: { [key: string]: number };
}

export interface AnotherClass {
  code: number;
  name: string;
  value: number;
}
