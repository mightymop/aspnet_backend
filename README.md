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
