import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * The standard shadcn/ui class helper: clsx resolves conditionals, tailwind-merge then
 * de-duplicates conflicting Tailwind utilities so a later class reliably wins.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
