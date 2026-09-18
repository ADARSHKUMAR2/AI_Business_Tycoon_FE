# AI Business Tycoon: Game Design Document (GDD)

## The Core Fantasy
Start with one empty shop → physically restock shelves yourself → serve customers → earn cash → hire AI employees to automate tasks → expand your land → build massive commercial districts.

This game merges the addictive, physical "Arcade Idle" gameplay loop (e.g., My Mini Mart) with deep tycoon district-building mechanics.

### 1. The Core Concept (Arcade Idle Tycoon)
The player starts with a small plot of land (10x10 meters) containing a basic, open-plan **Kirana Store**.

Instead of clicking buttons in a UI to generate money, the player interacts with the world physically:
1. Walk to the supply truck/box.
2. Pick up stacks of inventory (e.g., Rice, Tomatoes).
3. Carry them to empty display shelves inside the store.
4. Customers walk in, grab the items, and queue at the checkout.
5. The player runs to the checkout to process them and collect cash phys5. The player runs to the checkout to process them and collect cash plet → Cafe → Clothing Store → Electronics → Shopping Mall → Business District.

### 2. Land Expansion as Progression
The world is built on a 10x10 meter grid.
Initially, you own 1 tile containing your Kirana Store. 

When you earn enough cash, you walk to the edge of your property and step on a "Buy Land" trigger. A new 10x10 tile unlocks, allowing you to build a new business (like a Cafe) or expand your existing stores footprint.

### 3. Deep Business Automation (AI Employees)
As the store gets busier, the player cannot do everything manually. You use your profits to hire AI employees.
- **Restockers:** Automatically walk to the supply box, grab items, and fill empty shelves.
- **Cashiers:** Stand at the checkout counter and automatically process customer queues.
- **Cleaners:** Roam the store cleaning up trash dropped by customers (increases store rating).

### 4. Unique Gameplay Mechanics per Business
Each new building introduces a slightly different Arcade Idle mechanic:
- **Kirana:** Standard pick-up and restock loop.
- **Pizza:** Multi-step processing (Carry dough to oven → wait for baking → carry pizza to counter).
- **Cafe:** Customer sits at a table → Waiter (Player) takes order → Carries coffee to table.

### 5. AI Customers & Queuing Systems
Customers use NavMesh pathfinding to navigate the store.
- They have a target (e.g., "Shelf_Rice").
- If the shelf is empty, an "Angry/Waiting" emoji appears over their head.
- When they get their item, they pathfind to the `CheckoutQueue`.
- The Queue operates on a strict FIFO line physically visible in the game world.

### 6. Meaningful Multiplayer / Leaderboards
While the core district building is single-player, players compete on global leaderboards for "Highest Net Worth" and "Most Customers Served."
