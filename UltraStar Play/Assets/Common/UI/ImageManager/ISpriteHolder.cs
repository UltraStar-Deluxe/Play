using System.Collections.Generic;
using UnityEngine;

// Indicates a script that references a sprite, which has been loaded at runtime via the ImageManager.
// When loading an image at runtime via the ImageManager that is not visible to the ImageManager afterwards
// (i.e. not in Scene hierarchy or current UIDocument),
// then this interface must be used to indicate where the Sprite is used in the scene.
// Otherwise the Sprite may be considered as unused and could be removed from the ImageManger's cache to free memory.
public interface ISpriteHolder
{
    IReadOnlyCollection<Sprite> GetSprites();
}
