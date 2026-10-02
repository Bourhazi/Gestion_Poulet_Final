import { useAppContext } from "../app/AppContext";
import type { Language } from "../locales/i18n";
export function LanguageSelect() {
  const { lang, setLang } = useAppContext();
  return (
    <select
      aria-label="Language"
      value={lang}
      onChange={(e) => setLang(e.target.value as Language)}
    >
      <option value="fr">Français</option>
      <option value="en">English</option>
      <option value="ar">العربية</option>
    </select>
  );
}
