using System.Collections.Generic;
using UnityEngine;

namespace TwoLadles.UI
{
    /// <summary>
    /// Chef face portraits (head + upper body) cut from the tier-1 chef sprites.
    /// One source for the phone story contacts and the character picker. Cached per chef.
    /// </summary>
    public static class ChefPortraits
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>Portrait for "linh" / "jiwon"; null for unknown or unpicked chef ids.</summary>
        public static Sprite Get(string chefId)
        {
            if (chefId != "linh" && chefId != "jiwon") return null;
            if (cache.TryGetValue(chefId, out var s) && s != null) return s;
            s = HeadCrop(Resources.Load<Sprite>("Sprites/Characters/" + chefId + "_tier1"));
            if (s != null) cache[chefId] = s;
            return s;
        }

        // Top square of a full-body sprite = head and shoulders.
        public static Sprite HeadCrop(Sprite full)
        {
            if (full == null) return null;
            Rect r = full.rect;
            float side = Mathf.Min(r.width, r.height);
            var crop = new Rect(r.x, r.y + r.height - side, side, side);
            return Sprite.Create(full.texture, crop, new Vector2(0.5f, 0.5f), full.pixelsPerUnit);
        }
    }
}
