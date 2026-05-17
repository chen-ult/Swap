# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a 2D platformer Unity game featuring a slime character with unique mechanics including wall-splitting, momentum-based movement, and state machine-driven gameplay. The project uses Unity's new Input System and DOTween for animations.

## Architecture

### Core Systems

**State Machine Architecture**
- Central `StateMachine` class manages entity states
- Player states: Idle, Move, Jump, Fall, FallEnd, Dead
- Enemy states: Idle, Move, Ground
- All states inherit from `EntityState` base class

**Entity System**
- `Entity` base class provides common functionality
- `Player` and `Enemy` classes extend Entity
- Components include stats, gaze detection, and animation triggers

**Manager System**
- `LevelManager`: Singleton managing scene transitions, checkpoints, and level progression
- `UIManager`: Handles UI fade transitions, health/stars display, and menu systems
- `MomentumSwapManager`: Manages momentum-based object swapping and bullet time mechanics

### Key Features

**Player Mechanics**
- Movement with ground/air control
- Wall jumping and sliding
- Slime splitting when hitting walls at high velocity (creates a clone)
- One-way platform passthrough
- Checkpoint-based respawn system

**Enemy System**
- Basic AI with player detection
- State-based behavior patterns
- Stun mechanics
- Battle state transitions

**Environmental Interactions**
- Moving platforms and paths
- Gravity attractors
- Momentum elevators and acceleration blocks
- Pressure buttons and toggle doors
- Spike traps and breakable obstacles
- Collectible stars and level keys

**Momentum Swap System**
- Press LeftShift to activate bullet time
- Click on swappable objects to select them
- Exchange or transfer momentum between objects
- Visual feedback with arrows and particle effects

## Development Commands

### Unity Editor Operations
- **Play Mode**: Press Play in Unity Editor to test gameplay
- **Scene Management**: Use `LevelManager.Instance.LoadNextLevel()` or `LoadSpecificLevel("SceneName")`
- **Checkpoint Testing**: Set checkpoints and test respawn with `LevelManager.Instance.RespawnAtCheckpoint()`

### Common Script Locations
- Player logic: `Assets/Script/Player/`
- Enemy logic: `Assets/Script/Enemy/`
- State machines: `Assets/Script/Player/PlayerState/` and `Assets/Script/Enemy/EnemyState/`
- Environment objects: `Assets/Script/Environment/`
- Managers: `Assets/Script/Managers/`
- UI components: `Assets/Script/UI/`

### Debugging
- Press `Delete` key in-game to clear all PlayerPrefs (for testing progression)
- Check Unity Console for debug logs during scene transitions
- Use DOTween Utility Panel (Tools > Demigiant > DOTween Utility Panel) for animation debugging

## Input System
- Uses Unity's new Input System package
- Input actions defined in `PlayerInputSet.cs`
- Movement input handled via `input.Player.Move.performed/canceled` events

## Animation & Tweening
- DOTween used for smooth animations and transitions
- State changes trigger animation parameter updates
- Scene transitions use `UIManager.Instance.FadeOutRoutine()`/`FadeInRoutine()`

## Important Implementation Notes

### State Machine Pattern
When adding new states:
1. Create state class inheriting from appropriate base (e.g., `PlayerState`)
2. Add state reference to entity class
3. Initialize state in entity's `Awake()` method
4. Implement Enter/Exit/Update logic following existing patterns

### Scene Management
- Always use `LevelManager` for scene transitions to ensure proper cleanup
- Checkpoint system uses `PlayerPrefs` for persistence
- Scene transitions kill all DOTween animations via `DOTween.KillAll()`
- Player spawn points prioritize `NextLevelDoor` for backward navigation

### Physics & Collision
- Ground detection uses raycasting with `whatIsGround` layer mask
- Collision handling in `OnCollisionEnter2D` for mechanics like wall-splitting
- Platform effector components used for one-way platforms

### Momentum System
- Swappable objects must implement `IMomentumSwappable` interface
- Momentum calculations support both true momentum (mass × velocity) and direct velocity swapping
- Speed limits prevent infinite velocity accumulation
- Transfer functionality can be unlocked via `isTransferUnlocked` flag

### UI System
- Heart-based health display system
- Star collection tracking
- Fade transitions between scenes
- Pause menu with restart functionality
- End sequence with typing text effects and time display

## Project Settings
- 2D physics with custom Physics2D settings
- Universal Render Pipeline (URP) configured
- Input System package integrated
- DOTween animation library included

## Asset Structure
- `Assets/Art/`: Sprite assets organized by theme (pixel adventure, cave)
- `Assets/Script/`: C# source code
- `Assets/DOTween/`: Animation library
- `Assets/InputSystem/`: Input action assets
- `Assets/Editor/`: Editor scripts (e.g., `ClearPlayerPrefs.cs`)