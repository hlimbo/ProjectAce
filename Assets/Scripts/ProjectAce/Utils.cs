﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAce
{
    public static class Utils
    {
        public static readonly Dictionary<string, Sprite> cardAssets = new Dictionary<string, Sprite>();
        public static readonly Dictionary<string, Sprite> avatarAssets = new Dictionary<string, Sprite>();
        
        public static void LoadCardAssets()
        {
            // Load cards when they aren't loaded
            if (cardAssets.Count > 0) return;

            var sprites = Resources.LoadAll<Sprite>("CardAssets");
            foreach(var sprite in sprites)
            {
                cardAssets[sprite.name] = sprite;
            }
        }

        public static void LoadAvatarAssets()
        {
            if (avatarAssets.Count > 0) return;

            var sprites = Resources.LoadAll<Sprite>("Avatars");
            foreach(var sprite in sprites)
            {
                avatarAssets[sprite.name] = sprite;
            }
        }
    }
}
