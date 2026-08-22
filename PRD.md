# Product Requirements Document (PRD)
## Project: Pathweaver: Pocket Realms
**Target Platform:** Android (Google Play) | **Target Window:** Q4 2026+ | **Target Audience:** Casual-to-Midcore Strategy, Commuter/Offline Gamers

---

## 1. Executive Summary & Product Vision

### 1.1 Product Vision
*Pathweaver: Pocket Realms* is a cozy, tactile route-building puzzle game engineered specifically for offline single-thumb play on modern mobile devices. It transforms classic path-laying mechanics (*Pipe Mania*, *Carcassonne*) into an emergent logistics and resource-flow game powered by game-theoretic risk loops, deterministic procedural map generation, and a horizontal expansion model that respects player autonomy and time.

### 1.2 Core Value Proposition
- **Frictionless Offline Experience:** Instant launch (<1.5s), zero required internet connection, and zero gameplay degradation when offline.
- **Cognitive Flow State:** Deep tactical depth through risk-reward path extension with zero forced timers or reflex stress.
- **Ethical Monetization:** No energy gates, no predatory gacha mechanics, and no unskippable or forced interstitials.

---

## 2. Market Analysis & Target Persona

### 2.1 Market Trends (2026 Landscape)
- **Monetization Fatigue:** Maturing mobile audiences increasingly churn away from hyper-aggressive paywalls and energy bars.
- **Snackable Session Demand:** Average session lengths center around 3 to 6 minutes (transit, short breaks) requiring instant suspend-and-resume support.
- **Premium Hybridization:** High willingness to pay for premium unlock models, optional cosmetic tiers, and modular expansion packs.

### 2.2 Target Player Personas
1. **The Daily Commuter ("Strategic Optimizer"):** Plays during transit without Wi-Fi; values battery conservation, responsive UX, and self-contained puzzle solving.
2. **The Cozy Minimalist ("Mindful Explorer"):** Seeks aesthetic audio-visual feedback, calm ambient art, low-stress difficulty curves, and progression without time gates.

---

## 3. Core Gameplay & Game Theory Mechanics

### 3.1 The Core Loop
1. **Draw & Inspect:** Player draws hexagonal conduit tiles with varying multi-directional paths (water, wind, crystal, trade).
2. **Evaluate & Place:** Player positions tiles on a dynamic grid to route resources from source springs to destination hubs.
3. **Trigger Flow:** When an active path is completed, resources flow through the line, triggering harvest multipliers and clearing target quota requirements.
4. **Expand & Evolve:** Harvested Star Essence unlocks adjacent nodes on the World Atlas constellation.

### 3.2 Game-Theoretic Decision Architecture

#### A. Risk vs. Certainty Payoff (The Path Length Dilemma)
Players choose between closing short routes early versus building extended multi-node circuits.
- **Short Route:** Yields a fixed base score \( S_{base} \) with certainty (\( P_{success} \approx 1.0 \)).
- **Long Route:** Scales with a geometric multiplier \( S = S_{base} \times (1.35)^{L-1} \), where \( L \) is tile length, but consumes open hex tiles and increases risk of grid congestion.

```
+-------------------+-----------------------------+-----------------------------+
| Strategy          | Immediate Payoff            | Grid Congestion Penalty     |
+-------------------+-----------------------------+-----------------------------+
| Early Closure     | Moderate & Guaranteed       | Minimal (Grid stays open)   |
| Extended Routing  | High Exponential Multiplier | Severe (High deadlock risk) |
+-------------------+-----------------------------+-----------------------------+
```

#### B. Asymmetric Sunk Cost Management
- **Pivot Tokens:** Players earn consumable pivot tokens through high-efficiency plays, allowing the rotation or retrieval of previously placed tiles. This eliminates deadlock frustration while rewarding forward planning.

---

## 4. Game Modes & Progression Architecture

### 4.1 Game Modes
- **Atlas Campaign:** 120+ handcrafted puzzle stages across diverse biomes, each introducing distinct environmental rules (e.g., frozen rivers, volcanic vents).
- **The Daily Expedition:** A deterministic 24-hour daily seed generated fully offline using device date algorithms, offering identical puzzle setups globally.
- **Endless Wayfare:** Procedurally generated hex grids with escalating flow constraints and adaptive objective generations.

### 4.2 Expansion-Friendly Progression Map
Progression occurs across a non-linear constellation map rather than vertical stat scaling:
- **Relic Slots:** Equipable passive perks (e.g., "+1 tile redraw per stage", "River tiles gain +20% score").
- **Modular Biome Packs:** New standalone regions dock cleanly onto the outer edges of the constellation without requiring balance reworks of earlier biomes.

---

## 5. Technical Architecture (Offline-First Android)

### 5.1 Engine & Storage
- **Engine:** Lightweight 2D Unity (Universal Render Pipeline) or Godot 4.x runtime, targeting an initial APK/AAB download size under 85 MB.
- **Persistence:** Local SQLite / Room Database with encrypted binary serialization for ongoing run states.
- **State Sync:** Asynchronous delta synchronization with Google Play Games Cloud Saves upon network availability.

```
[ Local Game Engine ] ──(Write Local State)──> [ SQLite Local DB ]
        │                                             │
 (Online Event)                               (Async Delta Sync)
        │                                             │
        ▼                                             ▼
[ Network Detection ] ──────────────────────> [ Google Play Cloud Save ]
```

### 5.2 Performance & Battery Optimization
- **Variable Frame Rate:** Renders at 120/90 Hz during active tile drag/drop and animations; throttles to 30 Hz during idle strategic thinking.
- **Cold Boot Time:** Under 1.5 seconds to the interactive main menu.

---

## 6. Ethical Monetization Strategy

### 6.1 Monetization Pillars
- **Zero Pay-to-Win:** Real money never purchases raw victory or puzzle skips.
- **No Forced Ads:** No unskippable video ads or mandatory interstitial banners.
- **Complete Transparency:** Every unlockable is accessible either via pure gameplay mastery or clean one-time purchases.

### 6.2 Revenue Channels
1. **Core Free Experience:** Includes the complete first 3 biomes (60+ levels) and unlimited Endless Wayfare access.
2. **Supporter Pass ($4.99 USD):** Unlocks 5 exclusive hand-painted tile themes, an alternative ambient soundtrack, and golden path particle trails.
3. **Regional Biome DLCs ($2.49 - $2.99 USD):** Content packs featuring 30+ thematic puzzles and 2 novel environmental mechanics.
4. **Rewarded Video Hints (Optional):** Players may voluntarily watch a short ad to receive a non-destructive layout hint (capped at 2 per day, never required).

---

## 7. Telemetry & Success Metrics

### 7.1 Key Performance Indicators (KPIs)
- **Day 1 / Day 7 / Day 30 Retention:** Target D1 > 42%, D7 > 20%, D30 > 10%.
- **Offline Session Ratio:** Target > 45% of total sessions completed offline.
- **Average Session Duration:** 4.5 minutes.
- **Expansion Conversion Rate:** Target > 8% of active D30 players purchasing regional DLC packs.
- **Store Rating:** Maintain a Google Play rating of 4.7+ stars.

---

## 8. Release Roadmap & Milestones

- **Phase 1 (Alpha):** Core hex engine, deterministic RNG generator, local save framework, and 20 test levels.
- **Phase 2 (Closed Beta):** Full UI/UX polish, haptic feedback integration, dynamic frame-rate governor, and cloud delta-sync testing.
- **Phase 3 (Global Launch):** 3 full biomes, Daily Expedition mode, Google Play Achievements, Supporter Pass integration.
- **Phase 4 (Expansion Cycle):** Quarterly biome DLC drops, community map seed sharing, and accessibility enhancements.
