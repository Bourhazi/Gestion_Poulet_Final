import { useState, useEffect } from "react";
import { translator, type Language } from "../locales/i18n";
export function usePreferences() {
  const [lang, setLang] = useState<Language>(
    (localStorage.getItem("lang") as Language) || "fr",
  );
  const t = translator(lang);
  const [dark, setDark] = useState(localStorage.getItem("theme") === "dark");
  useEffect(() => {
    localStorage.setItem("lang", lang);
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === "ar" ? "rtl" : "ltr";
  }, [lang]);
  useEffect(() => {
    localStorage.setItem("theme", dark ? "dark" : "light");
    document.documentElement.dataset.theme = dark ? "dark" : "light";
  }, [dark]);

  return { lang, setLang, t, dark, setDark };
}
