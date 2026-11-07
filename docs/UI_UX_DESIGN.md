# UI / UX Design System: PaperDesk

The design system for Paper Trading Simulator. Governs all Angular work across all phases.

---

## 1. Principles

1. **Dark trading terminal aesthetic**: High contrast, functional, dense information architecture inspired by Bloomberg Terminal and Kite/Zerodha.
2. **Strict color semantic**:
   - Backgrounds: Dark slate neutrals (`--surface-0: #0a0e14`, `--surface-1: #121820`, `--surface-2: #1a222d`).
   - Borders and rules: Subdued grays (`--rule-control: #253040`).
   - Accent / Brand: Subtle cyan/amber (`--brand: #2979ff` or `--brand: #00e676`).
   - Price direction:
     - Gain / Up: Green (`--gain: #00e676` or `#26a69a`) with upward triangle `▲` and `+` prefix.
     - Loss / Down: Red (`--loss: #ef5350` or `#f44336`) with downward triangle `▼` and `-` prefix.
     - Green and red are reserved **exclusively** for financial direction / status / errors. Never use them decoratively.
3. **Tabular Numerals**: Every number, price, timestamp, and percentage uses `font-variant-numeric: tabular-nums lining-nums` to eliminate layout jitter during live price changes.
4. **Right-aligned numbers**: All quantitative columns in tables must align right, with headers matching alignment.
5. **No decorative distractions**:
   - No gradients.
   - No decorative card grids with arbitrary drop shadows.
   - No unnecessary fade-and-slide entrance animations.
   - Smooth, fast cell flash highlights (0.9s duration) on tick updates.
   - Honor `prefers-reduced-motion: reduce`.
6. **Accessibility**:
   - High WCAG AA contrast on dark surfaces.
   - Explicit `:focus-visible` ring on every interactive element.
   - Never announce every tick to screen readers (cells are non-live). Only connection and market status transitions announce via `aria-live="polite"`.

---

## 2. Color Tokens

```scss
:root {
  --surface-0: #0a0e14;
  --surface-1: #121820;
  --surface-2: #1a222d;
  --surface-hover: #1f2a38;

  --text-1: #e6edf3;
  --text-2: #8b949e;
  --text-placeholder: #484f58;

  --rule-base: #21262d;
  --rule-control: #30363d;
  --rule-control-hover: #484f58;

  --brand: #2f81f7;
  --brand-hover: #58a6ff;
  --on-brand: #ffffff;

  --gain: #238636;
  --gain-bright: #3fb950;
  --gain-flash: rgba(63, 185, 80, 0.25);
  --on-gain: #06150e;

  --loss: #da3633;
  --loss-bright: #f85149;
  --loss-flash: rgba(248, 81, 73, 0.25);

  --font-sans: 'IBM Plex Sans', 'Noto Sans', system-ui, sans-serif;
  --font-cond: 'IBM Plex Sans Condensed', 'IBM Plex Sans', system-ui, sans-serif;

  --radius-control: 6px;
  --radius-chip: 3px;
  --radius-data: 2px;

  --motion-fast: 120ms;
}
```

---

## 3. Typography & Numerical Formatting

- Base font: IBM Plex Sans / system-ui sans-serif.
- Currency formatting: Indian Numbering System (`en-IN`), e.g., `₹10,00,000.00`.
- Price formatting: Strict 2 decimal places, tick grid on 0.05.
- Percentage formatting: Explicit sign `+X.XX%` or `-X.XX%`, accompanied by directional indicator `▲` / `▼`.

---

## 4. Layout & Navigation

- **Top Bar**: Persistent compact header containing brand glyph, app name, ticker strip (Phase 2), market status dot/badge, connection state indicator, and user account menu.
- **Navigation**: Clean horizontal or rail tabs for rapid view switching (Markets, Portfolio, Orders, Leaderboard).
- **Markets View**: Dense, responsive table with sticky headers, quick search filter, sortable columns, and tabular numeric cells.

---

## 5. Live Price Updates & Flash Directives

- When an instrument's price increments, its cell background flashes briefly with gain color (`rgba(63, 185, 80, 0.25)`).
- When an instrument's price decrements, its cell background flashes briefly with loss color (`rgba(248, 81, 73, 0.25)`).
- Transitions last ~0.9s before decaying to the normal background.
- Reduced motion: Flash transition is disabled when `@media (prefers-reduced-motion: reduce)` is active.

---

## 9. Copy deck

### Phase 1 base copy

| Place | Copy |
|---|---|
| Top bar brand | Paper Trading |
| Mode chip | Practice account |
| Stat label | Cash balance |
| Stat note | Practice money. Nothing here is real. |
| Ledger title | Cash ledger |
| Sign out button | Sign out |

### Phase 2 additions

| Place | Copy |
|---|---|
| Nav entry and page title | Markets |
| Search placeholder | Search symbol or company |
| Columns | Symbol, Company, Last price, Change, Change % |
| Empty search | No instruments match your search. |
| Load error | Couldn't load prices. Try again. (with a Retry button) |
| Connection states | Live, Reconnecting…, Offline |
| Stale data | Prices delayed |
| Market status | Market open, Market closed, Demo market open |
| Closed-market hint | Opens Monday 09:15 IST (compute the actual next open) |
