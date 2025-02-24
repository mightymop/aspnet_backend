using fahrtenbuch_service.Services;
using log4net.Config;
using Microsoft.IdentityModel.Logging;
using Utils.Other;
using ConfigurationManager = Microsoft.Extensions.Configuration.ConfigurationManager;

//Infos zur Konfiguration in README.md

XmlConfigurator.Configure(new FileInfo("log4net.config"));

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager cfgmgr = builder.Configuration;
ConfigService config = new ConfigService(cfgmgr);
IdentityModelEventSource.ShowPII = true;

SwaggerExtensions.ConfigureSwaggerBuilder(builder, config);
ConfigurationHelper.configureBuilder(builder, cfgmgr, config);

var app = builder.Build();

SwaggerExtensions.ConfigureSwaggerApp(app, config);
ConfigurationHelper.configureApp(app);

app.Run();
