# Billboard Presenter - Rendering On Top Guide

This guide explains how to make your story presenter render on top of everything without blending issues.

---

## Quick Solution ✅

The presenter now has **two settings** you can adjust:

### **In Inspector:**

```
Billboard Presenter Component:
├─ Rendering
│  ├─ Always On Top: ✅ true    ← Enable this!
│  └─ Sorting Order: 100        ← Higher = more on top
```

**That's it!** This should fix the transparency blending issues.

---

## How It Works

### 1. **Canvas Sorting Order**
```csharp
canvas.sortingOrder = 100;  // Higher values render on top
canvas.overrideSorting = true;
```
- Default UI renders at order 0
- Your billboard at 100 renders after everything else
- Adjust this if you have multiple overlapping UI elements

### 2. **Depth Testing Disabled**
```csharp
material.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
```
- Normally UI checks depth buffer (what's in front/behind)
- `CompareFunction.Always` = "render regardless of depth"
- Result: Billboard always visible, no blending with 3D objects

### 3. **Render Queue**
```csharp
material.renderQueue = 4000;
```
- Determines when in the frame the UI renders
- 4000 = after all transparent objects
- Ensures it's the last thing drawn

---

## Settings Breakdown

### **Always On Top** (Default: `true`)

**✅ Enabled (Recommended for VR):**
- Billboard never hidden by 3D objects
- No transparency blending issues
- Perfect for critical UI/dialog

**❌ Disabled:**
- Billboard respects depth (can be hidden by objects)
- More realistic but can cause occlusion issues
- Use if you want UI to feel "part of the world"

### **Sorting Order** (Default: `100`)

**Higher values = render later = on top**

```
Sorting Order 0   → Background UI
Sorting Order 50  → Normal world-space UI
Sorting Order 100 → Story presenter (default)
Sorting Order 200 → Critical alerts/warnings
```

**When to adjust:**
- **Increase** if billboard is behind other UI
- **Decrease** if you want other UI on top of billboard
- Keep at 100 for most story use cases

---

## Alternative Solutions (If Needed)

### Solution A: Layer-Based Rendering

If you need more control, use Unity's layer system:

**1. Create a new Layer:**
```
Edit → Project Settings → Tags and Layers
Add layer: "StoryUI"
```

**2. Assign presenter to layer:**
```csharp
gameObject.layer = LayerMask.NameToLayer("StoryUI");
```

**3. Setup camera rendering:**
```
Main Camera:
  - Culling Mask: Everything EXCEPT StoryUI

UI Camera (new):
  - Clear Flags: Depth Only
  - Culling Mask: ONLY StoryUI
  - Depth: Higher than main camera
```

**Result:** Story UI renders in a separate pass, always on top.

### Solution B: Screen Space Overlay (Not Recommended for VR)

**Change canvas mode:**
```csharp
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
```

**Pros:**
- Always renders on top
- No depth issues ever

**Cons:**
- ❌ Doesn't work well in VR (flat on screen)
- ❌ Loses world-space positioning
- ❌ No 3D placement

**Use case:** Desktop/2D games only.

### Solution C: Custom Shader

For advanced control, create a custom shader:

**Create: `AlwaysOnTop.shader`**
```hlsl
Shader "UI/AlwaysOnTop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay+100" 
            "RenderType"="Transparent" 
        }
        
        Pass
        {
            ZTest Always  // ← Always render on top
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
```

**Apply to UI:**
```csharp
Material customMat = new Material(Shader.Find("UI/AlwaysOnTop"));
image.material = customMat;
```

---

## Troubleshooting

### ❌ Problem: Billboard still blends with objects

**Solutions:**
1. **Increase sorting order** to 200 or 300
2. **Check `alwaysOnTop` is enabled** in inspector
3. **Ensure camera is assigned** to Canvas.worldCamera
4. **Check material** is being applied (look for debug logs)

### ❌ Problem: Billboard renders BEHIND other UI

**Solutions:**
1. **Increase sorting order** above other UI elements
2. Check other Canvas components - they may have higher sorting
3. Ensure `canvas.overrideSorting = true`

### ❌ Problem: Text looks weird/pixelated when on top

**Solutions:**
1. This is normal with depth-less rendering in some cases
2. **Increase canvas scale** slightly (try 0.0015 instead of 0.0012)
3. **Use higher quality font** textures in TMP settings
4. Enable **soft edges** in TextMeshPro settings

### ❌ Problem: Billboard visible through walls (don't want that)

**Solutions:**
1. **Disable `alwaysOnTop`** in inspector
2. Use **raycast check** to hide billboard when occluded:

```csharp
void Update()
{
    // Check if anything blocks view to billboard
    bool isVisible = !Physics.Raycast(
        _head.position, 
        transform.position - _head.position, 
        Vector3.Distance(_head.position, transform.position)
    );
    
    canvasGroup.alpha = isVisible ? 1f : 0f;
}
```

---

## Performance Considerations

### ✅ Good Performance:
- Using `alwaysOnTop` with sorting order (built-in)
- Single material shared across UI elements
- Canvas set to `overrideSorting`

### ⚠️ Can Impact Performance:
- Multiple cameras for layer-based rendering
- Custom shaders with complex effects
- Per-frame raycast occlusion checks

### 💡 Best Practice:
The built-in solution (alwaysOnTop + sortingOrder) is optimized and works great for VR. Use it unless you have specific advanced needs.

---

## Quick Reference

| Setting | Default | Effect |
|---------|---------|--------|
| `alwaysOnTop` | `true` | Disable depth testing, always visible |
| `sortingOrder` | `100` | Render order (higher = later = on top) |
| `canvas.overrideSorting` | `true` | Use custom sorting order |
| `material.renderQueue` | `4000` | Render after transparent objects |

---

## Summary

**For 99% of use cases:**
1. ✅ Leave `alwaysOnTop = true`
2. ✅ Adjust `sortingOrder` if needed (100 is good default)
3. ✅ Done!

The presenter will now render cleanly on top without blending issues! 🎉

