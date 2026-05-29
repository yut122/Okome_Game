# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity game project named **OKOME_PLEASE** — a "Papers Please"-style rice shop simulation where players judge customer purchase requests against regulations, selling or refusing accordingly.

- Unity version: 6 (URP 17.3.0)
- Render pipeline: Universal Render Pipeline
- Language: C# (Unity MonoBehaviour / ScriptableObject pattern)
- UI: TextMesh Pro (TMPro)

## Development Workflow

This project has no CLI build commands — all development is done inside the Unity Editor:

- **Open project**: Launch Unity Hub → open this folder
- **Play/test**: Use the Unity Editor Play button
- **Build**: File → Build Settings → Build
- **Tests**: Window → General → Test Runner (uses `com.unity.test-framework`)

There is no separate compile step; Unity recompiles scripts automatically on save.

## Architecture

The game follows a simple three-component design:

### Data Layer
- **`CustomerData.cs`** — `ScriptableObject` defining a customer: name, region, dialogue, requested rice amount (`requestKg`), coupon count, rice metadata (origin, grade, `riceDayOld`, moisture), and an `isFraud` flag.
- **`CustomerData01–03.asset`** — Pre-authored customer instances created via `[CreateAssetMenu]`. New customers are added as new `.asset` files using the same ScriptableObject.

### Logic Layer
- **`JudgeManager.cs`** — `MonoBehaviour` that holds a reference to the active `CustomerData` and evaluates three violation rules:
  1. Purchase quantity exceeds coupon allowance (`requestKg > coupons * 5`)
  2. Rice origin matches the daily banned region (`bannedOrigin`)
  3. Rice age exceeds 30 days (`riceDayOld > 30`)
  Returns a Japanese-language violation string, or `""` if no violation.

### Controller Layer
- **`GameController.cs`** — `MonoBehaviour` wired to UI buttons. Calls `JudgeManager.CheckViolation()` on sell; applies a +5,000円 reward (clean sale) or −10,000円 fine (violation). Displays result via a `TextMeshProUGUI` reference.

## Key Conventions

- All in-game strings and feedback text are in Japanese.
- Game rules (fine amounts, day limits, banned regions) are hardcoded in `JudgeManager` and `GameController` — adjust those constants directly when changing game balance.
- New violation types belong in `JudgeManager.CheckViolation()`.
- New customer fields belong in `CustomerData.cs`; all existing `.asset` files must be updated to include the new field.
