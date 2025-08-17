# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language
    必ず日本語で応対すること
## Project Overview

This is a Unity 6 project for testing mocopi (Sony's motion capture system) dual avatar functionality. The project integrates mocopi motion capture data reception with VRM 1.0 avatar support using the Universal Render Pipeline (URP).

## Core Architecture

### Motion Capture Data Flow
1. **MocopiUdpReceiver** (`Assets/MocopiReceiver/Runtime/Core/MocopiUdpReceiver.cs`): Receives UDP packets containing Sony Motion Format data
2. **SonyMotionFormat** (`Assets/MocopiReceiver/Runtime/SonyMotionFormat/Scripts/SonyMotionFormat.cs`): Native library wrapper for parsing motion data using P/Invoke
3. **MocopiSimpleReceiver** (`Assets/MocopiReceiver/Runtime/MocopiSimpleReceiver.cs`): High-level coordinator that manages UDP receivers and avatar binding
4. **MocopiAvatar** (`Assets/MocopiReceiver/Runtime/MocopiAvatar.cs`): Applies motion data to VRM avatars through bone mapping

### Key Integration Points
- The system uses delegate-based event handling for motion data callbacks
- Native libraries are included for iOS (`libsony_motion_format.a`), Android (`libsony_motion_format.so`), macOS (`sony_motion_format.bundle`), and Windows (`sony_motion_format.dll`)
- Assembly definition `com.sony.mocopi.receiver.asmdef` isolates mocopi functionality with platform-specific includes

### VRM Avatar System
- VRM 1.0 integration through `com.vrmc.vrm` package (v0.129.3)
- Avatar bone hierarchy mapping to mocopi skeleton data
- Runtime avatar loading and manipulation capabilities

## Development Commands

### Unity Editor Operations
- **Build**: Use Unity Editor → File → Build Settings (no CLI build scripts present)
- **Testing**: Use Unity Editor → Window → General → Test Runner (Unity Test Framework v1.4.5 included)
- **Package Management**: Unity Editor → Window → Package Manager

### Git Workflow
```bash
git status          # Check repository status
git add .          # Stage changes
git commit -m "message"  # Commit with message
git push           # Push to remote
```

### Platform-Specific Notes
- **macOS**: Native libraries are properly code-signed bundles
- **iOS/Android**: Cross-platform native libraries included for mobile deployment
- **Assembly Definitions**: Use Unity's assembly system for modular compilation

## Key Directories
- `Assets/MocopiReceiver/`: Core mocopi receiver functionality and samples
- `Assets/VRM10/`: VRM 1.0 avatar system and runtime components
- `Assets/Script/`: Custom project scripts (currently contains basic `Player.cs`)
- `ProjectSettings/`: Unity project configuration

## Testing and Validation
- Use Unity Play Mode for runtime testing of mocopi data reception
- Sample scene: `Assets/MocopiReceiver/Samples/ReceiverSample/Scenes/ReceiverSample.unity`
- Monitor Unity Console for mocopi UDP connection status and errors
- Verify native library loading across target platforms

## Dependencies and Packages
- Unity 6 (6000.0.31f1) with URP template
- Input System package for modern input handling
- Sony Motion Format native libraries for motion data parsing
- VRM 1.0 and UniGLTF packages for avatar support
- lilToon shader package for rendering

## Important Notes
- This project requires mocopi hardware and app for motion data streaming
- UDP port configuration is handled through MocopiSimpleReceiver settings
- Native library loading uses conditional compilation for platform-specific paths
- Assembly definitions ensure proper dependency isolation between mocopi and VRM systems