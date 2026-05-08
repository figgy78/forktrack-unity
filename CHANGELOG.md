# Changelog

All notable changes to the ForkTrack Unity Package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.13] - 2026-05-08

### Fixed
- `PerformUnlock` now cascades to dependent nodes, matching the existing post-Complete cascade. Previously, children connected via `OnUnlock` edges only unlocked if `UnlockRootNodes` happened to iterate them after their parent in the JSON node array; otherwise they stayed Locked until the parent was Completed, making OnUnlock edges behave like OnComplete.

## [1.0.0] - 2026-01-12

### Added

#### Core Features
- Static `ForkTrack` API for easy integration
- C# event system for reactive game logic
- Full v1.2.0 schema support with automatic migration from v1.0.0 and v1.1.0
- Node state machine (Locked → Unlocked → Completed)
- Cascade unlock system with dependency evaluation

#### Variable System
- NUMBER and BOOLEAN variable types
- Variable operations: SET, ADD, SUBTRACT, TOGGLE
- Variable conditions on edges: ==, !=, >, <, >=, <=
- Variable actions triggered by node state changes

#### Dependency System
- AND/OR/NOT dependency conditions
- RequiredState support (OnUnlock, OnComplete)
- Variable conditions integrated with edge evaluation

#### Persistence
- Save/Load progress with named slots
- Export/Import progress as JSON strings
- Graph caching (memory + disk)
- Offline-first design

#### API Integration
- Bearer token authentication
- SSL certificate validation
- Configurable API endpoints
- Graph listing and fetching

#### Editor Tools
- ForkTrack Settings Window (Tools > ForkTrack > Settings)
- Token validation and connection testing
- Graph browser
- Cache management

#### Components
- `ForkTrackController` MonoBehaviour for Inspector-based workflow
- UnityEvents for all ForkTrack events
- Auto-save and auto-load progress options
- Custom Inspector with runtime debugging

#### Samples
- **Basic Usage**: Loading, events, completion, variables
- **Quest System**: Complete quest tracking with categories

#### Events
- `OnGraphLoaded` / `OnGraphLoadError`
- `OnNodeUnlocked` / `OnNodeCompleted` / `OnNodeReset`
- `OnNodeStateChanged`
- `OnVariableChanged`
- `OnEventTriggered` / `OnNoteFired`

### Security
- Encrypted token storage in builds
- No SSL bypass in release builds
- Device-specific encryption keys
- Tokens never logged

### Compatibility
- Unity 2021.3 LTS and later
- No external dependencies
- Mobile platform support (iOS, Android)
- WebGL support

## [Unreleased]

### Planned
- API-based graph loading with caching
- Real-time sync (WebSocket) option
- Visual graph debugger
- More sample scenes
