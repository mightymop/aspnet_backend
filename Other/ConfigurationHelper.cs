using AspNetCoreRateLimit;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using fahrtenbuch_service.Other;
using fahrtenbuch_service.Services;

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

        private static void configureAuthorization(WebApplicationBuilder builder, ConfigService config)
        {
            builder.Services.AddSingleton<IAuthorizationHandler>(o => new CustomAuthHandler(config.isAuthEnabled()));

            builder.Services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

           
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CustomAuth", policy =>
                {
                    policy.AddRequirements(new IsEnabledRequirement());
                });
            });

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
                    OnAuthenticationFailed = (context) =>
                    {
                        log.Error("Authentication failed.", context.Exception);
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        log.Error("OnForbidden: " + context.Request + " " + context.Response);
                        return Task.CompletedTask;
                    }
                };

                string metadataurl = config.getAuthMetadata();
                if (Functions.IsWebPageAvailable(metadataurl))
                {
                    x.MetadataAddress = metadataurl;

                    x.Audience = config.getAuthAudience();

                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    x.BackchannelHttpHandler = handler;

                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = config.getAuthAudience(),

                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromMinutes(2),
                        ValidateIssuer = false,
                        RequireSignedTokens = Convert.ToBoolean(config.isAuthValidateSignEnabled()),
                        RequireAudience = Convert.ToBoolean(config.isAuthValidateAudienceEnabled()),
                        // SaveSigninToken = true,
                        TryAllIssuerSigningKeys = config.isAuthValidateSignEnabled(),
                        ValidateActor = false,
                        ValidateAudience = config.isAuthValidateAudienceEnabled(),
                        ValidateIssuerSigningKey = config.isAuthValidateSignEnabled(),
                        ValidateTokenReplay = false
                    };
                }
                else
                {
                    log.Error("Die OpenID Connect Metadaten konnten nicht geladen werden, Token können nicht validiert werden. (URL: " + metadataurl + ")");
                }
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
            configureAuthorization(builder, config);
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
