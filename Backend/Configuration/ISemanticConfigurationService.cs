namespace Backend.Configuration
{
    /// <summary>
    /// Interface for centralized semantic configuration management
    /// </summary>
    public interface ISemanticConfigurationService
    {
        /// <summary>
        /// Complete semantic keyword configuration combining all mapping files
        /// </summary>
        SemanticKeywordConfig SemanticConfig { get; }

        /// <summary>
        /// Platform aliases for fuzzy matching
        /// </summary>
        Dictionary<string, List<string>> PlatformAliases { get; }
        
        // /// <summary>
        // /// Genre aliases for fuzzy matching
        // /// </summary>
        // Dictionary<string, List<string>> GenreAliases { get; }
        
        // /// <summary>
        // /// Game mode aliases for fuzzy matching
        // /// </summary>
        // Dictionary<string, List<string>> GameModeAliases { get; }
        
        // /// <summary>
        // /// Player perspective aliases for fuzzy matching
        // /// </summary>
        // Dictionary<string, List<string>> PlayerPerspectiveAliases { get; }

        /// <summary>
        /// Indicates if the configuration was loaded successfully
        /// </summary>
        bool IsConfigurationLoaded { get; }

        /// <summary>
        /// Gets the last time the configuration was loaded
        /// </summary>
        DateTime? LastLoadTime { get; }

        /// <summary>
        /// Refresh the configuration from disk
        /// </summary>
        Task RefreshConfigurationAsync();
    }
}