using JetBrains.Annotations;
using Pustalorc.Libraries.DbConnectionWrapper;
using Pustalorc.MySqlDatabaseWrapper.Configuration;

namespace Pustalorc.MySqlDatabaseWrapper.Abstractions;

/// <inheritdoc />
/// <summary>
///     A base class that adds minimal support for configurations for MySql connection wrapping.
/// </summary>
/// <typeparam name="TMySqlConfiguration">
///     The type for the mysql configuration. Must inherit from
///     <see cref="IMySqlConfiguration" />.
/// </typeparam>
public abstract class MySqlConnectionWrapper<TMySqlConfiguration> : DatabaseConnectionWrapper
    where TMySqlConfiguration : IMySqlConfiguration
{
    /// <summary>
    ///     The instance of the configuration.
    /// </summary>
    protected TMySqlConfiguration Configuration { get; private set; }

    /// <summary>
    ///     Instantiates a new wrapper with the specified configuration.
    /// </summary>
    /// <param name="configuration">The instance of the configuration</param>
    protected MySqlConnectionWrapper(TMySqlConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    ///     Reloads the configuration in memory, so that the connection opens with a different set of values.
    /// </summary>
    /// <param name="configuration">The instance of the configuration.</param>
    [UsedImplicitly]
    public virtual void ReloadConfiguration(TMySqlConfiguration configuration)
    {
        Configuration = configuration;
    }
}