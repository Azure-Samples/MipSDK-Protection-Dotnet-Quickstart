using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.InformationProtection.Protection;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using System.Windows.Forms;
using System.Collections;

namespace mipsdk_dotnet_protection_quickstart
{
    public class Action : IDisposable
    {
        private AuthDelegateImplementation authDelegate;
        private ApplicationInfo appInfo;
        private IProtectionProfile profile;
        private IProtectionEngine engine;
        private MipContext mipContext;

        public Action(ApplicationInfo appInfo) 
        {
            this.appInfo = appInfo;

            // Initialize AuthDelegateImplementation using AppInfo. 
            authDelegate = new AuthDelegateImplementation(this.appInfo);

            // Initialize SDK DLLs. If DLLs are missing or wrong type, this will throw an exception
            MIP.Initialize(MipComponent.Protection);

            // Create MipConfiguration Object
            MipConfiguration mipConfiguration = new MipConfiguration(appInfo, "mip_data", LogLevel.Trace, false);

            // Create MipContext using MipConfiguration
            mipContext = MIP.CreateMipContext(mipConfiguration);

            // This method in AuthDelegateImplementation triggers auth against Graph so that we can get the user ID.
            var id = authDelegate.GetUserIdentity();

            // Create profile
            profile = CreateProtectionProfile(appInfo, ref authDelegate);

            // Create engine by providing Idenity from authDelegate to assist with service discovery.
            engine = CreateProtectionEngine(id);
        }

        /// <summary>
        /// Unload engine, null refs to engine and profile and release all MIP resources.
        /// </summary>        
        public void Dispose()
        {            
            //profile.UnloadEngineAsync(engine.Settings.EngineId).Wait();
            engine.Dispose();
            profile.Dispose();
            mipContext.ShutDown();
            mipContext.Dispose();
        }

        private IProtectionProfile CreateProtectionProfile(ApplicationInfo appInfo, ref AuthDelegateImplementation authDelegate)
        {
            // Initialize ProtectionProfileSettings
            var profileSettings = new ProtectionProfileSettings(mipContext, 
                CacheStorageType.OnDisk, 
                new ConsentDelegateImplementation());

            // Use MIP.LoadProtectionProfileAsync() providing settings to create IProtectionProfile
            // IProtectionProfile is the root of all SDK operations for a given application
            var profile = MIP.LoadProtectionProfile(profileSettings);

            return profile;
        }

        // Create a protection engine
        private IProtectionEngine CreateProtectionEngine(Identity identity)
        {
            if (profile == null)
            {
                profile = CreateProtectionProfile(appInfo, ref authDelegate);
            }

            // Create protection engine settings object. Passing in empty string for the first parameter, engine ID, will cause the SDK to generate a GUID.
            // Passing in a email address or other unique value helps to ensure that the cached engine is loaded each time for the same user.
            // Locale settings are supported and should be provided based on the machine locale, particular for client applications.
            var engineSettings = new ProtectionEngineSettings(identity.Email, authDelegate, "", "")
            {
                Identity = identity
            };

            var engine = profile.AddEngine(engineSettings);

            return engine;
        }

        public List<TemplateDescriptor> ListTemplates()
        {
            return engine.GetTemplates();
        }

        // Create a handler for publishing. 
        public IProtectionHandler CreatePublishingHandler(string templateId)
        {
            ProtectionDescriptor protectionDescriptor = new ProtectionDescriptor(templateId);
            PublishingSettings publishingSettings = new PublishingSettings(protectionDescriptor);

            var protectionHandler = engine.CreateProtectionHandlerForPublishing(publishingSettings);
            return protectionHandler;
        }

        // Create a handler for consumption from the publishing license.
        public IProtectionHandler CreateConsumptionHandler(List<byte> serializedPublishingLicense)
        {
            PublishingLicenseInfo plInfo = PublishingLicenseInfo.GetPublishingLicenseInfo(serializedPublishingLicense, mipContext);

            ConsumptionSettings consumptionSettings = new ConsumptionSettings(plInfo)
            {
                // This is a new required field for tracking content for Track and Revoke. 
                ContentName = "A few bytes."
            };

            var protectionHandler = engine.CreateProtectionHandlerForConsumption(consumptionSettings);
            return protectionHandler;
        }

        // Protect the input bytes. 
        public byte[] Protect(IProtectionHandler handler, byte[] data)
        {
            long buffersize = handler.GetProtectedContentLength(data.Length, true);
            byte[] outputBuffer = new byte[buffersize];
            
            handler.EncryptBuffer(0, data, outputBuffer, true);
            return outputBuffer;
        }

        public byte[] Unprotect(IProtectionHandler handler, byte[] data)
        {
            long buffersize = data.Length;
            byte[] clearBuffer = new byte[buffersize];
            
            var bytesDecrypted = handler.DecryptBuffer(0, data, clearBuffer, true);
            
            byte[] outputBuffer = new byte[bytesDecrypted];
            for(int i = 0; i < bytesDecrypted; i++)
            {
                outputBuffer[i] = clearBuffer[i];
            }

            return outputBuffer;
        }

    }
}
