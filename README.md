# UnityVerse Quest App

Meta Quest VR application demonstrating UnityVerseBridge Core package usage for VR-to-mobile streaming.

## Overview

This is a sample Quest application that showcases:
- VR camera streaming to mobile devices
- Receiving touch input from mobile devices
- Interactive VR objects responding to remote touch
- Meta XR SDK integration

## Requirements

- Unity 6 LTS (6000.0.33f1) or Unity 2022.3 LTS
- Meta XR SDK
- UnityVerseBridge Core package
- Android Build Support
- Quest 2/3/Pro device

## Project Setup

1. Clone this repository
2. Open `UnityProject` folder in Unity
3. Import required packages:
   - UnityVerseBridge Core
   - Meta XR SDK
   - Unity WebRTC

## Configuration

1. Open the sample scene: `Assets/Scenes/QuestStreamingDemo.unity`
2. Find `UnityVerseBridge_Quest` GameObject
3. Configure the UnityVerseConfig:
   - Signaling URL: Your signaling server address
   - Room ID: Unique room identifier
   - Auto Connect: Enable for automatic connection

## Building for Quest

1. File > Build Settings
2. Switch to Android platform
3. Player Settings:
   - Minimum API Level: 29
   - Target API Level: 32+
   - Texture Compression: ASTC
4. XR Plug-in Management:
   - Enable Oculus
5. Build and Run

## Features Demonstrated

### VR Click Handler
- `VRClickHandler.cs`: Sample script showing how to handle remote touch input
- Provides visual feedback (color change, scale animation)
- Can trigger Unity Events

### Input System Fix
- `InputSystemFix.cs`: Workaround for Unity 6 + Meta XR compatibility
- Automatically applied at runtime

### Video Streaming
- Default resolution: 640x360 @ 30fps for optimal performance
- Adaptive quality based on connected peers
- H264 codec with hardware acceleration
- Passthrough support for mixed reality

## Testing in Editor

1. Install Meta XR Simulator
2. Window > Meta > XR Simulator
3. Enable "Play Mode OpenXR Runtime"
4. Enter Play mode

## Troubleshooting

### Unity 6 Input System Errors
The project includes `InputSystemFix.cs` to resolve known compatibility issues. If errors persist:
1. Edit > Project Settings > Player
2. Set Active Input Handling to "Input Manager (Old)"

### URP Missing Types Warnings
These warnings don't affect functionality. To resolve:
1. Update Universal RP package to latest version
2. Or regenerate URP Global Settings

### Build Errors
1. Ensure Android Build Support is installed
2. Check Meta XR SDK is properly imported
3. Verify minimum API level is 29+

## Sample Usage

The demo scene includes:
- A streaming camera setup
- Interactive 3D objects
- Touch visualization canvas
- Debug UI (can be disabled)

Run the Quest app first, then connect with the mobile app using the same room ID.

## License

See LICENSE file in the root repository.