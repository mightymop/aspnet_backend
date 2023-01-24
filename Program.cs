using log4net.Config;
using Microsoft.IdentityModel.Logging;
using System.Configuration;
using Utils.Other;
using ConfigurationManager = Microsoft.Extensions.Configuration.ConfigurationManager;

//Infos zur Konfiguration in README.md

XmlConfigurator.Configure(new FileInfo("log4net.config"));

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager cfgmgr = builder.Configuration;
//Für Zugriff in Controller
builder.Services.AddSingleton<ConfigurationManager>(cfgmgr);

IdentityModelEventSource.ShowPII = true;

SwaggerExtensions.ConfigureSwaggerBuilder(builder, cfgmgr);
ConfigurationHelper.configureBuilder(builder, cfgmgr);

var app = builder.Build();

SwaggerExtensions.ConfigureSwaggerApp(app, cfgmgr);
ConfigurationHelper.configureApp(app);

app.Run();