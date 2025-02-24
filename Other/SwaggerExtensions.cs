﻿using fahrtenbuch_service.Services;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Utils.Other
{
    public static class SwaggerExtensions
    {
        public static void ConfigureSwaggerBuilder(WebApplicationBuilder builder, ConfigService config)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(config.get("api:version"), new OpenApiInfo { Title = config.get("api:name"), Version = config.get("api:version")});
                c.AddOauth2AuthSchemaSecurityDefinitions(config);
            }).AddSwaggerGenNewtonsoftSupport();
        }

        public static void ConfigureSwaggerApp(WebApplication app, ConfigService config)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
                c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/"+ config.get("api:version") +"/swagger.json", config.get("api:name") + " " + config.get("api:version"));

                //oauth2

                c.OAuthClientId(config.get("auth:clientid"));
                c.OAuthUsePkce();
                c.OAuthAppName(config.get("api:name"));
                c.OAuthScopeSeparator(" ");
                c.OAuthUseBasicAuthenticationWithAccessCodeGrant();

            });
            
        }

        public static SwaggerGenOptions AddOauth2AuthSchemaSecurityDefinitions(this SwaggerGenOptions options, ConfigService config)
        {
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "OAuth2.0 Auth Code with PKCE",
                Name = "oauth2",
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri(config.getAuthorizeUrl()), 
                        TokenUrl = new Uri(config.getTokenUrl()),
                        Scopes = new Dictionary<string, string>
                        {
                             { "openid", "Use Openid Connect" }
                        }
                    }
                }
            }) ;

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                    Enter 'Bearer' [space] and then your token in the text input below.
                    \r\n\r\nExample: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        },
                        Scheme = "oauth2",
                        Name = "oauth2",
                        In = ParameterLocation.Header
                    },
                    new List < string > ()
                }
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                                                {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "Bearer",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });

            return options;
        }

    }
}
