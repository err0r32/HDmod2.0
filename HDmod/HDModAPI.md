# HD Mod API Reference

## Overview
This document describes the public API for HD texture replacement system that other mods can interact with.

## Core Services

### `HDTextureManager`
Main service for texture management

```csharp
public class HDTextureManager
{
    // Enables/disables HD texture replacement globally
    public bool Enabled { get; set; }
    
    // Gets texture with automatic fallback
    public Task<Texture2D> GetTextureAsync(string texturePath, bool preloadOnly = false);
    
    // Preloads textures for specific location
    public Task PreloadTexturesForLocation(string locationIdentifier);
}
HDTextureRegistry
For mods to register their own HD textures

csharp
public class HDTextureRegistry
{
    // Registers texture override
    public void RegisterTexture(
        string originalPath, 
        string hdPath,
        TexturePriority priority = TexturePriority.Normal);
    
    // Unregisters texture override
    public void UnregisterTexture(string originalPath);
}

public enum TexturePriority
{
    Low,       // Will be overridden by other mods
    Normal,    // Default priority
    High,      // Will override other mods
    Critical   // Always takes precedence
}
Integration Points
Texture Replacement
Mods can provide their own HD textures:

csharp
// In your mod's initialization:
var registry = HDServiceLocator.Get<HDTextureRegistry>();
registry.RegisterTexture(
    originalPath: "Content/Items/Tools/welder.png",
    hdPath: "Content/HD/MyMod/welder_hd.dds",
    priority: TexturePriority.High);
Event Hooks
Subscribe to texture events:

csharp
HDEventDispatcher.OnTextureLoaded += (originalPath, hdPath, texture) => 
{
    DebugConsole.NewMessage($"Texture loaded: {hdPath ?? originalPath}");
};
Available events:

OnTextureLoaded

OnTextureCacheMiss

OnPreloadComplete

Configuration
Via XML
Other mods can include HD configuration:

xml
<!-- In your mod's XML file -->
<HDConfig>
  <Textures>
    <Texture original="Content/Items/Tools/welder.png" 
             hd="Content/HD/MyMod/welder_hd.dds"
             priority="High"/>
  </Textures>
  <Settings>
    <DisableForItems>
      <Identifier>welder</Identifier>
    </DisableForItems>
  </Settings>
</HDConfig>
Best Practices
Texture Naming:

Keep original filenames when possible (tool.png → tool_hd.dds)

Place in Content/HD/[YourModName]/ folder

Performance:

Use DDS format when possible

Register textures during init phase

Provide multiple LOD versions for large textures

Compatibility:

Use appropriate priority levels

Test with other HD-enabled mods

Provide fallback to vanilla textures

Example Implementation
csharp
public class MyMod : IAssemblyPlugin
{
    public void Initialize()
    {
        var registry = HDServiceLocator.Get<HDTextureRegistry>();
        
        // Register textures
        registry.RegisterTexture(
            "Content/Items/Tools/welder.png",
            "Content/HD/MyMod/welder_hd.dds");
            
        // Subscribe to events
        HDEventDispatcher.OnTextureLoaded += TextureLoadedHandler;
    }
    
    private void TextureLoadedHandler(string original, string hd, Texture2D texture)
    {
        if (original.Contains("welder"))
        {
            // Special handling for welder textures
        }
    }
}
Versioning
API version: 1.2
Stability: Stable
Barotrauma compatibility: 1.8.7.0+