/** Up to two initials from the first and last word of the name, or the first letter of the email without a name. */
export function initialsOf(name: string, email: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const letters = words.length > 1 ? [words[0], words[words.length - 1]] : words;
  const initials = letters.map((word) => word[0]).join('');

  return (initials || email.trim()[0] || '?').toUpperCase();
}
