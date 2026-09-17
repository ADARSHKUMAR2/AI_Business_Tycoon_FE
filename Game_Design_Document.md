# AI Business Tycoon: Game Design Document (GDD)

## The Core Fantasy
Start with one tiny shop → build businesses → hire employees → expand your land → create a commercial empire → compete with other players.

This concept provides much more room for progression than a single-store simulator, essentially describing a "build and own your own commercial district" tycoon simulator.

### 1. The Core Concept
Imagine the player starts with a tiny plot of empty land (e.g., ₹10,000).
They start with something small like a **Small Kirana Store**.

**Progression:**
Kirana → Grocery Store → Pizza Outlet → Cafe → Pharmacy → Clothing Store → Electronics → Restaurant → Cinema → Shopping Mall → Business District → City

*The player isn't simply upgrading a shop. They're building an entire area.*

### 2. Land as a Major Progression System
Initially:
```text
┌──────────┐
│  SHOP    │
└──────────┘
```

Then buy land:
```text
┌──────────┬──────────┐
│  SHOP    │ NEW LAND │
└──────────┴──────────┘
```

Expand into a district:
```text
┌──────────┬──────────┬──────────┐
│ GROCERY  │  CAFE    │ PIZZA    │
├──────────┼──────────┼──────────┤
│ PARKING  │   ROAD   │  EMPTY   │
└──────────┴──────────┴──────────┘
```

Eventually:
```text
┌──────┬──────┬──────┬──────┬──────┐
│MALL  │CINEMA│HOTEL │CAFE  │PIZZA │
├──────┼──────┼──────┼──────┼──────┤
│BANK  │PARK  │ROAD  │GROCERY│GYM  │
├──────┼──────┼──────┼──────┼──────┤
│OFFICE│PARKING│     │       │      │
└──────┴──────┴──────┴───────┴──────┘
```
*The player can literally see their empire growing. Extremely satisfying.*

### 3. Deep Business Simulation (No Magic Money)
Each business should have an actual simulation rather than being a basic idle tycoon.

*Example: Pizza Shop*
Customers → Queue → Order → Kitchen → Pizza Prepared → Customer Receives Pizza → Payment → Customer Leaves.

**Management aspects:** Employees, Ingredients, Inventory, Pricing, Customer satisfaction, Queue length, Opening hours, Electricity, Rent, Maintenance, Waste.

*Business Decision Example:*
- **1 Employee:** Customers (30/hr) > Capacity (15/hr) = Long queues, angry customers, lost sales.
- **2 Employees:** Higher salary cost, higher throughput, more revenue.

### 4. AI Employees with Characteristics
Employees are not just cosmetic NPCs; they have stats.
- **Ravi (Cashier):** Speed 72, Accuracy 91, Customer Care 64, Salary ₹1,200/day, Exp 2
- **Aman (Cashier):** Speed 94, Accuracy 61, Customer Care 73, Salary ₹2,000/day, Exp 5

*Decision:* Do you hire the expensive employee or three cheap employees?

### 5. Employee Leveling & Management Automation
Employee → Level 1 → Level 2 → Level 3 → Senior Employee → Manager → Store Manager.

A manager can automatically handle: employee scheduling, stock ordering, pricing, staff replacement, opening/closing.
*Progression:* Gradually going from doing everything manually to managing managers.

### 6. Unique Gameplay Mechanics per Business
Don't make every building "Buy → wait → collect money". Each new building should feel like a new mechanic.
- **🛒 Kirana:** Inventory + pricing + suppliers.
- **🍕 Pizza:** Recipes + ingredients + cooking + delivery.
- **🎬 Cinema:** Movies + ticket pricing + show timings + seating.
- **🏨 Hotel:** Rooms + cleanliness + booking + staff.
- **🏋️ Gym:** Memberships + equipment + trainers.
- **💊 Pharmacy:** Inventory + demand + expiry.
- **👕 Clothing store:** Fashion trends + inventory + discounts.
- **☕ Cafe:** Menu + seating + customer preferences.
- **🏦 Bank:** Deposits + loans + security + reputation.

### 7. City Economy
Shops don't operate in isolation. The city features Population, Demand, Traffic, Income level, Weather, Events, Competitors, and Tourism.

*Examples:*
- **Cricket Final:** Cinema demand ↑, Pizza demand ↑↑, Cafe demand ↑, Clothing demand ↑.
- **Heavy Rain:** Outdoor businesses ↓, Delivery ↑, Cinema ↑, Cafe ↑.
- **Festival:** Grocery ↑↑, Clothing ↑↑, Restaurant ↑, Cinema ↑.

### 8. NPC Customers with Personalities (AI Driven)
Instead of 500 identical NPCs, use Customer AI to simulate: Age, Budget, Preferences, Patience, Favorite shops, and Shopping habits.
- **Rahul:** Budget ₹1,500, Low Patience, Likes Pizza, High Price Sensitivity.
- **Priya:** Budget ₹8,000, High Patience, Likes Premium Brands, Low Price Sensitivity.

### 9. District Reputation System
Give the entire district a reputation score.
*Example:* ⭐ 4.7 / 5 (Safety 94, Cleanliness 81, Prices 76, Service 91, Entertainment 88)
Higher reputation = More visitors = More customers = More revenue. Poor management damages it.

### 10. Competitors (Multiplayer)
Players compete for the same population with competing commercial districts.
If Player A opens a Pizza Restaurant, Player B notices demand and opens one too. They compete on Price, Quality, Location, Marketing, Employee quality, Customer satisfaction.

### 11. Multiplayer Modes
- **Mode 1 (Cooperative City):** 2–4 players own different businesses in the same city.
- **Mode 2 (Competitive Districts):** Each player owns a different district and competes for the customer population.
- **Mode 3 (Business Partnership):** Invest in another player's business for a profit split (e.g., 20% / 80%).
- **Mode 4 (Marketplace):** Players trade inventory, raw materials, businesses, land, vehicles.

### 12. Global Leaderboards
Include multiple leaderboards to keep it fair for newer players:
- **Wealth:** Total money.
- **District Value:** Total property + business value.
- **Revenue:** Daily revenue.
- **Customer Satisfaction:** Average rating.
- **Business Empire:** Number of successful businesses.
- **Prestige:** Combined progression metric.

### 13. Prestige System
Instead of infinite numbers (Level 10,000), use titles:
Kirana Owner → Shop Owner → Business Owner → Entrepreneur → Tycoon → Industrialist → Business Mogul → City Magnate → Empire Builder.
Allows for adding content without arbitrary numbers.

### 14. Random Events
Crucial for long-term gameplay:
🔥 Kitchen fire, 📈 Supplier shortage, 🎉 Festival, 🏗️ Road construction, 🚨 Employee strike, ⭐ Celebrity visit, 💰 Investor offer, 🏪 Competitor opens nearby.

### 15. AI-Powered Living Business Simulation
Position the game as an "AI-powered living business simulation".
AI can control customers, employees, competitors, suppliers, market trends, and city events. Employees learn, customers develop preferences, competitors react.

### 16. Selective AI/LLM Usage
Do not use LLM for *every* transaction (too expensive and unnecessary).
- **Deterministic Simulation:** Customer movement, purchases, inventory, revenue, salaries, traffic.
- **AI/LLM Layer:** Dynamic events, business advice, competitor strategy, NPC conversations, special scenarios.

### 17. AI Business Advisor
Players can ask an AI assistant for insights.
*Player:* "Why did my grocery store revenue fall?"
*AI Analyzes Data:* Competitor nearby, milk prices 12% higher, stockouts up 8%.
*AI Recommends:* Lower prices by 5%, hire a cashier, and increase inventory.

### 18. Minimum Viable Product (MVP) Strategy
Start small before building out 50 businesses and global economies.
- **One Small City**
- **3 Businesses:** Kirana, Pizza, Cafe.
- **Employees:** Cashier, Cook, Manager.
- **Customers:** Basic AI.
- **Progression:** Land expansion.
- **Economy:** Money + expenses.
- **Leaderboard:** Net worth.
- **Multiplayer:** 2 players.
Prove the core loop is fun, then expand.

---

## Daily City Events & Franchise Auction System

Transform the tycoon game into a living economy where different cities have different economic characteristics and daily events drive player engagement.

### 1. Distinct City Economies
Create a map of India with specialized demands:
- **Delhi:** Retail, food, entertainment
- **Mumbai:** Luxury, finance, entertainment
- **Bengaluru:** Technology, cafes, gaming
- **Hyderabad:** Restaurants, technology
- **Chennai:** Manufacturing, retail
- **Kolkata:** Food, retail, culture
- **Jaipur:** Tourism, hotels, restaurants
- **Goa:** Tourism, hotels, nightlife

### 2. Daily Franchise Events
Rotate big daily events to keep players logging in:
- **Mon:** New Grocery Chain (3 slots)
- **Tue:** Pizza Brand Expansion (5 slots)
- **Wed:** Cinema Chain (2 slots)
- **Thu:** International Coffee Brand (4 slots)
- **Fri:** Luxury Hotel (1 slot)
- **Sat:** Mega Retail Brand (1 slot)
- **Sun:** City Development Auction (New land)

### 3. Fictional Franchises
Use fictional brands (Starbrew Coffee, Pizza Planet, FreshMart, CineMax, QuickBite) to avoid legal issues, leaving room for real official licensed events (like Starbucks) later if partnerships occur.

### 4. Franchise vs. Owned Business Strategy
- **Owned Business:** Build for ₹10M, Revenue ₹200K/day, Profit ₹80K/day. (Total control)
- **Franchise:** Fee ₹50M, Revenue ₹500K/day, Royalty 15%, Brand bonus +30% customers.
*Real strategic choices for the player.*

### 5. Franchise Scarcity
Not everyone can own the franchise. If Starbrew opens 3 slots in New Delhi, 1,248 players might compete. Winners lock it in for a duration, creating reasons for daily return.

### 6. Franchise Contracts
e.g., Starbrew Contract (30 days, ₹75M investment, 12% royalty, +35% customers). At the end of 30 days, renew for ₹90M or lose it.

### 7. Dynamic Market Events
- 🌧️ **Heavy Monsoon (Mumbai):** Cinema +20%, Delivery +40%, Outdoor -25%.
- 🎉 **Festival Week (Jaipur):** Hotels +50%, Tourism +60%.
- 🏏 **Major Cricket Final (Mumbai):** Sports Bars +80%, Pizza +35%.

### 8. In-Game News Ticker
```text
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
       📰 CITY BUSINESS NEWS

Starbrew announces expansion into
New Delhi. Three franchise locations
will be auctioned today.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
Players can tap `[VIEW EVENT]` to see remaining time, highest bids, and place their own bids.

### 9. Diverse Event Types
Franchise Auctions, Land Auctions, Supply Contracts (discounted inventory for 24h), Partnerships, Crisis (category loses profitability), Market Boom, Government Development (new metro increases traffic), Investor Events.

### 10. Persistent World State
Backend maintains a `WorldState` tracking the Day, active Cities, Active Events, and Market Modifiers that universally apply to all players.

### 11. Expanded Leaderboards
- **Global Empire Ranking**
- **City Rankings** (e.g., #1 in New Delhi)
- **Franchise Rankings** (e.g., #1 in Coffee Empire)
- **Business Reputation** (Most Trusted Business Owner)

### 12. City Ownership / Influence
Players aim to dominate commercial influence in specific cities.
- **Delhi:** 82% Influence
- **Mumbai:** 41% Influence
Goal: Become the most influential business empire in India.

### 13. Meaningful Multiplayer Integration
Players participate in the same evolving global economy. When a franchise opportunity appears, everyone competes, someone wins, and others adapt.

---

## The Core Game Loop

```text
               START
                 │
                 ▼
            Build Shop
                 │
                 ▼
             Hire Staff
                 │
                 ▼
          Serve Customers
                 │
                 ▼
              Earn ₹
                 │
                 ▼
            Buy Land
                 │
                 ▼
          Build Businesses
                 │
                 ▼
       ┌─────────┴─────────┐
       │                   │
       ▼                   ▼
   Daily Events        City Events
       │                   │
       └─────────┬─────────┘
                 ▼
          Franchise / Land
              Auctions
                 │
                 ▼
          Compete with Players
                 │
                 ▼
          Expand Your Empire
                 │
                 ▼
          Global Leaderboard
                 │
                 └──────► Repeat
```

### Technical Architecture Recommendation
**Tech Stack:** Unity 3D + deterministic economic simulation + AI NPC layer + FastAPI backend + UGS multiplayer + global leaderboard.

*Focus on making "build your own living commercial district" the central fantasy.*
