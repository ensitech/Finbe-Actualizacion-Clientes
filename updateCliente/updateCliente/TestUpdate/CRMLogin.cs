using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk.Client;
using System.Net;

namespace updateCliente
{
    class CRMLogin
    {
        public static IOrganizationService createService()
        {
            //String url = "http://srvbbcrm11des1.mdaote.bepensa.local:5555/Financiera/XRMServices/2011/Organization.svc";
            String url = "https://appscrm13.bepensa.com:445/Financiera/XRMServices/2011/Organization.svc";
           // String url = "https://appsfb.bepensa.com:5000/FinancieraPreProd/XRMServices/2011/Organization.svc";
            String user = "jcenm";
            //"admin_crmdes";
            String domain = "adpeco";
            String password = "elviaje97";
                //"4dmin3110";

            return createService(url, user, domain, password);
        }

        private static IOrganizationService createService(String url, String user, String domain, String password)
        {
            OrganizationServiceProxy serviceProxy = null;
            IServiceConfiguration<IOrganizationService> config = ServiceConfigurationFactory.CreateConfiguration<IOrganizationService>(new Uri(url));

            switch (config.AuthenticationType)
            {
                case AuthenticationProviderType.Federation:
                    ClientCredentials clientCredentials = new ClientCredentials();
                    clientCredentials.UserName.UserName = domain + "\\" + user;
                    clientCredentials.UserName.Password = password;
                    serviceProxy = new OrganizationServiceProxy(config, clientCredentials);
                    break;
                case AuthenticationProviderType.ActiveDirectory:
                    ClientCredentials credentials = new ClientCredentials();
                    credentials.Windows.ClientCredential = new NetworkCredential(user, password, domain);
                    serviceProxy = new OrganizationServiceProxy(new Uri(url), null, credentials, null);
                    break;
            }

            if (serviceProxy.IsAuthenticated)
                Console.WriteLine(serviceProxy.IsAuthenticated);

            return serviceProxy;
        }
    }
}
