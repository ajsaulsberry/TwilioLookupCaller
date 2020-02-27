using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions; // Needed to catch PhoneLookup.DLL errors.
using Twilio.Rest.Lookups.V1;
using TwilioLookupCaller.Models;

namespace TwilioLookupCaller
{
    public class TwilioLookupModel : PageModel
    {
        readonly TwilioSettings _twilioSettings;

        [BindProperty(SupportsGet = true)]
        public PhoneNumberInfo PhoneNumberInfo { get; set; }

        public TwilioLookupModel(IOptions<TwilioSettings> twilioSettings)
        {
            _twilioSettings = twilioSettings?.Value
                ?? throw new ArgumentNullException(nameof(twilioSettings));
        }
        public IActionResult OnGet()
        {
            // For demonstration purposes, preset. Caller Lookup is a US-only service.
            PhoneNumberInfo.CountryCodeSelected = $"US";
            return Page();

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);

                // Return the input values to the ModelState.
                ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.CountryCodeSelected)}").Value.RawValue =
                    PhoneNumberInfo.CountryCodeSelected;

                ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.PhoneNumberRaw)}").Value.RawValue =
                    PhoneNumberInfo.PhoneNumberRaw;

                // Try getting information for the CountryCodeSelected and PhoneNumberRaw values.
                var phoneNumber = await PhoneNumberResource.FetchAsync(
                        countryCode: PhoneNumberInfo.CountryCodeSelected,
                        pathPhoneNumber: new Twilio.Types.PhoneNumber(PhoneNumberInfo.PhoneNumberRaw),
                        type: new List<string> { "caller-name" }
                    );
                // If PhoneNumberResource.FetchAsync can't resolve the pathPhoneNumber and countryCode into a valid phone number,
                // Twilio.dll throws Twilio.Exceptions.ApiException. You can catch the error by 1) using Twilio.Exceptions,
                // 2) catching the error, and 3) updating the ModelState with the error information.

                // If you've gotten to this point, the Twilio Helper Library was able to determine that the supplied country code and phone number are valid,
                // but not necessarily that the phone number has been assigned.
                ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.Valid)}").Value.RawValue = true;

                // You can return the validated phone number info to the UI via the model state.
                ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.CountryCode)}").Value.RawValue =
                    phoneNumber.CountryCode;

                ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.PhoneNumberFormatted)}").Value.RawValue =
                    phoneNumber.NationalFormat;

                ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.PhoneNumberMobileDialing)}").Value.RawValue =
                    phoneNumber.PhoneNumber;

                // CallerName is a Dictionary object containing caller_name, caller_type, and error_code.
                // It's created if you asked for it when you called PhoneNumberResource.FetchAsync.
                if (phoneNumber.CallerName != null)
                {

                    phoneNumber.CallerName.TryGetValue("error_code", out string callerErrorCode);
                    if (!String.IsNullOrEmpty(callerErrorCode))
                    {
                        throw new ApiException(int.Parse(callerErrorCode), 000, "caller lookup error", moreInfo: " ");
                    }
                    else
                    {
                        phoneNumber.CallerName.TryGetValue("caller_name", out string callerName);
                        ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.Caller)}.{nameof(PhoneNumberInfo.Caller.CallerName)}")
                            .Value.RawValue = (callerName ?? String.Empty);

                        phoneNumber.CallerName.TryGetValue("caller_type", out string callerType);
                        ModelState.FirstOrDefault(x => x.Key == $"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.Caller)}.{nameof(PhoneNumberInfo.Caller.CallerType)}")
                            .Value.RawValue = (callerType ?? String.Empty);
                    }
                }
            }
            catch (ApiException apiex)
            {
                ModelState.AddModelError($"{nameof(PhoneNumberInfo)}.{nameof(PhoneNumberInfo.PhoneNumberRaw)}", $"Twilio API Error {apiex.Code}: {apiex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(ex.GetType().ToString(), ex.Message);
            }
            return Page();
        }
    }
}