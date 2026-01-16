# Light Reveal System Setup Guide

This guide will help you set up the fog of war/light reveal system for your 2D Unity game.

## Overview

The system consists of three main components:
- **UnlitBlackout Shader**: Renders everything black except where the sprite mask cuts through
- **LightRevealManager**: Manages the blackout overlay across the screen
- **LightRevealController**: Controls individual light sources and their properties

## Setup Instructions

### Step 1: Create the Blackout Material

1. In Unity, navigate to `Assets/Materials` (or create this folder if it doesn't exist)
2. Right-click and select `Create > Material`
3. Name it `BlackoutMaterial`
4. In the Inspector, change the shader dropdown to `Custom/UnlitBlackout`
5. The material should now be solid black

### Step 2: Setup the Light Reveal Manager

1. In your scene hierarchy, create an empty GameObject
2. Name it `LightRevealManager`
3. Add the `LightRevealManager` component to it
4. In the Inspector:
   - **Main Camera**: Drag your main camera here (it will auto-detect Camera.main if left empty)
   - **Blackout Material**: Drag the `BlackoutMaterial` you created in Step 1
   - **Blackout Sorting Layer**: Set to the layer you want (default "Default")
   - **Blackout Sorting Order**: Set to a high number (e.g., 1000) to ensure it renders on top of everything

### Step 3: Create a Light Source

1. In your scene, create a new GameObject (or use an existing sprite object)
2. Name it something like `Light_Revealer`
3. Add a `Sprite Renderer` component (if not already present)
   - Assign a sprite (this is the shape that will reveal the area)
   - For soft edges, use a sprite with alpha gradient (e.g., a circle sprite that fades from white center to transparent edges)
4. Add a `Sprite Mask` component (Unity will add this automatically when you add LightRevealController)
5. Add the `LightRevealController` component
6. In the Inspector for `LightRevealController`:
   - **Is Light On**: Check to enable the light
   - **Edge Softness**: Adjust from 0 (hard edge) to 1 (soft/feathered edge)

### Step 4: Configure the Sprite Mask

The `LightRevealController` automatically configures the sprite mask, but here's what's happening:

1. Select your light GameObject
2. In the `Sprite Mask` component:
   - **Sprite**: Should be set to your reveal sprite
   - **Alpha Cutoff**: Controlled by Edge Softness slider
   - **Custom Range**: Enabled automatically
3. In the `Sprite Renderer` component:
   - The renderer is automatically disabled (we only need the mask, not the visible sprite)

### Step 5: Configure Stencil Settings in URP

For the sprite mask to work with the blackout overlay, ensure your project is using URP (Universal Render Pipeline):

1. The stencil settings are already configured in the shader
2. Sprite masks write to the stencil buffer (Ref 1)
3. The blackout shader only renders where stencil is NOT 1

### Step 6: Test Your Setup

1. Press Play
2. You should see everything black except where your light sprite is positioned
3. Try adjusting the `Edge Softness` slider in real-time
4. Toggle `Is Light On` to turn the light on/off

## Usage Examples

### Turning Light On/Off via Script

```csharp
using _Scripts.Core;

// Get reference to the light controller
LightRevealController light = GetComponent<LightRevealController>();

// Turn on
light.TurnOn();

// Turn off
light.TurnOff();

// Toggle
light.Toggle();
```

### Changing Edge Softness at Runtime

```csharp
LightRevealController light = GetComponent<LightRevealController>();
light.edgeSoftness = 0.8f; // Very soft edge
```

### Changing the Light Shape

```csharp
LightRevealController light = GetComponent<LightRevealController>();
light.SetSprite(myNewSprite); // Change to a different sprite shape
```

## Creating Better Reveal Sprites

For the best results with soft edges:

1. Create a sprite in your image editor (Photoshop, GIMP, etc.)
2. Make the center white (255, 255, 255, 255)
3. Use a gradient to fade to transparent at the edges (alpha = 0)
4. Import into Unity
5. Set Texture Type to "Sprite (2D and UI)"
6. Enable "Alpha is Transparency"
7. Set Filter Mode to "Bilinear" or "Trilinear" for smooth gradients

## Multiple Lights

To have multiple light sources:

1. Simply duplicate your light GameObject
2. Each light can have its own:
   - Sprite shape
   - Edge softness
   - On/off state
3. All lights work together - areas revealed by ANY light will be visible

## Troubleshooting

### Everything is black, no reveal area
- Check that `LightRevealController.isLightOn` is true
- Verify the sprite mask has a sprite assigned
- Ensure the light GameObject is positioned within the camera view

### Light has hard edges when softness is high
- Make sure your sprite has an alpha gradient
- Check sprite import settings (Alpha is Transparency should be enabled)
- Try increasing the sprite's resolution

### Blackout overlay doesn't cover the whole screen
- Check `LightRevealManager.mainCamera` is assigned correctly
- The overlay updates in `LateUpdate()` - it should auto-adjust to camera size

### Nothing renders
- Ensure `BlackoutMaterial` uses the `Custom/UnlitBlackout` shader
- Check that the blackout sorting order is high enough to render on top
- Verify your camera is rendering the correct layers

## Architecture Notes

This system uses:
- **Sprite Masks** (Unity built-in) for the reveal areas
- **Stencil Buffer** to punch holes in the blackout
- **Unlit Shader** for performant black overlay
- No render textures needed, single pass rendering

The system is performant because:
- Only one blackout quad is rendered
- Sprite masks use the stencil buffer (hardware accelerated)
- No expensive post-processing effects
- Works with Unity's batching system
