# Unity Editor Scripts - Quick Guide

## Files Created

1. `Assets/Editor/EventBannerUISetup.cs` - Creates Event Banner UI
2. `Assets/Editor/EventManagerSetup.cs` - Creates EventManager GameObject

## How to Use (30 seconds setup!)

### Step 1: Create EventManager
**Menu:** `GameObject > AI Business Tycoon > Create Event Manager`
- Creates EventManager GameObject with component
- Auto-configured with default settings

### Step 2: Create Event Banner UI
**Menu:** `GameObject > UI > AI Business Tycoon > Create Event Banner`
- Creates complete UI hierarchy (10+ objects)
- All RectTransforms configured
- All Inspector references wired automatically
- Ready to use immediately!

## What Gets Created

```
EventBannerPanel (600x120, top-center)
├── CanvasGroup (for animations)
├── EventBannerUI (component, all refs wired!)
├── BackgroundPanel (semi-transparent black)
├── ContentContainer
│   ├── EventIcon (80x80)
│   └── InfoContainer
│       ├── TitleText (24pt, Bold)
│       ├── StatusText (16pt, Green)
│       └── DetailsText (14pt)
├── TimerText (18pt, Right-aligned)
└── JoinButton (Green, 150x40)
    └── ButtonText (16pt, Bold)
```

## Auto-Configured

✅ All positions, anchors, sizes
✅ All text fonts, colors, alignment
✅ Button colors (normal, hover, pressed, disabled)
✅ Layout groups with spacing
✅ All EventBannerUI references
✅ Canvas created if missing

## Testing

1. Use menu items to create EventManager + Banner
2. Create test event via backend API
3. Press Play
4. Banner appears after 2-3 seconds!

## Benefits

**Manual Setup:** 15-20 minutes
**With Scripts:** 30 seconds (2 clicks)

No errors, perfectly configured, ready to test!

