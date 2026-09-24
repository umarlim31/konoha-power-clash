# Game Concept Bible v2 — implementation boundary

Source: `Negara_Konoha_Power_Clash_Game_Concept_Bible_v2.pdf`, 15 pages, provided by owner on 2026-09-24. Status in source: **Working Concept**, not a frozen tuning specification. This note corrects the earlier assumption that institutions should be grafted onto the 4v4 capture loop.

## Two modes, one shared combat foundation

| Mode | Bible v2 | Current repository | Implementation path |
| --- | --- | --- | --- |
| Jalur Takhta | Solo-first PvE campaign; co-op 1–4 later | Missing as a mode | New campaign layer with route, factions, access, final defense |
| Rebut Kursi | Competitive 4v4 chair capture and Power | Existing 0.0.6A.2 networked prototype | Preserve its match manager, bot loop and chair rules during campaign development |

The concept's example **3 of 5 Segel** is not a fixed requirement. A route should support alternative qualifying sectors and not require clearing every faction. The MVP intentionally uses a smaller set: Gerbang Rakyat, Majelis Daun, Biro Prosedur, and Istana Takhta. Komisi Suara Konoha, Konsorsium Modal and Menara Narasi expand the run later. **KPU** in earlier conversations is a real institution name; the Bible's fictional faction is **Komisi Suara Konoha**.

## First campaign vertical slice

1. Gerbang Rakyat: select a hero, spawn alone, learn movement and combat against a small encounter.
2. Plaza Aspirasi: show the distant seat and physically blocked Gerbang Dalam. Make the next two sectors readable from one hub.
3. Majelis Daun: commander and minions with a visible support/block effect; completing its objective earns one access seal.
4. Biro Prosedur: a different pressure mechanic (delay/gate), with an alternative objective path; earn the second MVP access seal.
5. Gerbang Dalam and Garda Takhta: server/local-run state validates prerequisites, then a final elite encounter opens the seat.
6. Fase Memerintah: sitting starts Power accumulation and a counterattack. Runtuh stops progress; completing the win condition ends the run.

The initial two-seal MVP is a **prototype decision**, not a numeric rule stated in the Bible. The exact number, encounter timers, AI tuning, resource rewards, and victory threshold remain tunable after a playable test. Leave room in the progression model for optional sectors and a 3-of-5 route later.

## Architecture before adding gameplay

- `CampaignRunState`: the first isolated route-state component is now implemented with distinct sector seals, a tunable unlock threshold, guard and seat gates, and paused Power after losing the seat. It does **not** yet own selected hero, resources, encounters, or a scene. Do not reuse PvP `NetworkMatchManager` state and `PowerToWin` as campaign truth.
- `CampaignObjectiveDirector`: owns gate checks and objective transitions. It must not make visually locked doors client-only; solo local authority first, with a future host authority interface for co-op.
- Faction/encounter data: composition and ability behaviors per organization. Reuse combat, Wibawa, Pengaruh, Runtuh and bot movement where appropriate, but give PvE bots separate targets and hostility rules.
- A separate campaign scene or additive sector set: visible central palace, hub-and-spoke routes and readable access status. The existing PvP arena should continue to generate identically.
- Manual mobile build: the pre-export entry point should prepare the chosen mode without an owner-operated Unity Editor. Deliver build steps and expected APK identity with every campaign branch.

## Acceptance gate for the first playable campaign build

- Solo run starts from the mobile UI without hosting a PvP room or matchmaking.
- Player cannot enter the final seat at spawn; unlock responds to actual objective state, not proximity alone.
- Majelis and Biro have different gameplay pressures; solo enemies are scaled for one player.
- Player can complete the route, beat a final guard, sit, survive counterattack and reach a result state.
- Existing 4v4 remains playable with identical capture, scoring and overtime behavior.
- Review on a real Android phone and tablet: sector signage, touch controls, performance, camera and readable objectives.

The four submitted hero sheets guide color and silhouettes, but are 2D references rather than production-ready models. Final assets should have original faces, costume shapes and faction symbols suitable for monetized release.

The three new EditMode cases check distinct seals, an optional 3-of-5 path, and Power stopping after the ruler is displaced. These tests still require the owner's manual Unity Build Automation run; the static source check cannot execute C# tests.
