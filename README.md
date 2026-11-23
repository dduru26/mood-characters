# Mood Characters

An interactive character experience built in Unity 6 (URP) for the theme **Emotional or Mood-Based Character System**.

Three characters stand on a stage. You choose one and set its mood. Each mood changes the character's animation, plays its voice reaction, crossfades the background music to match, and recolours the interface. Only one character is active at a time, and everything is coordinated through a central manager layer.

**Video walkthrough:** _add link_
**Repository:** https://github.com/dduru26/mood-characters

---

## Controls

| Action | UI | Keyboard / Mouse |
|---|---|---|
| Set mood | Neutral, Happy, Angry, Sad buttons | `1` Neutral, `2` Happy, `3` Angry, `4` Sad |
| Switch character | Dropdown, or Prev / Next buttons | `Left` / `Right` or `A` / `D`, or click a character |
| Wave (reaction) | Wave button | `Space`, or click the active character |
| Go back a screen | Back button | `Esc` |
| Volume / mute | Master, Music, SFX sliders and Mute toggle (Settings) | |

Mood and character controls only work on the Stage screen. The menu and settings screens block them on purpose.

---

## Screens

1. **Main Menu**: Start, Settings, Quit.
2. **Stage (HUD)**: live panel showing the active character, its mood (colour-coded), the highlighted mood button, and the current Animator state, including blends (for example "Blending to Happy").
3. **Settings**: volume sliders and mute, saved between sessions with PlayerPrefs.

Each screen animates in and out with a fade, slide and scale tween. Navigation uses a history stack, so Back and `Esc` always return to the previous screen, and Home clears the stack.

---

## Animation setup

All three characters are Mixamo Humanoid rigs that share one reusable Animator Controller (`MoodCharacter.controller`), so the same five clips retarget onto each body.

**Parameters**

| Name | Type | Purpose |
|---|---|---|
| `Mood` | Int | 0 Neutral, 1 Happy, 2 Angry, 3 Sad |
| `IsActive` | Bool | Only the selected character may leave Idle |
| `React` | Trigger | Plays the wave reaction |

**States:** Idle (default), Happy, Angry, Sad, React.

**Transitions:** Idle is the hub. `Idle to Happy` requires `Mood == 1` and `IsActive == true` (same pattern for Angry and Sad), and each mood returns to Idle when `Mood` no longer matches. `Any State to React` fires on the trigger, and React returns to Idle on exit time, after which the character drops back into its current mood. Nothing plays automatically: every parameter change comes from user input through the GameManager.

---

## Audio

- **Background:** four mood music tracks on two AudioSources that crossfade whenever the mood or active character changes.
- **Character sounds:** each character has its own clip for every mood plus a reaction clip. They all play through **one shared character AudioSource** that is stopped before every new clip and whenever the active character changes, so sounds never overlap between characters or states.
- **UI sounds:** click and screen-change sounds on a separate source.
- **Controls:** Master (AudioListener), Music and SFX volumes, plus Mute, all persisted.

---

## System architecture

```
                 ┌──────────────┐
  Input ───────► │ InputManager │──┐
                 └──────────────┘  │ calls
  UI buttons ──────────────────────┤
                                   ▼
                          ┌──────────────────┐
                          │   GameManager    │  singleton: app state,
                          │ (single source   │  active character,
                          │   of truth)      │  only one active
                          └────────┬─────────┘
                    events:        │ ActiveCharacterChanged, MoodChanged,
                                   │ CharacterReacted, StateChanged
          ┌────────────────┬───────┴────────┬──────────────────┐
          ▼                ▼                ▼                  ▼
   ┌─────────────┐  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐
   │MoodCharacter│  │AudioManager │  │InteractionHUD│  │ CameraFocus │
   │ (Animator)  │  │music + SFX  │  │ live UI      │  │ follows     │
   └─────────────┘  └─────────────┘  └──────────────┘  └─────────────┘

   UIManager ── opens/closes UIScreen (tweened) ── sets GameManager state
```

| Script | Responsibility |
|---|---|
| `GameManager` | Singleton. Holds the app state (Menu, Interacting, Settings) and the active character, enforces one active character at a time, validates every action, and raises C# events. |
| `MoodCharacter` | Wraps one character's Animator: sets `Mood`, `IsActive`, `React`, owns its sound clips and selection ring, and reports its live Animator state. |
| `AudioManager` | Singleton. Listens to GameManager events, crossfades mood music, plays character clips on one non-overlapping channel, handles volume and mute persistence. |
| `UIManager` | Singleton. Screen navigation with a history stack (Open, Back, GoHome) and tells the GameManager which state each screen represents. |
| `UIScreen` | Reusable tweened screen (CanvasGroup fade + slide + scale) that knows its app state. |
| `InteractionHUD` | Reacts to character, mood and animation state changes. |
| `SettingsScreen` | Binds sliders and toggle to the AudioManager. |
| `InputManager` | Keyboard and mouse input (works with both the new Input System and the legacy one), raycast character selection, ignores clicks over UI. |
| `CameraFocus` | Smoothly frames the active character. |
| `ButtonClickSound` | Drop-in click sound for any button. |

**Why it's built this way:** input and UI never touch animators or audio directly. They ask the GameManager, which validates the request and broadcasts an event, and each system reacts on its own. Adding a fourth character or a new mood means adding a prefab or an enum value, not rewriting the systems.

---

## Project structure

```
Assets/_Project/
  Animations/    MoodCharacter.controller + Mixamo clips
  Audio/Music/   mood tracks
  Audio/SFX/     voice + UI sounds
  Characters/    Mixamo character models
  Materials/
  Prefabs/       character prefabs
  Scenes/        Main.unity
  Scripts/
    Core/        GameManager, Mood/AppState enums
    Characters/  MoodCharacter, CameraFocus
    Audio/       AudioManager
    UI/          UIManager, UIScreen, InteractionHUD, SettingsScreen, ButtonClickSound
    Input/       InputManager
```

## Credits

- Characters and animations: [Mixamo](https://www.mixamo.com)
- Music and sound effects: [Pixabay](https://pixabay.com) (Pixabay Content License)
