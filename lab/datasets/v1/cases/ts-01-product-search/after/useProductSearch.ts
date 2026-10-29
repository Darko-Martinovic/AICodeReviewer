import { useEffect, useState } from "react";

export interface Product {
  id: string;
  name: string;
  price: number;
}

export function useProductSearch(query: string, delayMs = 300) {
  const [results, setResults] = useState<Product[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const term = query.trim();
    if (term.length < 2) {
      setResults([]);
      return;
    }

    const controller = new AbortController();
    const timer = setTimeout(async () => {
      try {
        const response = await fetch(`/api/products?q=${term}`, {
          signal: controller.signal,
        });
        if (!response.ok) {
          throw new Error(`Search failed with ${response.status}`);
        }
        setResults((await response.json()) as Product[]);
        setError(null);
      } catch (e) {
        if (!controller.signal.aborted) {
          setError(e instanceof Error ? e.message : String(e));
        }
      }
    }, delayMs);
  }, [query, delayMs]);

  return { results, error };
}
