using Backend.Configuration;

namespace Backend.Services.Recommendation
{
    public class PlatformAliasService
    {
        private readonly ISemanticConfigurationService _configService;
        private readonly ILogger<PlatformAliasService> _logger;

        public PlatformAliasService(ISemanticConfigurationService configService, ILogger<PlatformAliasService> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the canonical platform name for any platform name or alias
        /// </summary>
        public string GetCanonicalPlatformName(string platformName)
        {
            if (string.IsNullOrWhiteSpace(platformName))
                return platformName;

            var trimmed = platformName.Trim();
            var aliases = _configService.PlatformAliases;

            // Find canonical name by checking if this is an alias
            foreach (var (canonical, aliasList) in aliases)
            {
                if (string.Equals(canonical, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    aliasList.Any(alias => string.Equals(alias, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return canonical;
                }
            }

            return trimmed;
        }

        /// <summary>
        /// Gets all known aliases (including canonical name) for a platform
        /// </summary>
        public List<string> GetAllPlatformAliases(string platformName)
        {
            var canonical = GetCanonicalPlatformName(platformName);
            var aliases = _configService.PlatformAliases;

            if (aliases.TryGetValue(canonical, out var aliasList))
            {
                var result = new List<string> { canonical };
                result.AddRange(aliasList);
                return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            return new List<string> { platformName };
        }

        /// <summary>
        /// Normalizes a list of platform names to their canonical forms
        /// </summary>
        public List<string> NormalizePlatformNames(IEnumerable<string> platformNames)
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
        public List<string> ExpandPlatformNamesForSearch(IEnumerable<string> platformNames)
        {
            var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var platform in platformNames.Where(p => !string.IsNullOrWhiteSpace(p)))
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
        public bool AreSamePlatform(string platform1, string platform2)
        {
            if (string.IsNullOrWhiteSpace(platform1) || string.IsNullOrWhiteSpace(platform2))
                return false;

            var canonical1 = GetCanonicalPlatformName(platform1);
            var canonical2 = GetCanonicalPlatformName(platform2);
            
            return string.Equals(canonical1, canonical2, StringComparison.OrdinalIgnoreCase);
        }
    }
}