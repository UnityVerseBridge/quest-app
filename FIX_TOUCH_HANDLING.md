# Touch Handling in Quest App

## Overview
Touch handling in Quest app is now automatically configured through the UnityVerseBridge Core package. The `TouchInputHandler` component is automatically added when in Host mode and handles all touch input from mobile devices.

## How It Works

### Automatic Setup
When you add UnityVerseBridgeManager in Host mode, it automatically adds:
1. **TouchInputHandler** - Receives touch data and performs 3D raycasting
2. **QuestTouchExtension** - Provides visual feedback for touches
3. **QuestVideoExtension** - Handles video streaming
4. **QuestHapticExtension** - Handles haptic feedback

### No Manual Setup Required
The TouchInputHandler is now part of the core package and is automatically configured:
- Receives normalized touch coordinates (0-1) from mobile devices
- Converts to screen coordinates using Quest's screen resolution
- Performs raycasting from VR camera
- Sends events to hit objects

## Making Objects Touchable

For objects to respond to touches, they need:

1. **Collider component** (BoxCollider, SphereCollider, etc.)
2. **VRClickHandler component** (for click events)
3. **(Optional) ITouchable interface** (for advanced touch handling)

### VRClickHandler Support
The TouchInputHandler now supports VRClickHandler:
- Sends `OnVRClick` message on touch began
- Directly calls `VRClickHandler.OnVRClick()` if component exists
- Provides visual feedback through the component

## Coordinate System

Both mobile and Quest use the same coordinate system:
- **Origin**: Bottom-left (0, 0)
- **X**: 0 (left) to 1 (right)
- **Y**: 0 (bottom) to 1 (top)
- Coordinates are normalized to handle different screen resolutions

## Debugging

### Enable Debug Mode
On `UnityVerseBridge_Quest` GameObject:
1. Find `TouchInputHandler` component (auto-added)
2. Set `Debug Mode` = ✓
3. Set `Show Touch Visualizer` = ✓

### Debug Output
```
[TouchInputHandler] Screen Resolution: 1920x1080
[TouchInputHandler] Normalized Touch: (0.500, 0.500)
[TouchInputHandler] Screen Position: (960, 540)
[TouchInputHandler] Camera: CenterEyeAnchor, FOV: 90
[TouchInputHandler] Touch from mobile-123 hit: InteractiveCube at (0.0, 1.0, 2.0)
```

### Mobile Side Debug
Enable debug on `MobileInputExtension`:
```
[MobileInputExtension] Screen Resolution: 1920x1080
[MobileInputExtension] Original Touch Position: (960, 540)
[MobileInputExtension] Sending touch 0: (0.500, 0.500) - Began
```

## Troubleshooting

### Touch Not Working
1. **Check Connection**: Ensure WebRTC data channel is open
2. **Check Components**: TouchInputHandler should be auto-added
3. **Check Camera**: VR camera must be assigned (auto-detected)
4. **Check Colliders**: Target objects need colliders

### Coordinate Mismatch
If touches appear offset:
1. Compare screen resolutions in debug logs
2. Check aspect ratios between devices
3. Verify both apps use same Unity version
4. Ensure no custom coordinate transformations

### Performance
- Touch events are sent at 60 FPS by default
- Only active touches are transmitted
- Raycasting uses layer mask for optimization

## Advanced Features

### ITouchable Interface
For advanced touch handling, implement ITouchable:
```csharp
public interface ITouchable
{
    void OnTouchBegan(Vector3 worldPosition);
    void OnTouchMoved(Vector3 worldPosition);
    void OnTouchEnded(Vector3 worldPosition);
}
```

### Multi-Touch Support
- Supports up to 10 simultaneous touches
- Each touch has unique ID
- Different colors for different peers (QuestTouchExtension)

### Custom Touch Handlers
You can still add custom touch handlers alongside the automatic system:
- Subscribe to WebRtcManager data channel events
- Process TouchData messages
- Implement custom logic

## Migration from VrTouchReceiver

If you see missing script warnings:
1. Remove the missing VrTouchReceiver component
2. The functionality is now handled by TouchInputHandler
3. No manual setup needed - it's automatic

The system is now more robust and integrated into the core package!