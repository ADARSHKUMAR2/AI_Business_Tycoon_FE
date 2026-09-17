# AI Business Tycoon: Development Phases

This document outlines the step-by-step roadmap for building the AI Business Tycoon game. Following the principle of "start small, prove the fun, then scale," the project is broken down into structured phases. 

---

## Phase 1: The Core Loop (Proof of Concept)
**Goal:** Prove that the basic mechanical loop (Build -> Hire -> Operate -> Earn -> Expand) is fun and addictive in a strictly single-player, offline environment.

**Features:**
- **Single Small Plot:** Start with a small piece of land and basic UI.
- **One Business Type:** Just the Small Kirana Store.
- **Basic Simulation:** Simple deterministic flow (Customer spawns -> Walks to shop -> Buys item -> Pays -> Leaves).
- **Manual Management:** Player manually buys inventory, sets a single price, and hires a basic employee.
- **Land Expansion:** Earn enough to buy the adjacent tile and expand the store footprint.

**Technical Focus:**
- Setup project architecture.
- Build the grid-based building/expansion system.
- Implement the basic deterministic simulation.

---

## Phase 2: Minimum Viable Product (MVP)
**Goal:** Create a fully playable version of the game that introduces choice, basic AI, and early progression.

**Features:**
- **Three Business Types:** Kirana, Pizza, and Cafe.
- **Basic AI Employees & Customers:** 
  - Employees have basic stats (Speed, Salary).
  - Customers have basic preferences.
- **District Progression:** Expanding land to build separate shops, adding roads/parking.
- **Local Economy:** Simple income vs. daily expenses.
- **Basic Leaderboard:** "Net Worth" leaderboard.

**Technical Focus:**
- Basic backend setup for saving player state.
- Implement pathfinding for NPCs.
- Finalize the core game loop.

---

## Phase 3: The "Living Simulation" & AI Integration
**Goal:** Introduce the AI-powered layer to make the simulation feel alive.

**Features:**
- **Employee Progression:** Gain experience, level up, become Managers.
- **Advanced NPC AI:** Customers with budgets, favorite shops, and personalities.
- **Random Local Events:** Kitchen fires, VIP visits.
- **AI Business Advisor (LLM Layer):** A chatbot that analyzes deterministic data and gives advice.

**Technical Focus:**
- Integrate LLM API selectively.
- Build event-triggering engine.
- Deepen simulation.

---

## Phase 4: City Economy & World State
**Goal:** Move to a city-wide economy that changes dynamically.

**Features:**
- **Dynamic Demand:** Weather systems and local events alter demand.
- **News Ticker:** UI showing world events.
- **More Businesses:** Cinema, Gym, Pharmacy.
- **District Reputation System:** Grades for Cleanliness, Safety, affecting footfall.

**Technical Focus:**
- Move economy management fully to backend.
- Develop WorldState system.

---

## Phase 5: Multiplayer & Daily Franchises (Live Ops)
**Goal:** Introduce daily retention engines and competitive multiplayer layers.

**Features:**
- **Franchise Auctions:** Daily events to bid on premium brands.
- **Global & City Leaderboards:** Rank by Wealth, Influence, Reputation.
- **Multiplayer Competition:** Districts affect each other.
- **City Specializations:** Multiple cities (Delhi, Mumbai) with economic buffs.

**Technical Focus:**
- Integrate leaderboards and multiplayer state.
- Build live auction backend architecture.

---

## Phase 6: Scaling, Prestige, and Advanced Multiplayer
**Goal:** Solve infinite progression and add deep multiplayer cooperation.

**Features:**
- **Prestige System:** Resetting district progress for permanent titles.
- **Advanced Businesses:** Hotel, Bank, Mall.
- **Marketplace & Trading:** Players trade inventory/land.
- **Cooperative Mode:** Manage a single mega-city together.
- **City Ownership (Endgame):** Compete for commercial influence percentages.

**Technical Focus:**
- Scale backend for trading.
- Optimize rendering for 10,000+ NPCs.
