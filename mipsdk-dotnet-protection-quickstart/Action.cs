﻿using System;
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
    public class Action
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

            // This method in AuthDelegateImplementation triggers auth against Graph so that we can get the user ID.
            var id = authDelegate.GetUserIdentity();

            // Create profile
            profile = CreateProtectionProfile(appInfo, ref authDelegate);

            // Create engine by providing Idenity from authDelegate to assist with service discovery.
            engine = CreateProtectionEngine(id);
        }

        private IProtectionProfile CreateProtectionProfile(ApplicationInfo appInfo, ref AuthDelegateImplementation authDelegate)
        {
            // Initialize MipContext
            mipContext = MIP.CreateMipContext(appInfo, "mip_data", LogLevel.Trace, null, null);

            // Initialize ProtectionProfileSettings
            var profileSettings = new ProtectionProfileSettings(mipContext, 
                CacheStorageType.OnDisk, 
                authDelegate, 
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

            var engineSettings = new ProtectionEngineSettings("", "", "")
            {
                Identity = identity
            };

            var engine = profile.AddEngine(engineSettings);

            return engine;
        }

        public List<string> ListTemplates()
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
            PublishingLicenseInfo plInfo = PublishingLicenseInfo.GetPublishingLicenseInfo(serializedPublishingLicense);
            ConsumptionSettings consumptionSettings = new ConsumptionSettings(plInfo);
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
            byte[] outputBuffer = new byte[buffersize];

            var bytesDecrypted = handler.DecryptBuffer(0, data, outputBuffer, true);
            return outputBuffer;
        }

    }
}