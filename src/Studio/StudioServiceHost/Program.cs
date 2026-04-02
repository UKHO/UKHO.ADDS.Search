namespace StudioServiceHost
{
    /// <summary>
    /// Provides the executable entry point for the Studio service host process.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Builds and runs the Studio service host.
        /// </summary>
        /// <param name="args">The command-line arguments supplied to the host process.</param>
        public static void Main(string[] args)
        {
            // Build the application using the shared host bootstrap so all service registrations stay centralized.
            var app = StudioServiceHostApplication.BuildApp(args);

            // Start handling requests once the host has been fully configured.
            app.Run();
        }
    }
}
