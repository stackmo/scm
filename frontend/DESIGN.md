# SmartClinic Management (SCM) — UI/UX Design System Specification (`DESIGN.md`)

> **Target Audience:** AI Coding Agents (Antigravity, Cursor, Claude Code, Trae) & Frontend Engineers.  
> **Domain:** Hospital Information System (HIS) / Clinical Workstation (EMR/EHR/Reception/Cashier/Pharmacy).  
> **Tech Stack:** React 19 + TypeScript + Vite + Tailwind CSS v4 + Lucide React.

---

## 1. Design Philosophy & Workstation Ergonomics
- **Aesthetic Tone:** High-Density Clinical Workstation — Crisp, distraction-free, surgical precision.
- **Inspiration:** Linear ergonomics (crisp 1px borders, data-dense) + Cal.com (clean structured forms) + Apple Health (clinical clarity & contrast).
- **Core Principle:**
  - **Zero Fluff / Zero Lag:** No heavy animations, bouncing icons, or decorative gradients that slow down clinicians.
  - **Information Density:** High information density per viewport. Doctors and receptionists must see all key patient data, vitals, and order lines without excessive vertical scrolling.
  - **Keyboard-First Ergonomics:** Seamless tab navigation, quick search (F-keys like F8 for Service Catalog search), and explicit visual focus rings.
  - **Clinical Contrast (WCAG AA/AAA):** High contrast for readability under varied hospital lighting conditions.

---

## 2. Color Palette & Clinical Semantic Tokens (Tailwind v4)

### 2.1. Neutral Canvas & Structure
| Token | Tailwind Class (Light) | Tailwind Class (Dark) | Usage |
| :--- | :--- | :--- | :--- |
| **Canvas Background** | `bg-slate-50` (`#f8fafc`) | `bg-slate-950` (`#020617`) | Main application page background |
| **Surface / Card** | `bg-white` (`#ffffff`) | `bg-slate-900` (`#0f172a`) | Cards, panels, modal content, tables |
| **Subtle Surface** | `bg-slate-100` (`#f1f5f9`) | `bg-slate-800/60` | Table headers, secondary toolbars, disabled rows |
| **Borders** | `border-slate-200` (`#e2e8f0`) | `border-slate-800` (`#1e293b`) | All structural dividers and table grids |
| **Text Primary** | `text-slate-900` (`#0f172a`) | `text-slate-100` (`#f8fafc`) | Main headings, table content, active values |
| **Text Secondary** | `text-slate-500` (`#64748b`) | `text-slate-400` (`#94a3b8`) | Labels, timestamps, secondary metadata |
| **Text Muted** | `text-slate-400` (`#94a3b8`) | `text-slate-500` (`#64748b`) | Placeholders, inactive hints |

### 2.2. Clinical Primary & Accents
* **Primary Brand Accent (Clinical Teal):**
  * Light: `text-teal-700`, `bg-teal-600 hover:bg-teal-700`, `border-teal-600`
  * Focus Ring: `focus:ring-2 focus:ring-teal-500 focus:outline-none`
  * Light Background Tint: `bg-teal-50`, `border-teal-200`, `text-teal-800`
* **Secondary Brand Accent (Medical Slate/Blue):**
  * `bg-sky-50 text-sky-700 border-sky-200` (Used for non-critical information badges or diagnostic badges)

### 2.3. Clinical Semantic Indicators (Strict Invariants)
Do not swap or invent random colors for these clinical states:
* **Allergy / Critical Alert / Emergency:**
  * Badge / Box: `bg-red-50 text-red-700 border border-red-200 font-semibold`
  * Text highlight: `text-red-600`
* **Pending / In-Progress / Warning / Waitlist:**
  * Badge / Box: `bg-amber-50 text-amber-700 border border-amber-200`
  * Warning Indicator: `text-amber-600`
* **Completed / Normal / Paid / Dispensed:**
  * Badge / Box: `bg-emerald-50 text-emerald-700 border border-emerald-200`
  * Normal text: `text-emerald-600`
* **Insurance / BHYT / Commercial Guarantee (BHTM):**
  * Badge / Box: `bg-indigo-50 text-indigo-700 border border-indigo-200`
  * Badge text: `text-indigo-700 font-medium`

---

## 3. Typography & Data Presentation Rules
- **Font Family:** `font-sans` (`Inter`, system UI font fallback).
- **Tabular & Monospace Numbers:**
  - ALL numeric values (Vital signs, Heart rate, BP, SpO2, ICD-10 codes, BHYT card numbers, Prices, Quantities, Dosages) MUST use `font-mono` (`font-mono tabular-nums`).
- **Hierarchy Scale:**
  - Page Title: `text-lg font-bold text-slate-900 tracking-tight`
  - Section Header: `text-xs font-semibold uppercase tracking-wider text-slate-500`
  - Table Header: `text-[11px] font-semibold uppercase tracking-wider text-slate-500 bg-slate-100/75`
  - Table Cell Text: `text-xs text-slate-800 leading-normal`
  - Form Label: `text-xs font-medium text-slate-700 mb-1 block`
  - Hint / Error Text: `text-[11px] text-red-600 mt-0.5`

---

## 4. Spacing, Sizing & Grid Rhythm
- **Table Row Sizing:**
  - Compact Data Row: `h-8` to `h-9` (32px – 36px) with `px-3 py-1.5`.
  - Dense Medical Order Grid: `py-1 px-2.5` to fit 10+ items per screen.
- **Form Fields & Inputs:**
  - Input Height: Compact `h-8` (32px) or standard `h-9` (36px).
  - Input Padding: `px-2.5 py-1 text-xs`.
  - Border Radius: Uniform `rounded-md` (4px – 6px). **NEVER** use `rounded-full` for form fields.
  - Border: `border border-slate-300 dark:border-slate-700 focus:border-teal-500 focus:ring-1 focus:ring-teal-500`.
- **Card & Container Layout:**
  - Padding: `p-3` to `p-4` for workstation panels. Avoid large `p-8` padding.
  - Gap: `gap-2` to `gap-3` between form elements.
  - Multi-Pane Split: 3-column / 2-column layout (e.g., Left: Queue/Patient Info, Center: Clinical Examination/ICD, Right: Prescription/Service Orders).

---

## 5. Standard Component Patterns

### 5.1. Status & Clinical Badges
```tsx
// Status badge template
<span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium border bg-emerald-50 text-emerald-700 border-emerald-200">
  <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
  Đã hoàn thành
</span>
```

### 5.2. Keyboard Shortcut Indicator
```tsx
// Kbd hint badge
<kbd className="inline-flex items-center px-1.5 py-0.5 text-[10px] font-mono font-medium text-slate-500 bg-slate-100 border border-slate-300 rounded shadow-xs">
  F8
</kbd>
```

### 5.3. Dense Table Structure
```tsx
<div className="overflow-hidden border border-slate-200 rounded-md shadow-xs bg-white">
  <table className="w-full text-left border-collapse">
    <thead>
      <tr className="border-b border-slate-200 bg-slate-50/80 text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
        <th className="px-3 py-2">Mã DV</th>
        <th className="px-3 py-2">Tên dịch vụ</th>
        <th className="px-3 py-2 text-right">Đơn giá</th>
      </tr>
    </thead>
    <tbody className="divide-y divide-slate-100 text-xs">
      <tr className="hover:bg-slate-50/60 transition-colors">
        <td className="px-3 py-1.5 font-mono text-slate-600">DV-001</td>
        <td className="px-3 py-1.5 font-medium text-slate-900">Khám tổng quát</td>
        <td className="px-3 py-1.5 text-right font-mono tabular-nums">150,000 đ</td>
      </tr>
    </tbody>
  </table>
</div>
```

---

## 6. Strict Do's & Don'ts for AI Coding Agents
* **DO:**
  1. Always keep tables, inputs, and modals compact and tight.
  2. Always format numbers, prices, ICD codes, and timestamps using `font-mono tabular-nums`.
  3. Always provide clear visual feedback on `:focus-visible` using teal focus rings.
  4. Use semantic alerts for patient safety (allergies MUST jump out in red).
* **DON'T:**
  1. Do NOT use massive padding (e.g. `p-8`, `py-6`) or huge rounded pills (`rounded-3xl`) on workstation views.
  2. Do NOT use fancy animated loaders or bounce effects during critical clinic workflows.
  3. Do NOT invent new color semantics (e.g. using purple or pink for emergency states).
  4. Do NOT leave inputs without explicit border/ring states.
