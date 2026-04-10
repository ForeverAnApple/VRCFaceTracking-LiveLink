# VRCFaceTracking LiveLink Module

A [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) module that uses Apple ARKit face tracking via the [Live Link Face](https://apps.apple.com/us/app/live-link-face/id1495370836) app to drive facial expressions on compatible VRChat avatars.

Supports eye tracking, eye expressions, and 61 ARKit blendshapes mapped to VRCFaceTracking's Unified Expressions system.

## Requirements

- iPhone X or newer, iPad Pro 11-inch (1st gen+), or iPad Pro 12.9-inch (3rd gen+)
- [Live Link Face](https://apps.apple.com/us/app/live-link-face/id1495370836) app installed on your Apple device
- [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) installed
- Apple device and PC on the same local network

## Installation

1. Download `LiveLinkModule.zip` from the [latest release](https://github.com/ForeverAnApple/VRCFaceTracking-LiveLink/releases/latest)
2. In VRCFaceTracking, go to the Module Registry and install the zip

Things should work out of the box. If you run into issues, try restarting VRCFaceTracking or toggling OSC off and on.

## Usage

1. Open Live Link Face on your Apple device
2. Go to Settings > Live Link and add your PC's local IP address (leave port as default `11111`)
3. Return to the main screen and ensure the Live button is green
4. Launch VRCFaceTracking -- the LiveLink module should appear and start receiving data

## Building from Source

```
git clone --recurse-submodules https://github.com/ForeverAnApple/VRCFaceTracking-LiveLink.git
dotnet build VRCFaceTracking-LiveLink.sln -c Release
```

The built DLL will be at `LiveLinkModule/bin/Release/net7.0/LiveLinkModule.dll`.

## Credits

- [benaclejames/VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking)
- [Unreal Engine Live Link Face](https://apps.apple.com/us/app/live-link-face/id1495370836)
