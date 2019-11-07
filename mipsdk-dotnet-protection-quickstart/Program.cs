using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.InformationProtection;
using System.Collections;
using System.ComponentModel;

namespace mipsdk_dotnet_protection_quickstart
{
    class Program
    {
        private static readonly string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static readonly string appName = ConfigurationManager.AppSettings["app:Name"];
        private static readonly string appVersion = ConfigurationManager.AppSettings["app:Version"];
        
        static void Main(string[] args)
        {
            // Create ApplicationInfo, setting the clientID from Azure AD App Registration as the ApplicationId
            // If any of these values are not set API throws BadInputException.
            ApplicationInfo appInfo = new ApplicationInfo()
            {
                // ApplicationId should ideally be set to the same ClientId found in the Azure AD App Registration.
                // This ensures that the clientID in AAD matches the AppId reported in AIP Analytics.
                ApplicationId = clientId,
                ApplicationName = appName,
                ApplicationVersion = appVersion
            };

            // Initialize Action class, passing in AppInfo.
            Action action = new Action(appInfo);

            var templates = action.ListTemplates();
            
            for(int i = 0; i < templates.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i.ToString(), templates[i]);
            }

            Console.WriteLine("");
            Console.WriteLine("Select a template: ");
            var selectedTemplate = Console.ReadLine();

            var publishHandler = action.CreatePublishingHandler(selectedTemplate);

            Console.WriteLine("Enter some string to protect: ");
            var userInputString = Console.ReadLine();
            var userInputBytes = Encoding.ASCII.GetBytes(userInputString);

            var encryptedBytes = action.Protect(publishHandler, userInputBytes);
            Console.WriteLine("");
            Console.WriteLine(Encoding.ASCII.GetString(encryptedBytes));

            Console.WriteLine("");

            var serializedPublishingLicense = publishHandler.GetSerializedPublishingLicense();

            var consumeHandler = action.CreateConsumptionHandler(serializedPublishingLicense);

            var decryptedBytes = action.Unprotect(consumeHandler, encryptedBytes);

            Console.WriteLine("Decrypted content: {0}", Encoding.ASCII.GetString(decryptedBytes));

            Console.WriteLine("Press a key to quit.");
            Console.ReadKey();
        }
    }
}
