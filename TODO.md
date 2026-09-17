# AI Business Tycoon: Frontend & Backend Tasks (TODO)

## Phase 1: The Core Loop (Proof of Concept)
### Frontend (FE) - Game Client
- [ ] Initialize FE project and folder structure.
- [ ] Create basic grid-based map system.
- [ ] Implement camera controls (pan, zoom, rotate).
- [ ] Build basic UI for buying/placing the "Kirana Store".
- [ ] Implement UI to hire an employee.
- [ ] Build visual simulation: Customer spawn -> walk to store -> wait -> buy -> leave.
- [ ] Implement visual feedback for earning money.
- [ ] Create Land Expansion UI.

### Backend (BE) - Server
- [ ] Initialize backend project (FastAPI) or mock locally.
- [ ] Define initial schemas: `PlayerState`, `Business`.
- [ ] Create local state manager for saving/loading progress.

---

## Phase 2: Minimum Viable Product (MVP)
### Frontend (FE)
- [ ] Implement UI for selecting 3 business types (Kirana, Pizza, Cafe).
- [ ] Add unique visual assets for new businesses.
- [ ] Implement pathfinding (NavMesh) for NPCs.
- [ ] Update Employee UI to show basic stats.
- [ ] Add basic Customer UI tooltips.
- [ ] Create main HUD for income vs. daily expenses.
- [ ] Build "Net Worth" leaderboard screen UI.

### Backend (BE)
- [ ] Set up FastAPI server and connect to DB.
- [ ] Create API for user auth and session management.
- [ ] Build `PlayerState` sync API.
- [ ] Implement basic daily calculation logic (rent/salaries).
- [ ] Create basic Leaderboard API.

---

## Phase 3: The "Living Simulation" & AI Integration
### Frontend (FE)
- [ ] Implement Employee leveling UI.
- [ ] Build varied NPC character models.
- [ ] Add visual indicators for local events.
- [ ] Create "AI Business Advisor" chat UI.
- [ ] Connect chat UI to BE advisor endpoint.

### Backend (BE)
- [ ] Create API for Employee progression.
- [ ] Implement localized event generator logic.
- [ ] Integrate LLM API (OpenAI/Anthropic).
- [ ] Build `POST /advisor` endpoint (player state + prompt -> LLM -> advice).
- [ ] Develop deterministic logic for queues/stock.

---

## Phase 4: City Economy & World State
### Frontend (FE)
- [ ] Create UI for District Reputation.
- [ ] Add new business models (Cinema, Gym, Pharmacy).
- [ ] Implement "News Ticker" UI.
- [ ] Build weather visual effects.
- [ ] Implement dynamic UI updates based on `WorldState`.

### Backend (BE)
- [ ] Move core economic authority to server.
- [ ] Develop `WorldState` manager (global Day, active cities, market modifiers).
- [ ] Create dynamic event scheduling (e.g., Heavy Rain).
- [ ] Build WebSocket/Polling system for WorldState/News.

---

## Phase 5: Multiplayer & Daily Franchises (Live Ops)
### Frontend (FE)
- [ ] Build Daily Franchise Auction UI.
- [ ] Implement visual badges for Franchise-owned shops.
- [ ] Expand Leaderboard UI with tabs (Wealth, Reputation, City).
- [ ] Add UI to switch City Maps (Delhi, Mumbai).

### Backend (BE)
- [ ] Build live Auction System (concurrent bids, locking).
- [ ] Set up daily cron jobs for franchise generation.
- [ ] Integrate full multiplayer leaderboards.
- [ ] Implement logic for City Specializations.

---

## Phase 6: Scaling, Prestige, and Advanced Multiplayer
### Frontend (FE)
- [ ] Build Prestige reset UI and Title display.
- [ ] Implement UI for massive endgame buildings.
- [ ] Create Player Marketplace UI.
- [ ] Build Co-op District View.
- [ ] Implement City Ownership heatmap UI.
- [ ] Optimize FE rendering for high NPC count.

### Backend (BE)
- [ ] Implement Prestige calculation API.
- [ ] Build robust trading APIs for Marketplace.
- [ ] Implement Co-op session management.
- [ ] Build API for City Commercial Influence calculation.
- [ ] Optimize server performance for high concurrent loads.
