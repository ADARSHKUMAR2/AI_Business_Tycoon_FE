# AI Business Tycoon: Development Phases

This document outlines the step-by-step roadmap for building the Arcade Idle Tycoon game.

---

## Phase 1: The Arcade Idle Core Loop (Proof of Concept)
**Goal:** Prove that the physical interaction loop (Restock -> Sell -> Checkout) is fun.

**Features:**
- **Single Small Plot:** 10x10 grid with an open-room Kirana Store prefab.
- **Player Controller:** Click-to-move or joystick movement for the main player character.
- **Inventory System:** Logic for shelves holding 0/10 items, and player holding 0/5 items.
- **Customer AI (NavMesh):** Basic NPCs that spawn, walk to shelves, grab items, and wait at checkout.
- **Checkout Queue:** Physical line where customers wait.
- **Economy:** Cash drops on the counter, player collects it.

---

## Phase 2: Automation & Expansion (MVP)
**Goal:** Transition the player from manual worker to manager.

**Features:**
- **Hire Employees:** UI to spend money to spawn AI Restockers and Cashiers.
- **Worker AI:** Employees use NavMesh to autonomously perform player tasks.
- **Land Expansion:** Walk over a physical "Buy Land" trigger to unlock adjacent 10x10 tiles.
- **Multiple Businesses:** Introduce Pizza Outlet (requires multi-step cooking logic) and Cafe.
- **Backend Sync:** Save player money, owned tiles, and employee counts to FastAPI backend.

---

## Phase 3: The "Living Simulation" 
**Goal:** Deepen the simulation and make the world feel alive.

**Features:**
- **Employee Leveling:** Pay money to upgrade employee Speed and Carry Capacity.
- **Customer Variability:** Different NPC models, walking speeds, and budgets.
- **Trash & Cleaning System:** Customers drop trash; player must clean it or hire Janitors.
- **Store Upgrades:** Pay to upgrade shelves from holding 10 items to 20 items.

---

## Phase 4: World State & Events
**Goal:** Introduce external factors that force the player to adapt.

**Features:**
- **Weather Systems:** Rain/Night cycles affect customer spawn rates.
- **Supply Deliveries:** Instead of infinite supply boxes, delivery trucks arrive periodically.
- **District Reputation:** Store cleanliness and queue wait times affect a 5-star rating, which dictates customer spawn rate.

---

## Phase 5: Multiplayer & Leaderboards
**Goal:** Introduce competitive layers.

**Features:**
- **Net Worth Leaderboards.**
- **Franchise Auctions:** Daily server events to bid on premium brands.
