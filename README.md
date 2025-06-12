# UnityVerse Quest App

Meta Quest VR application that streams camera view to mobile devices.

## Quick Setup

1. **Open in Unity Hub**
   - Unity version: 6 LTS (6000.0.33f1) or 2022.3 LTS
   - Open folder: `quest-app/UnityProject`

2. **Build Settings** 
   - File → Build Settings → Android
   - Texture Compression: ASTC
   - Click "Switch Platform"

3. **XR Settings**
   - Edit → Project Settings → XR Plug-in Management
   - Enable "OpenXR" for Android
   - Add "Meta Quest" feature

4. **Connection Setup**
   - Open scene: `Assets/Scenes/QuestScene`
   - Select `UnityVerseBridge` GameObject
   - In Inspector, find `ConnectionConfig`
   - Set your signaling server URL and room ID

5. **Build & Run**
   - Connect Quest via USB
   - File → Build And Run
   - Accept any developer mode prompts

## Testing in Editor

1. Set `ConnectionConfig`:
   - Client Type: Quest
   - Room ID: test-room
   - Auto Start: ✓

2. Play the scene - it will simulate Quest mode

## Features

- Automatic VR camera detection
- Touch input visualization
- Haptic feedback support
- Auto-reconnection