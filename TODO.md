# AI Business Tycoon: Frontend & Backend Tasks (TODO)

## Phase 1: The Arcade Idle Core Loop (Proof of Concept)

### Frontend (Unity)
- [x] Create Grid System (10x10 meter tiles).
- [x] Generate interior-style building prefabs (Floor, Walls, Counter, Shelves).
- [ ] Implement NavMesh for the game world.
- [ ] Create `PlayerController` for physical movement (Click-to-move / WASD).
- [ ] Create `ShelfComponent` logic (Max Capacity, Current Stock, Add/Remove visual items).
- [ ] Create `CheckoutCounter` logic (Queue management, Cash dropping).
- [ ] Build `CustomerAI` (Spawn -> Walk to Shelf -> Walk to Checkout -> Leave).
- [ ] Implement physical interaction (Player standing near shelf transfers items).
- [ ] Connect Earned Cash visually to the HUD.

### Backend (FastAPI)
- [x] Initialize backend project (FastAPI) and MongoDB.
- [x] Define schemas: `PlayerState`, `Business`, `LandTile`.
- [ ] Update `Business` schema to track Employee Upgrades (Speed/Capacity).

---

## Phase 2: Automation & Expansion (MVP)

### Frontend (Unity)
- [ ] Create `WorkerAI` (Restocker state machine, Cashier state machine).
- [ ] Build physical "Buy Land" trigger pads on the edges of owned property.
- [ ] Build "Hire Employee" physical trigger pads inside the store.
- [ ] Add Pizza Outlet (Dough -> Oven -> Counter mechanic).
- [ ] Integrate Backend Sync (Save land/money/employees to FastAPI).

### Backend (FastAPI)
- [ ] Build Employee Hiring APIs.
- [ ] Build Land Purchasing APIs (Validating adjacency).

---

## Phase 3: The "Living Simulation"
### Frontend (Unity)
- [ ] Implement Employee leveling UI (popups over employees).
- [ ] Add visual Trash prefabs and Cleaning logic.
- [ ] Expand NPC variety (models, animations).
- [ ] Implement Shelf upgrade logic.

### Backend (FastAPI)
- [ ] Create API for Employee stat progression.
- [ ] Create API for Business layout/shelf upgrades.
