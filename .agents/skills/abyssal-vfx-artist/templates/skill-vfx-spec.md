# Skill & Elemental VFX Spec — `<ClassName> [<Q/W/E/R>] <SkillName>`

- **Class & Build Flavor**: `<ClassName> (<BuildName>)`
- **Skill Slot & Category**: `[Q/W/E/R]` — `<SlashWave / Slam / Cone / Missile / Meteor / Chain / Nova / Beam / Hazard / Summon / Barrier / Blink / Tether / Transform / Aura>`
- **Element & Colors**: `<Element>` (`Core: #______`, `Mid: #______`, `Rim: #______`, max HDR `<= 2.6`)
- **Multiplayer Sync Tier**: `<Shared Visual Event (op: hero_skill) / Host-Authoritative>`

## 4-Stage Choreography

| Stage | Timing (`0.00s` ref) | Authoritative Trigger Source | Visual Layers (`Core_Primary`, `Accent`, `Secondary`) | Audio / Hit-Stop Sync |
| --- | --- | --- | --- | --- |
| **1. Anticipation** | `0.00s – __s` | Cast begin (`windupDelay`) | | Cast chime / gather SFX |
| **2. Execution** | `__s – __s` | Active projectile / sweep | | Launch / swing whoosh |
| **3. Impact** | `__s – __s` | Hit / destination arrival | | Impact SFX + optional trauma (`<= 0.15`) |
| **4. Dissipation** | `__s – __s` | Tail fade / pool return | | Fade embers / decal fade |

## Quality Tier Scaling (`ScalableVfxResponder`)

- **Low (Integrated GPU)**: `<e.g., Core beam + sharp ground telegraph ONLY; maxParticles <= 14; 0 PointLights>`
- **Medium**: `<e.g., Core + crescent ribbon + 50% embers; maxParticles <= 32>`
- **High (GTX 1650 target)**: `<e.g., Full embers, scorch decal fade, optional short 0.3s PointLight>`
- **Telegraph Preservation**: If skill displays a warning area for enemies/allies, mark ground ring `isCombatTelegraph = true`.

## Zero-Desync Verification
- [ ] Prefab contains **zero** `Collider`, damage logic, or cooldown timers.
- [ ] Visual radius matches `GameDB` / `ActorUnit` authoritative AoE radius (`<radius>m`).

