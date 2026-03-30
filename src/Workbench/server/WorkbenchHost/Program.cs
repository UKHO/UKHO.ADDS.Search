using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Radzen;
using UKHO.Search.ServiceDefaults;
using UKHO.Workbench.Infrastructure;
using WorkbenchHost.Components;
using WorkbenchHost.Extensions;

namespace WorkbenchHost
{
    /// <summary>
    ///     Hosts the Workbench Blazor shell and configures its supporting infrastructure.
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     Builds and runs the Workbench host application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the host process.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Apply the shared service-default configuration used across the repository's hosts.
            builder.AddServiceDefaults();

            // Register the workbench infrastructure services required by the host.
            builder.Services.AddWorkbenchInfrastructure();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                   .AddInteractiveServerComponents()
                   .AddHubOptions(options => options.MaximumReceiveMessageSize = 10 * 1024 * 1024);

            // Register Radzen services so the host can render the shell using Radzen assets.
            builder.Services.AddRadzenComponents();

            builder.Services.AddRadzenCookieThemeService(options =>
            {
                options.Name = "workbench-theme";
                options.Duration = TimeSpan.FromDays(365);
            });

            builder.Services.AddHttpClient();

            // Provide the current HTTP context and authorization helpers required by the host.
            builder.Services.AddHttpContextAccessor().AddTransient<AuthorizationHandler>();

            // Normalize realm-role claims before authorization runs against the current principal.
            builder.Services.AddTransient<IClaimsTransformation, KeycloakRealmRoleClaimsTransformation>();

            var oidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

            // Configure cookie-backed OpenID Connect authentication against the shared Keycloak realm.
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = oidcScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddKeycloakOpenIdConnect("keycloak", "ukho-search", oidcScheme, options =>
                {
                    options.ClientId = "search-workbench";
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                });

            // Flow the authenticated user through the Blazor component tree.
            builder.Services.AddCascadingAuthenticationState();

            // Require authenticated users by default for the workbench surface.
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var app = builder.Build();

            var forwardingOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            forwardingOptions.KnownIPNetworks.Clear();
            forwardingOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardingOptions);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found");
            app.UseHttpsRedirection();
            app.UseAntiforgery();


            app.MapStaticAssets();
            app.UseAntiforgery();

            // Expose login and logout endpoints before the authenticated UI pipeline executes.
            app.MapLoginAndLogout();

            // Authenticate requests before enforcing the fallback authorization policy.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}