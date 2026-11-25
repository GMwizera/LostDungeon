---
name: Abyssal Crypt
colors:
  surface: '#131313'
  surface-dim: '#131313'
  surface-bright: '#3a3939'
  surface-container-lowest: '#0e0e0e'
  surface-container-low: '#1c1b1b'
  surface-container: '#201f1f'
  surface-container-high: '#2a2a2a'
  surface-container-highest: '#353534'
  on-surface: '#e5e2e1'
  on-surface-variant: '#d8c3ad'
  inverse-surface: '#e5e2e1'
  inverse-on-surface: '#313030'
  outline: '#a08e7a'
  outline-variant: '#534434'
  surface-tint: '#ffb95f'
  primary: '#ffc174'
  on-primary: '#472a00'
  primary-container: '#f59e0b'
  on-primary-container: '#613b00'
  inverse-primary: '#855300'
  secondary: '#7bd0ff'
  on-secondary: '#00354a'
  secondary-container: '#00a6e0'
  on-secondary-container: '#00374d'
  tertiary: '#ffbcb7'
  on-tertiary: '#68000a'
  tertiary-container: '#ff938c'
  on-tertiary-container: '#8d0012'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffddb8'
  primary-fixed-dim: '#ffb95f'
  on-primary-fixed: '#2a1700'
  on-primary-fixed-variant: '#653e00'
  secondary-fixed: '#c4e7ff'
  secondary-fixed-dim: '#7bd0ff'
  on-secondary-fixed: '#001e2c'
  on-secondary-fixed-variant: '#004c69'
  tertiary-fixed: '#ffdad7'
  tertiary-fixed-dim: '#ffb3ad'
  on-tertiary-fixed: '#410004'
  on-tertiary-fixed-variant: '#930013'
  background: '#131313'
  on-background: '#e5e2e1'
  surface-variant: '#353534'
typography:
  headline-xl:
    fontFamily: Space Grotesk
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 52px
  headline-xl-mobile:
    fontFamily: Space Grotesk
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 36px
  headline-lg:
    fontFamily: Space Grotesk
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 38px
  headline-lg-mobile:
    fontFamily: Space Grotesk
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 28px
  headline-md:
    fontFamily: Space Grotesk
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 28px
  headline-sm:
    fontFamily: Space Grotesk
    fontSize: 18px
    fontWeight: '500'
    lineHeight: 24px
  body-lg:
    fontFamily: Geist
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Geist
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Geist
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-lg:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 18px
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  label-sm:
    fontFamily: JetBrains Mono
    fontSize: 10px
    fontWeight: '500'
    lineHeight: 14px
spacing:
  gutter: 1rem
  gutter-lg: 1.5rem
  margin: 1rem
  margin-md: 1.5rem
  margin-lg: 2.5rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2.5rem
---

## Brand & Style

This design system targets players seeking a hardcore, precision-driven rogue-dungeon crawler experience. The tone is severe, enigmatic, and hyper-focused. The aesthetic combines pure black minimalism with neon-arcane functional telemetry. Interfaces must evoke the cold dread of an ancient obsidian dungeon punctured only by concentrated magical energy. 

Avoid ornamental filigree, faux-parchment textures, distressed skeuomorphism, and muddy warm earth tones. Depth and spatial hierarchy are conveyed through stark contrast, razor-sharp geometric framing, and saturated luminescence against void backgrounds.

## Colors

The palette operates on absolute void levels and luminous chromatic signifiers:

- **Void Surfaces**: Pure black `#000000` serves as the canvas viewport and root screen level. Pure obsidian `#0a0a0a` defines modular frames, stat panels, and structural backdrops. Higher-level resting containers settle on `#121212` with borders resting at `#1f1f1f` to prevent warm or muddy undertones.
- **Loot & Relics (Primary)**: `#f59e0b` (Amber Gold). Dedicated to key pickups, chests, currency tokens, and primary action affordances.
- **Agility & Elements (Secondary)**: `#38bdf8` (Frost Cyan). Represents dash meters, mana stamina, weather effects, and chilling defensive runes.
- **Combat & Mortality (Tertiary)**: `#ef4444` (Crimson Ember). Strictly reserved for biological vital signs, active threats, lethal strikes, and critical health warnings.
- **Restoration & Mastery**: `#10b981` (Jade Emerald). Allocated to alchemy buffs, recovered vitality, puzzle completion, and sanctuary areas.
- **Text & Telemetry**: Crisp high-contrast zinc (`#f4f4f5`) for primary readability, muted steel (`#71717a`) for secondary stats, and dimmed ash (`#3f3f46`) for inactive slots.

## Typography

The typographic hierarchy relies on a technical-gothic interplay:

- **Headlines (Space Grotesk)**: Chosen for its chiseled, architectural glyphs and mechanical bite. Used for boss encounters, dungeon depth indicators, room names, and primary inventory sections.
- **Body (Geist)**: Pure neutral legibility with clean verticals and exceptional rendering on OLED pitch-black backdrops. Used for spell lore, quest chronicles, and narrative dialogues.
- **Labels & Values (JetBrains Mono)**: A monospaced typeface that enforces mathematical exactness. Applied across health percentages, rune cooldowns, loot roll statistics, inventory grid counts, and active keybindings. All numerical data columns should be set to tabular figures.

## Layout & Spacing

The layout structure is built on an uncompromising modular grid with dense, utilitarian spacing:

- **Grid Systems**: An 8-column layout for mobile viewports and a rigid 12-column layout for desktop/consoles. Desktop HUD configurations utilize corner-pinned view anchors with center-open stage clearing.
- **Layout Adaptations**:
  - **Mobile**: Margins lock to `1rem` (`margin`), with collapsible drawer sheets for inventory and full-screen modal overlays for skill progression trees.
  - **Tablet**: Margins expand to `1.5rem` (`margin-md`), organizing side-by-side gear slots and active dungeon maps.
  - **Desktop/Widescreen**: HUD anchors pin to outer canvas margins of `2.5rem` (`margin-lg`), leaving the game canvas free of central obstructions while inventory matrices dock into fixed-width side panels (360px–420px).
- **Rhythm**: All component gaps, grid tracks, and padding values align to an exact 4px/8px modular base scale (`space-xs` through `space-xl`).

## Elevation & Depth

Visual hierarchy uses edge definition, tonal step-ups, and localized chromatic luminescence rather than heavy drop shadows:

- **Base Layer (Elevation 0)**: Pitch black (`#000000`). Root canvas and gameplay viewport.
- **Tonal Planes (Elevation 1 & 2)**: Panel containers, equipment bays, and stat matrices settle on `#0a0a0a` (Elevation 1) and elevate to `#121212` (Elevation 2) when overlapping or active.
- **Low-Contrast Razor Edges**: Outlines use 1px solid structural dividers set to `#1f1f1f` or `#27272a`. Soft or blurred drop shadows are forbidden.
- **Neon Arcane Signifiers**: Elevation changes on hover or focus are signaled by switching edge outlines from `#1f1f1f` to sharp neon borders:
  - Gold items: `border-color: #f59e0b` paired with a sharp, low-spread glow: `box-shadow: 0 0 12px rgba(245, 158, 11, 0.25)`.
  - Agility/Stamina: `border-color: #38bdf8` with `box-shadow: 0 0 12px rgba(56, 189, 248, 0.25)`.
  - Danger/Combat: `border-color: #ef4444` with `box-shadow: 0 0 12px rgba(239, 68, 68, 0.3)`.
  - Completion/Potions: `border-color: #10b981` with `box-shadow: 0 0 12px rgba(16, 185, 129, 0.25)`.

## Shapes

The design system enforces a zero-radius rule (`roundedness: 0`). 

Every element features razor-sharp corners: buttons, inventory slots, tooltips, dialogue boxes, health bars, and progress indicators have `0px` border radii. Diagonal 45-degree corner notches (chamfers measuring 4px to 8px) are permitted on primary hero frames and top-tier item cards to reflect carved obsidian slabs and sharp-edged dungeon architecture.

## Components

- **Buttons & Triggers**:
  - **Primary (Relic/Gold)**: Razor 1px border (`#f59e0b`), `#0a0a0a` background, `#f59e0b` typography (`JetBrains Mono`, uppercase, bold). On hover, fill shifts to solid `#f59e0b` with `#000000` text and an amber neon outline.
  - **Secondary / Ghost**: 1px border (`#27272a`), `#000000` background, `#f4f4f5` text. On hover, border illuminates to `#38bdf8` with white text.
  - **Lethal / Combat**: 1px border (`#ef4444`), `#0a0a0a` background, text `#ef4444`. On active press, triggers an immediate `#ef4444` border pulse.
- **Inventory Grid & Loot Cards**:
  - Modular squares rendered in `#0a0a0a` with a 1px `#1a1a1a` border.
  - Rarity and item affinity are indicated by a 2px top edge pip colored in `#f59e0b` (Gold/Treasure), `#38bdf8` (Spell/Dash), `#ef4444` (Weapon/Ember), or `#10b981` (Tincture/Jade).
  - Selected state applies an active 1px full border in the respective accent color with zero corner rounding.
- **Telemetry, Meters & Gauges**:
  - **Vitality Track**: Stepped segmented blocks (width: 8px, gap: 2px) filled with solid `#ef4444` on a `#1f1f1f` track. Critical states pulse between `#ef4444` and `#000000`.
  - **Stamina/Dash Track**: Horizontal bar filled with `#38bdf8`, with instantaneous linear deplete and recharge transitions.
- **Chips & Status Tags**:
  - Monospace caps, zero radius, 1px perimeter border. Low-opacity colored backgrounds (e.g., `rgba(16, 185, 129, 0.1)` for jade status tags with solid `#10b981` text).
- **Form Inputs & Keystroke Prompts**:
  - Pitch-black backgrounds (`#000000`), inset borders in `#27272a`, focus states transitioning cleanly to `#38bdf8`. 
  - Keybind prompts feature small square brackets `[E]` or `[SPACE]` formatted in `JetBrains Mono` with muted steel frames.
- **Dialogue & Chronicle Logs**:
  - Anchored panels in `#0a0a0a` with a 1px border. Speaker identities styled in `Space Grotesk` uppercase alongside neon accent markers matching their dungeon faction.