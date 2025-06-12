# Quest App Debugging Guide

## Current Issues and Solutions

### 1. Platform Detection Issue
**Problem**: Quest platform is not being detected correctly, defaulting to "mobile" client type.

**Solution Implemented**:
- Enhanced platform detection with multiple fallback methods
- Added delayed initialization to wait for XR systems
- Checking for MetaXRFeature, OVRManager, and XR settings

### 2. Component Initialization Order
**Problem**: Extensions were being disabled due to mode check before manager was initialized.

**Solution Implemented**:
- Removed RequireComponent from all Extension classes
- Extensions now find manager using GetComponentInParent() or FindFirstObjectByType()
- Mode check moved to after initialization in coroutine

### 3. WebSocket Connection Closing
**Problem**: Server closes connection immediately after register message (code 1000).

**Possible Causes**:
1. Server requires authentication (REQUIRE_AUTH environment variable)
2. Invalid room ID format
3. Rate limiting from IP address
4. Client type mismatch (Quest being registered as "mobile")

**Debug Steps**:
1. Check server logs for specific error messages
2. Verify REQUIRE_AUTH is set to false on server
3. Ensure room ID matches pattern: ^[a-zA-Z0-9_-]+$
4. Check server is running on correct port (9090)

## Recommended Server Launch Command
```bash
cd signaling-server
REQUIRE_AUTH=false PORT=9090 npm start
```

## Unity Scene Setup
1. Add UnityVerseBridgeManager to scene
2. Assign ConnectionConfig asset with:
   - signalingServerUrl: ws://YOUR_SERVER_IP:9090
   - clientType: Quest (should auto-detect)
   - roomId: valid room ID (e.g., "quest-room-001")
3. Assign VR Camera reference
4. Extensions will be added automatically as child components

## Debugging Tools
- Use PlatformDebugger component to verify XR detection
- Check Unity Console for detailed connection logs
- Monitor server console for connection attempts