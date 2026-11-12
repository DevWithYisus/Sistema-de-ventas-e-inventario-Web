export function Logo({ className = "" }: { className?: string }) {
  return <img src="/logo.webp" alt="Minino's Pet Shop" className={`select-none ${className}`} draggable={false} />;
}
