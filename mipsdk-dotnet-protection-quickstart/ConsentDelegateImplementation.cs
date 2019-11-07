using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Protection;

namespace mipsdk_dotnet_protection_quickstart
{
    class ConsentDelegateImplementation : IConsentDelegate
    {
        public Consent GetUserConsent(string url)
        {
            return Consent.AcceptAlways;
        }
    }
}
