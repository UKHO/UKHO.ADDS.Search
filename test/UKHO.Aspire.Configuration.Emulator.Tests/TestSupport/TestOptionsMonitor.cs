using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace UKHO.Aspire.Configuration.Emulator.Tests.TestSupport
{
    /// <summary>
    /// Provides a stable <see cref="IOptionsMonitor{TOptions}"/> implementation for unit tests that need deterministic option values.
    /// </summary>
    /// <typeparam name="TOptions">The options type exposed through the monitor.</typeparam>
    internal sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        private readonly TOptions _currentValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOptionsMonitor{TOptions}"/> class.
        /// </summary>
        /// <param name="currentValue">The fixed options instance returned for every lookup.</param>
        public TestOptionsMonitor(TOptions currentValue)
        {
            // Store the supplied options so every call observes the same deterministic values.
            _currentValue = currentValue;
        }

        /// <summary>
        /// Gets the current options value.
        /// </summary>
        public TOptions CurrentValue
        {
            get
            {
                // Return the preconfigured options instance because test scenarios do not mutate options at runtime.
                return _currentValue;
            }
        }

        /// <summary>
        /// Gets the named options value.
        /// </summary>
        /// <param name="name">The requested options name.</param>
        /// <returns>The fixed options instance supplied at construction time.</returns>
        public TOptions Get(string? name)
        {
            // Ignore the name because the tests only need a stable options snapshot.
            return _currentValue;
        }

        /// <summary>
        /// Registers a change callback.
        /// </summary>
        /// <param name="listener">The listener to invoke when options change.</param>
        /// <returns>An <see cref="IDisposable"/> that represents the no-op registration.</returns>
        public IDisposable OnChange(Action<TOptions, string?> listener)
        {
            // Return a disposable no-op registration because the monitor never raises change notifications in unit tests.
            return ChangeToken.OnChange(
                () => new CancellationChangeToken(CancellationToken.None),
                () => { });
        }
    }
}
