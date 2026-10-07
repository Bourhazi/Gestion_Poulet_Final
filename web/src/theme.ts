import { createTheme, type PaletteMode } from "@mui/material/styles";

export function createAppTheme(mode: PaletteMode) {
  const dark = mode === "dark";

  return createTheme({
    palette: {
      mode,
      primary: { main: "#2563eb", dark: "#1d4ed8", light: "#dbeafe" },
      secondary: { main: "#15803d", dark: "#166534", light: "#dcfce7" },
      background: {
        default: dark ? "#0f172a" : "#f8fafc",
        paper: dark ? "#172033" : "#ffffff",
      },
      text: {
        primary: dark ? "#f1f5f9" : "#172033",
        secondary: dark ? "#b6c2d2" : "#64748b",
      },
      divider: dark ? "#2b3a52" : "#e2e8f0",
      success: { main: "#15803d" },
      warning: { main: "#b45309" },
      error: { main: "#b42318" },
    },
    shape: { borderRadius: 10 },
    typography: {
      fontFamily: '"DM Sans", Arial, sans-serif',
      h1: { fontFamily: 'Manrope, "DM Sans", Arial, sans-serif', fontWeight: 800 },
      h2: { fontFamily: 'Manrope, "DM Sans", Arial, sans-serif', fontWeight: 700 },
      h3: { fontFamily: 'Manrope, "DM Sans", Arial, sans-serif', fontWeight: 700 },
      button: { fontWeight: 700, textTransform: "none" },
    },
    components: {
      MuiButton: { defaultProps: { disableElevation: true } },
      MuiPaper: { styleOverrides: { root: { backgroundImage: "none" } } },
      MuiTextField: { defaultProps: { size: "small" } },
      MuiOutlinedInput: {
        styleOverrides: {
          root: { backgroundColor: dark ? "#172033" : "#ffffff" },
        },
      },
    },
  });
}
