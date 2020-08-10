using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAce
{
    public static class Utils
    {
        public static readonly Dictionary<string, Sprite> cardAssets = new Dictionary<string, Sprite>();
        public static void LoadCardAssets()
        {
            var sprites = Resources.LoadAll<Sprite>("CardAssets");
            foreach(var sprite in sprites)
            {
                cardAssets[sprite.name] = sprite;
            }
        }
    }
}
