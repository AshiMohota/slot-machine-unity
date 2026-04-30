Jackpot Slot Machine — Unity Project

A feature-complete 3-reel slot machine game built for Unity with smooth animations,
weighted RNG, multiple bet amounts, and jackpot bonus features.

---

Game Overview

A classic 3-reel slot machine with 4 symbols (7, Cherry, Bell, BAR).
Win by landing the same symbol on all 3 reels on the payline.

| Symbol | Multiplier | Frequency |
|--------|-----------|-----------|
|  Seven | 500× | Rare (weight: 5) |
|  Cherry | 20× | Uncommon (weight: 20) |
|  Bell | 10× | Common (weight: 25) |
|  BAR | 5× | Most common (weight: 50) |

---

 How to Run the WebGL Build

1. Open the `/Build/WebGL` folder.
2. Open a local server (e.g., `python3 -m http.server 8080`).
3. Navigate to `http://localhost:8080` in your browser.
4. Or open `index.html` directly in Firefox.

---

 Project Structure

```
Assets/
├── Scripts/
│   ├── SlotMachineController.cs  ← Main game controller
│   ├── ReelController.cs         ← Per-reel animation & strip logic
│   ├── SlotSymbol.cs             ← Symbol data class
│   ├── UIAnimator.cs             ← Win-line, popups, coin counter
│   └── AudioManager.cs           ← Singleton audio manager
├── Prefabs/
│   ├── Reel.prefab
│   ├── SymbolCell.prefab
│   └── SlotMachine.prefab
├── Animations/
│   ├── ReelSpin.anim
│   └── WinPopupIn.anim
├── Sprites/
│   ├── Symbols/
│   └── UI/
└── Scenes/
    └── MainGame.unity
```

---

 Bonus Features

- **Staggered reel stops** — reels stop one after another for suspense
- **Jackpot particles** — particle burst on 3× Seven
- **Animated coin counter** — balance smoothly ticks up on win
- **Keyboard support** — Space/Enter to spin, 1/2/3 for bet amount
- **Responsive reel strip** — virtual strip with 30 symbols for smooth travel

---

 Thought Process

1. **RNG fairness**: Weighted random for each reel independently ensures true randomness.
   Symbol weights directly control expected RTP (return-to-player).

2. **Reel animation**: Used AnimationCurve for deceleration — fast spin → ease-out stop.
   Extra loops before landing create the classic "overspin" feel.

3. **Architecture**: SlotMachineController orchestrates everything; ReelController is
   self-contained. Event-based communication (OnSpinComplete) keeps coupling low.

4. **Win detection**: Simple ID comparison of all 3 landed symbols — O(1) check.
