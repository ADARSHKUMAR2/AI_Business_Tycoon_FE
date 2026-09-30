# Event Banner UI - Setup Guide

## ✅ Implementation Complete

All code has been successfully created for displaying active franchise tournament events in Unity!

## 📁 Files Created/Modified

### Backend (Fixed - Already Complete)
- `backend/shared/database.py` - Added FranchiseEvent to Beanie initialization
- `backend/services/game/controllers/event_controller.py` - Fixed query syntax
- `backend/services/game/utils/state_manager.py` - Fixed query syntax

### Unity Frontend (NEW)
1. **`Assets/Scripts/Data/EventData.cs`** ✅
   - EventResponse model matching backend API
   - Helper methods for time calculations and formatting

2. **`Assets/Scripts/Config/BackendConfig.cs`** ✅ (Modified)
   - Added event endpoint URLs
   - GetActiveEventURL() and GetRegisterEventURL() methods

3. **`Assets/Scripts/Game/Services/TycoonAPIService.cs`** ✅ (Modified)
   - GetActiveEvent() method
   - RegisterForEvent() method

4. **`Assets/Scripts/Game/Managers/EventManager.cs`** ✅ (NEW)
   - Singleton manager for event state
   - Auto-polls backend every 60 seconds
   - Registration logic with money validation
   - Events: OnEventUpdated, OnEventEnded, OnRegistrationSuccess

5. **`Assets/Scripts/UI/EventBannerUI.cs`** ✅ (NEW)
   - Banner UI component with fade animations
   - Live countdown timer
   - Join button with affordability check
   - Auto show/hide based on event availability

---

## 🎮 Unity Setup Instructions

### Step 1: Add EventManager to Scene
1. Open your main game scene
2. Create Empty GameObject: `GameObject > Create Empty`
3. Name it: `EventManager`
4. Add Component: `Event Manager`
5. Inspector Settings:
   - Poll Interval: `60` (seconds)
   - Initial Poll Delay: `2` (seconds)
   - Enable Debug Logs: ✓

### Step 2: Create Event Banner UI

**Create UI Hierarchy:**

```
Canvas (your existing canvas)
└── EventBannerPanel (NEW)
    ├── BackgroundPanel (Image - semi-transparent dark)
    ├── TitleText (TextMeshProUGUI - "STARBUCKS TOURNAMENT")
    ├── StatusText (TextMeshProUGUI - "● ACTIVE")
    ├── DetailsText (TextMeshProUGUI - "Entry: ₹5000 | Players: 5/10")
    ├── TimerText (TextMeshProUGUI - "Ends in: 25:30")
    └── JoinButton (Button)
        └── ButtonText (TextMeshProUGUI - "JOIN NOW")
```

**EventBannerPanel Settings:**
- RectTransform: Anchor Top-Center, Position Y=-80, Size 600x120
- Add: Canvas Group component (for fade animation)
- Add: EventBannerUI script

**Wire Inspector:**
- Drag all UI elements to EventBannerUI component slots
- Set colors: Active (green), Upcoming (yellow), Registered (gray)

### Step 3: Test

**Create Event via Backend:**
```bash
POST http://localhost:8000/api/game/events/create
{"franchise_name": "Starbucks", "duration_minutes": 30, "entry_fee": 5000}
```

**Expected:**
- Banner slides in after 2-3 seconds
- Shows event name, status, timer
- Join button works (deducts ₹5000)

---

## 🎯 How It Works

1. **EventManager polls** backend every 60 seconds for active events
2. **When event found**, fires OnEventUpdated event
3. **EventBannerUI subscribes** and shows banner with fade-in
4. **Timer updates** every frame showing countdown
5. **Join button** validates money and calls RegisterForEvent API
6. **On success**, button shows "✓ REGISTERED"

---

## 🔧 Integration Points

- Uses `GameManager.Instance.CurrentPlayer` for player data
- Works with existing `LeaderboardPanel` (WebSocket rankings)
- Uses existing `TycoonAPIService` for HTTP requests
- Follows project's namespace pattern (`AIBusinessTycoon.*`)

---

## ✅ Quick Checklist

- [ ] All scripts compiled without errors
- [ ] EventManager in scene
- [ ] EventBannerPanel UI created
- [ ] All Inspector references assigned
- [ ] Backend API running
- [ ] Test event created
- [ ] Banner appears in Play Mode
- [ ] Join button works

**Done! Your event system is ready! 🎉**
