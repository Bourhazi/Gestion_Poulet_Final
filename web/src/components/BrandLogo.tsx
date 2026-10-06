import dajajProLogo from "../assets/dajajpro-logo.png";

export function BrandLogo({ className = "" }: { className?: string }) {
  return (
    <img
      className={`brand-logo ${className}`.trim()}
      src={dajajProLogo}
      alt="DajajPro"
    />
  );
}
