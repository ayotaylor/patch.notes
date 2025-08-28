using System.Collections.Concurrent;

namespace Backend.Services.Recommendation
{
    public static class PlatformAliasService
    {
        private static readonly ConcurrentDictionary<string, string> _platformAliases = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, HashSet<string>> _canonicalToAliases = new(StringComparer.OrdinalIgnoreCase);
        
        static PlatformAliasService()
        {
            InitializePlatformMappings();
        }

        private static void InitializePlatformMappings()
        {
            var platformMappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // Sony PlayStation
                ["PlayStation 5"] = new[] { "ps5", "playstation5", "sony ps5", "ps 5" },
                ["PlayStation 4"] = new[] { "ps4", "playstation4", "sony ps4", "ps 4" },
                ["PlayStation 3"] = new[] { "ps3", "playstation3", "sony ps3", "ps 3" },
                ["PlayStation 2"] = new[] { "ps2", "playstation2", "sony ps2", "ps 2" },
                ["PlayStation"] = new[] { "ps1", "psx", "playstation1", "sony playstation" },
                ["PlayStation Portable"] = new[] { "psp", "sony psp", "playstation portable" },
                ["PlayStation Vita"] = new[] { "ps vita", "psvita", "vita", "sony vita" },
                
                // Microsoft Xbox
                ["Xbox Series X/S"] = new[] { "xbox series x", "xbox series s", "xsx", "xss", "series x", "series s", "xbox sx", "xbox ss" },
                ["Xbox One"] = new[] { "xone", "xbox 1", "microsoft xbox one", "xb1" },
                ["Xbox 360"] = new[] { "x360", "xbox360", "microsoft xbox 360", "360" },
                ["Xbox"] = new[] { "xbox original", "microsoft xbox", "original xbox" },
                
                // Nintendo
                ["Nintendo Switch"] = new[] { "switch", "nintendo sw", "ns", "nintendo switch oled" },
                ["Nintendo 3DS"] = new[] { "3ds", "nintendo3ds", "new 3ds", "3ds xl", "new 3ds xl" },
                ["Nintendo DS"] = new[] { "ds", "nds", "nintendo ds lite", "dsi" },
                ["Nintendo Wii U"] = new[] { "wii u", "wiiu", "nintendo wiiu" },
                ["Nintendo Wii"] = new[] { "wii", "nintendo wii" },
                ["Nintendo GameCube"] = new[] { "gamecube", "gc", "ngc", "nintendo gc" },
                ["Nintendo 64"] = new[] { "n64", "nintendo64", "nintendo 64" },
                ["Super Nintendo Entertainment System"] = new[] { "snes", "super nintendo", "super nes", "nintendo snes" },
                ["Nintendo Entertainment System"] = new[] { "nes", "nintendo nes", "famicom" },
                ["Game Boy Advance"] = new[] { "gba", "gameboy advance", "game boy advance sp" },
                ["Game Boy Color"] = new[] { "gbc", "gameboy color" },
                ["Game Boy"] = new[] { "gb", "gameboy", "nintendo gameboy" },
                
                // PC Platforms
                ["PC (Microsoft Windows)"] = new[] { "pc", "windows", "microsoft windows", "win", "computer", "steam", "epic games", "gog" },
                ["Mac"] = new[] { "macos", "mac os", "apple mac", "osx", "os x", "macintosh" },
                ["Linux"] = new[] { "ubuntu", "linux pc", "steam os", "steamos" },
                
                // Mobile
                ["Android"] = new[] { "google android", "android mobile", "android phone", "android tablet" },
                ["iOS"] = new[] { "iphone", "ipad", "apple ios", "ios mobile", "ipod touch" },
                
                // Sega
                ["Sega Dreamcast"] = new[] { "dreamcast", "dc", "sega dc" },
                ["Sega Saturn"] = new[] { "saturn", "sega saturn" },
                ["Sega Genesis"] = new[] { "genesis", "mega drive", "sega genesis", "sega mega drive" },
                ["Sega Master System"] = new[] { "master system", "sms", "sega ms" },
                ["Sega Game Gear"] = new[] { "game gear", "gg", "sega gg" },
                
                // Atari
                ["Atari 2600"] = new[] { "atari2600", "atari 2600", "vcs" },
                ["Atari 7800"] = new[] { "atari7800", "atari 7800" },
                ["Atari Jaguar"] = new[] { "jaguar", "atari jaguar" },
                ["Atari Lynx"] = new[] { "lynx", "atari lynx" },
                
                // Other Consoles
                ["Neo Geo"] = new[] { "neogeo", "neo-geo", "snk neo geo" },
                ["3DO"] = new[] { "3do interactive", "panasonic 3do" },
                ["TurboGrafx-16"] = new[] { "turbografx", "tg16", "pc engine" },
                ["Intellivision"] = new[] { "mattel intellivision" },
                ["ColecoVision"] = new[] { "coleco", "colecovision" },
                
                // Arcade and Retro
                ["Arcade"] = new[] { "arcade machine", "coin-op", "cabinet" },
                ["Web Browser"] = new[] { "browser", "web", "html5", "flash", "online" },
                
                // VR Platforms
                ["Oculus Rift"] = new[] { "oculus", "rift", "meta rift", "facebook oculus" },
                ["HTC Vive"] = new[] { "vive", "htc vive", "steamvr" },
                ["PlayStation VR"] = new[] { "psvr", "ps vr", "playstation vr" },
                ["Quest"] = new[] { "oculus quest", "meta quest", "quest 2", "quest 3" }
            };

            // Build both forward and reverse mappings
            foreach (var (canonical, aliases) in platformMappings)
            {
                // Map canonical name to itself
                _platformAliases[canonical] = canonical;
                
                // Store aliases for canonical lookup
                _canonicalToAliases[canonical] = new HashSet<string>(aliases, StringComparer.OrdinalIgnoreCase);
                _canonicalToAliases[canonical].Add(canonical); // Include canonical name itself
                
                // Map each alias to canonical name
                foreach (var alias in aliases)
                {
                    _platformAliases[alias] = canonical;
                }
            }
        }

        /// <summary>
        /// Gets the canonical platform name for any platform name or alias
        /// </summary>
        public static string GetCanonicalPlatformName(string platformName)
        {
            if (string.IsNullOrWhiteSpace(platformName))
                return platformName;

            var trimmed = platformName.Trim();
            return _platformAliases.TryGetValue(trimmed, out var canonical) ? canonical : trimmed;
        }

        /// <summary>
        /// Gets all known aliases (including canonical name) for a platform
        /// </summary>
        public static HashSet<string> GetAllPlatformAliases(string platformName)
        {
            var canonical = GetCanonicalPlatformName(platformName);
            return _canonicalToAliases.TryGetValue(canonical, out var aliases) 
                ? new HashSet<string>(aliases, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string> { platformName };
        }

        /// <summary>
        /// Normalizes a list of platform names to their canonical forms
        /// </summary>
        public static List<string> NormalizePlatformNames(IEnumerable<string> platformNames)
        {
            return platformNames
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(GetCanonicalPlatformName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Expands platform names to include common aliases for better matching
        /// </summary>
        public static List<string> ExpandPlatformNamesForSearch(IEnumerable<string> platformNames)
        {
            var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var platform in platformNames)
            {
                var aliases = GetAllPlatformAliases(platform);
                foreach (var alias in aliases)
                {
                    expanded.Add(alias);
                }
            }
            
            return expanded.ToList();
        }

        /// <summary>
        /// Checks if two platform names refer to the same platform (considering aliases)
        /// </summary>
        public static bool AreSamePlatform(string platform1, string platform2)
        {
            if (string.IsNullOrWhiteSpace(platform1) || string.IsNullOrWhiteSpace(platform2))
                return false;

            var canonical1 = GetCanonicalPlatformName(platform1);
            var canonical2 = GetCanonicalPlatformName(platform2);
            
            return string.Equals(canonical1, canonical2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets platform-specific keywords for semantic embedding
        /// </summary>
        public static List<string> GetPlatformSemanticKeywords(string platformName)
        {
            var canonical = GetCanonicalPlatformName(platformName);
            var keywords = new List<string>();

            // Add base platform info
            keywords.AddRange(GetAllPlatformAliases(canonical));

            // Add platform-specific semantic keywords
            switch (canonical.ToLowerInvariant())
            {
                case var p when p.Contains("playstation"):
                    keywords.AddRange(new[] { "sony", "console", "controller", "exclusive", "cinematic", "blu-ray" });
                    break;
                case var p when p.Contains("xbox"):
                    keywords.AddRange(new[] { "microsoft", "console", "gamepass", "controller", "backwards-compatible" });
                    break;
                case var p when p.Contains("nintendo"):
                    keywords.AddRange(new[] { "nintendo", "family-friendly", "portable", "first-party", "innovative" });
                    break;
                case var p when p.Contains("pc") || p.Contains("windows"):
                    keywords.AddRange(new[] { "pc", "computer", "mods", "high-performance", "customizable", "steam", "multiple-stores" });
                    break;
                case var p when p.Contains("mobile") || p.Contains("android") || p.Contains("ios"):
                    keywords.AddRange(new[] { "mobile", "portable", "touchscreen", "casual", "free-to-play" });
                    break;
                case var p when p.Contains("vr"):
                    keywords.AddRange(new[] { "virtual-reality", "immersive", "3d", "motion-controls", "experimental" });
                    break;
            }

            return keywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}