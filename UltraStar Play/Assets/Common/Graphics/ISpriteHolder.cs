using UnityEngine;

// Indicates a script that references a sprite, which has been loaded at runtime via the ImageManager.
// When loading an image at runtime via the ImageManager for something different than a UnityEngine.UI.Image,
// then this interface must be used to indicate where the Sprite is used in the scene.
// Otherwise the Sprite may be considered as unused and could be removed from the ImageManger's cache to free memory.
public interface ISpriteHolder
{
    Sprite GetSprite();
}
