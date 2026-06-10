using AspNetCoreRateLimit;
using fahrtenbuch_service.Other;
using fahrtenbuch_service.Services;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text.Json;

namespace Utils.Other
{
    public static class ConfigurationHelper
    {
        private static ILog log = LogManager.GetLogger(typeof(ConfigurationHelper));
        private static void configureCors(WebApplicationBuilder builder, ConfigService config)
        {
            var allowedOrigins = config.getCorsOrigins() ?? new string[] { "*" };

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .WithMethods("GET", "POST", "OPTIONS", "DELETE", "PUT", "PATCH", "HEAD")
                     .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                    // Falls Credentials verwendet werden, darfst du keine AllowAnyOrigin verwenden
                    // und musst zudem AllowCredentials explizit setzen:
                    policy.SetIsOriginAllowedToAllowWildcardSubdomains();
                    if (allowedOrigins[0].Equals("*") == false)
                    {
                        policy.AllowCredentials();
                    }
                    else
                    {
                        policy.AllowAnyOrigin();
                    }
                });
            });
        }

        private static void configureAuthorization(WebApplicationBuilder builder, ConfigurationManager cfgmgr)
        {
            builder.Services.AddSingleton<IAuthorizationHandler>(
                o => new CustomAuthHandler(cfgmgr.GetSection("auth").GetSection("enabled").Get<bool>()));

            builder.Services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()))
                .SetApplicationName(
                    Assembly.GetEntryAssembly()?.GetName().Name ?? "DefaultAppName");

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CustomAuth", policy =>
                {
                    policy.AddRequirements(new IsEnabledRequirement());
                });
            });

            // ====================================================
            // Metadata VOR AddJwtBearer laden
            // ====================================================

            var metadataUrls = cfgmgr.GetSection("auth").GetSection("metadata").Get<string[]>();

            List<string> validIssuers = new();
            List<SecurityKey> signingKeys = new();

            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using HttpClient httpClient = new HttpClient(handler);

            foreach (var metadataUrl in metadataUrls)
            {
                try
                {
                    if (!Functions.IsWebPageAvailable(metadataUrl))
                    {
                        log.Error($"Metadata nicht erreichbar: {metadataUrl}");
                        continue;
                    }

                    // issuer direkt aus JSON lesen
                    string metadataJson =
                        httpClient.GetStringAsync(metadataUrl)
                            .GetAwaiter()
                            .GetResult();

                    using JsonDocument doc = JsonDocument.Parse(metadataJson);

                    if (doc.RootElement.TryGetProperty("issuer", out var issuerProperty))
                    {
                        string issuer = issuerProperty.GetString();

                        if (!string.IsNullOrWhiteSpace(issuer))
                        {
                            validIssuers.Add(issuer);

                            log.Info(
                                $"Issuer aus OpenID Metadata geladen: {issuer}");
                        }
                    }
                    else
                    {
                        log.Error(
                            $"Metadata enthält kein issuer-Feld: {metadataUrl}");
                    }

                    if (doc.RootElement.TryGetProperty("access_token_issuer", out var access_token_issuerProperty))
                    {
                        string access_token_issuer = access_token_issuerProperty.GetString();

                        if (!string.IsNullOrWhiteSpace(access_token_issuer))
                        {
                            validIssuers.Add(access_token_issuer);

                            log.Info(
                                $"Access_token_issuer aus OpenID Metadata geladen: {access_token_issuer}");
                        }
                    }
                    else
                    {
                        log.Error(
                            $"Metadata enthält kein access_token_issuer-Feld: {metadataUrl}");
                    }

                    // Keys weiterhin über OIDC laden
                    var manager =
                        new Microsoft.IdentityModel.Protocols.ConfigurationManager<
                            Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
                            metadataUrl,
                            new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever()
                        );

                    var oidcConfig =
                        manager.GetConfigurationAsync()
                            .GetAwaiter()
                            .GetResult();

                    foreach (var key in oidcConfig.SigningKeys)
                    {
                        signingKeys.Add(key);
                    }

                    log.Info($"OIDC Issuer Property: {oidcConfig.Issuer}");
                    log.Info($"Signing Keys geladen: {oidcConfig.SigningKeys.Count}");
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Fehler beim Laden der Metadata: {metadataUrl}",
                        ex);
                }
            }

            log.Info("ValidIssuers:");

            foreach (var issuer in validIssuers)
            {
                log.Info($"  {issuer}");
            }

            // ====================================================
            // Authentication
            // ====================================================

            builder.Services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                sharedOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = context =>
                    {
                        log.Error("Authentication failed.", context.Exception);
                        return Task.CompletedTask;
                    },

                    OnForbidden = context =>
                    {
                        log.Error(
                            "OnForbidden: " +
                            context.Request +
                            " " +
                            context.Response);

                        return Task.CompletedTask;
                    }
                };

                x.BackchannelHttpHandler = handler;
                x.RequireHttpsMetadata = false;

                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuers = validIssuers,

                    IssuerSigningKeys = signingKeys,

                    ValidAudiences = cfgmgr.GetSection("auth:audience").Get<string[]>(),

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),

                    ValidateIssuer = cfgmgr.GetSection("auth").GetSection("validate_issuer").Get<bool>(),

                    RequireSignedTokens = cfgmgr.GetSection("auth").GetSection("validate_sign").Get<bool>(),
                    RequireAudience = cfgmgr.GetSection("auth").GetSection("validate_audience").Get<bool>(),
                    ValidateAudience = cfgmgr.GetSection("auth").GetSection("validate_audience").Get<bool>(),
                    ValidateIssuerSigningKey = cfgmgr.GetSection("auth").GetSection("validate_sign").Get<bool>(),

                    TryAllIssuerSigningKeys = cfgmgr.GetSection("auth").GetSection("validate_sign").Get<bool>(),

                    ValidateActor = false,
                    ValidateTokenReplay = false
                };
            })
            .AddCookie();
        }

        public static void configureRateLimit(WebApplicationBuilder builder, ConfigurationManager cfgmgr)
        {
            builder.Services.AddMemoryCache();
            builder.Services.Configure<IpRateLimitOptions>(cfgmgr.GetSection("IpRateLimiting"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            builder.Services.AddSingleton<IRateLimitConfiguration, CustomRateLimitConfiguration>();

        }

        public static void configureBuilder(WebApplicationBuilder builder, ConfigurationManager cfgmgr, ConfigService config)
        {
            builder.Services.AddSingleton<ConfigService>(o => config);
            builder.Services.AddSingleton<DatabaseService>(o => new DatabaseService(config));

            configureCors(builder, config);
            configureAuthorization(builder, cfgmgr);
            configureRateLimit(builder, cfgmgr);

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(1);
            });

            builder.Services.AddControllersWithViews().AddXmlSerializerFormatters();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddRazorPages();
        }

        public static void configureApp(WebApplication app)
        {
            app.UseMiddleware<CustomIpRateLimitMiddleware>();

            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCookiePolicy();

            app.UseSession();
            app.UseAuthorization();
            app.UseAuthentication();
            app.UseCors();

            app.MapControllers();
        }
    }

    public class IsEnabledRequirement : IAuthorizationRequirement
    {
    }

    public class CustomAuthHandler : AuthorizationHandler<IsEnabledRequirement>
    {

        private bool _enabled;
        public CustomAuthHandler(bool authEnabled)
        {
            this._enabled = authEnabled;
        }

        public override Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (!this._enabled)
            {
                foreach (IAuthorizationRequirement itm in context.Requirements)
                {
                    context.Succeed(itm);
                }
                return Task.CompletedTask;
            }

            return base.HandleAsync(context);
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       IsEnabledRequirement requirement)
        {
            if (!this._enabled)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
