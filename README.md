# Backend

## Use
### Starten
Starten der Anwendung in Vistual Studio über die Datei `aspauthtest.sln`.

### Veröffentlichen
Nach erfolgreichem Start kann das Projekt per <kdb>rechts klick</kdb> auf `backend/Veröffentlichen...` unter `bin\release\net6.0\publish` veröffentlicht werden.

### map-srvadmi
Über den `map-srvadmi-1-v` kann eine Verbinudng zu bspw. dem `map-polosk-1-v`/`map-polosk-2-v` aufgebaut werden.

### map-polosk-1-v/map-polosk-2-v
Auf den redundanten Servern ist jeweils unter `C:\inetpub\wwwroot`\`<backendname>` das Projekt zu hinterlegen.

### IIS Manager
Per Server Manager kann unter `Tools` in den IIS Manager gewechselt werden. Dort sind die Site und der Application Pool anzupassen.

#### Application Pools
Die Anwendung ist unter den `Application Pools` mittels `Add Application Pool...` zu hinterlegen.
```json
{
    "name"                              : "test_server",
    ".Net CLR version"                  : "Not managed",
    "Managed pileline mode"             : "integrated",
    "start application pool imediatly"  : true
}
```

#### Sites (Default)
Die Anwendung ist unter den `Default Web Sites` mittels `Add Application` zu hinterlegen
```json
    "Alias": "test_service",
    "Physical path": "C:\inetpub\wwwroot\test_service"
```

### Datenbank

## Swagger
`https://polosk-lb.int.polizei.berlin.de/test_service/swagger/index.html`


## Ramons

<pre>
 {
  "api": {   
    "version": "v1",        //Version der API    
    "name": "Backend API"   //Name der API
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,  //(de)aktiviert API-Endpunkt-ratepunktlimiting
    "StackBlockedRequests": false,       
    "RealIPHeader": "X-Real-IP",        
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,              // HTTP Response Statuscode bei geblocktem Request
    "GeneralRules": [
      {
        "Endpoint": "*/test/info",       //Endpunkt
        "Period": "5s",                 //Zeitraum der Überwachung
        "Limit": 50                     //max. Zugriffe innerhalb des Zeitraums
      }
    ]
  },
  "auth": {
    "enabled": "true",                  //(de)aktiviert die Tokenprüfung am Endpunkt
    "metadata": "https://dc2019.poldom.local/adfs/.well-known/openid-configuration",    //Info vom AUthProvider, nötig für Validierung
    "authorizeurl": "https://dc2019.poldom.local/adfs/oauth2/authorize",    //nötig für Swagger für Login am AuthProvider
    "tokenurl": "https://dc2019.poldom.local/adfs/oauth2/token",        //Url zur Prüfung der Token
    "trusturl": "http://dc2019.poldom.local/adfs/services/trust",       //Url zur Prüfung der Token
    "clientid": "633d0b2d-a45e-4e9a-9288-64344f5d19fc",                 //Client-ID unter welcher die App beim AuthProvider registriert wurde
    "audience": "microsoft:identityserver:633d0b2d-a45e-4e9a-9288-64344f5d19fc", //hier steht normalerweise die Client-ID, beim ADFS in Kombination mit microsoft:identityserver:<CLIENT-ID>
    "validate_audience": "true",
    "validate_sign": "false"
  }
}
<pre>
