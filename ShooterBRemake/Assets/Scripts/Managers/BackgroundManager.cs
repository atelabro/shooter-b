using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public static class BackgroundManager
    {
        private static readonly Dictionary<string, string> BackgroundPaths = new Dictionary<string, string>
        {
            { "arcade_arc_de_triumphe", "Backgrounds/bckArcDeTriumpe" },
            { "arcade_eifel_paris", "Backgrounds/bckEiffelParis" },
            { "arcade_louvre_paris", "Backgrounds/bckLouvre" },
            { "arcade_moulen_paris", "Backgrounds/bckMoulen" },
            { "arcade_notre_dame_paris", "Backgrounds/bckNotreDame" },
            { "arcade_sacre_paris", "Backgrounds/bckSacreParis" }
        };

        private static readonly string[] ArcadeBackgroundKeys =
        {
            "arcade_arc_de_triumphe",
            "arcade_eifel_paris",
            "arcade_louvre_paris",
            "arcade_moulen_paris",
            "arcade_notre_dame_paris",
            "arcade_sacre_paris"
        };

        public static Constants.GameMode ResolveMode(Constants.GameMode? mode = null)
        {
            return mode ?? Constants.GameMode.Arcade;
        }

        public static Sprite GetBackgroundForMode(Constants.GameMode? mode = null)
        {
            Constants.GameMode resolvedMode = ResolveMode(mode);

            switch (resolvedMode)
            {
                case Constants.GameMode.Campaign:
                    // Placeholder: Normal mode will use selected background ID later.
                    return LoadRandomArcadeBackground();
                case Constants.GameMode.Arcade:
                default:
                    return LoadRandomArcadeBackground();
            }
        }

        public static Sprite LoadBackgroundByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (!BackgroundPaths.TryGetValue(key, out string path))
            {
                Debug.LogWarning($"[BackgroundManager] Unknown background key: {key}");
                return null;
            }

            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[BackgroundManager] Could not load sprite at path: {path}");

            return sprite;
        }

        public static Sprite LoadRandomArcadeBackground()
        {
            if (ArcadeBackgroundKeys.Length == 0)
                return null;

            int index = Random.Range(0, ArcadeBackgroundKeys.Length);
            return LoadBackgroundByKey(ArcadeBackgroundKeys[index]);
        }
    }
}
