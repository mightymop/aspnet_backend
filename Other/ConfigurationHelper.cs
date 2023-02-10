using AspNetCoreRateLimit;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Net;

namespace Utils.Other
{
    public static class ConfigurationHelper
    {
        private static ILog log = LogManager.GetLogger(typeof(ConfigurationHelper));

        private static void configureCors(WebApplicationBuilder builder, ConfigurationManager cfgmgr)
        {
            /*
                Hier wird Cors konfiguriert. Zugriff von Clients auf andere Server als derer woher der Clientcode geladen wurde.
                Sonst brechen die Browser ab.
             */
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("*", "https://localhost/", cfgmgr["auth:authority"]);
                    policy.SetIsOriginAllowed(origin => true);
                    policy.AllowAnyOrigin();
                    policy.AllowAnyHeader();
                    policy.WithMethods("GET", "POST", "OPTIONS", "DELETE", "PUT", "PATCH", "HEAD");
                    // policy.AllowAnyMethod();
                    policy.SetIsOriginAllowedToAllowWildcardSubdomains();
                    policy.SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                });
            });
        }

        private static void configureAuthorization(WebApplicationBuilder builder, ConfigurationManager cfgmgr)
        {
            //Authhandler als Service starten.
            builder.Services.AddSingleton<IAuthorizationHandler>(o => new CustomAuthHandler(cfgmgr["auth:enabled"] != null ? !cfgmgr["auth:enabled"].Equals("true") : true));

            builder.Services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            // configures IIS in-proc settings
            builder.Services.Configure<IISServerOptions>(iis =>
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
                options.AddPolicy("CustomAuth", policy => {
                    policy.AddRequirements(new IsEnabledRequirement());
                });
            });
            
            //Prüfung der Token von Client beim Zugriff auf die API-Endpunkte
            builder.Services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                string metadataurl = cfgmgr["auth:metadata"];
                if (IsWebPageAvailable(metadataurl))
                {
                    x.MetadataAddress = cfgmgr["auth:metadata"]; //.../.well-known/openid-configuration URL aus config ... Die Datei enthält Infos für die Auth. der Token

                    x.Audience = cfgmgr["auth:clientid"]; // microsoft:identityserver:<CLIENT ID FÜR APP IM ADFS>
                    x.Authority = cfgmgr["auth:authority"]; // Url / Referer unter dem der Client läuft

                    // UM SSL Zertifikatstest der Backends untereinander zu deaktivieren... sonst müsste man hier valide SSL Zertifikate installieren etc...
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    x.BackchannelHttpHandler = handler;

                    //Einstellung welche Dinge im Token geprüft werden:
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = cfgmgr["auth:audience"],

                        ValidIssuer = cfgmgr["auth:trusturl"],
                        ValidateLifetime = true,

                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        RequireAudience = true,
                        SaveSigninToken = true,
                        TryAllIssuerSigningKeys = true,
                        ValidateActor = false,
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
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

        public static bool IsWebPageAvailable(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 5000;
                request.AllowAutoRedirect = false;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception)
            {
                return false;
            }
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

        public static void configureBuilder(WebApplicationBuilder builder, ConfigurationManager cfgmgr)
        {

            configureCors(builder, cfgmgr);
            configureAuthorization(builder, cfgmgr);
            configureRateLimit(builder, cfgmgr);

            //builder.Services.AddControllers();
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

            app.UseAuthorization();
            app.UseAuthentication();
            app.UseCors();

            app.MapControllers();
        }
    }

    //Nötig, nicht löschen!
    public class IsEnabledRequirement : IAuthorizationRequirement
    {
    }

    


    /*
       Dient der Möglichkeit die Authorisierung per Konfigurationsdatei zu (de-)aktivieren.
    */
    public class CustomAuthHandler : AuthorizationHandler<IsEnabledRequirement>
    {

        private bool _disabled;
        public CustomAuthHandler(bool authDisabled)
        {
            this._disabled = authDisabled; 
        }

        public override Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (this._disabled)
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
            if (this._disabled)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
