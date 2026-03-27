using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using UKHO.Search.ServiceDefaults;
using UKHO.Workbench.Infrastructure;
using WorkbenchHost.Components;
using WorkbenchHost.Extensions;

namespace WorkbenchHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.Services.AddWorkbenchInfrastructure();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpContextAccessor().AddTransient<AuthorizationHandler>();

            builder.Services.AddTransient<IClaimsTransformation, KeycloakRealmRoleClaimsTransformation>();

            var oidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

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

            builder.Services.AddCascadingAuthenticationState();

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var app = builder.Build();

            app.MapDefaultEndpoints();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAntiforgery();

            app.MapLoginAndLogout();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}