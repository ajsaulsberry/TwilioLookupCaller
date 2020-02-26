using System.ComponentModel.DataAnnotations;

namespace TwilioLookupCaller.Models
{
    public class PhoneNumberInfo
    {
        private string _countryCodeSelected;

        [Display(Name = "Issuing Country")]
        [Required]
        public string CountryCodeSelected
        {
            get => _countryCodeSelected;
            set => _countryCodeSelected = value?.ToUpperInvariant();
        }

        [Required]
        [Display(Name = "Phone Number")]
        [MaxLength(18)]
        public string PhoneNumberRaw { get; set; }

        [Display(Name = "Valid Number")]
        public bool Valid { get; set; }

        [Display(Name = "Country Code")]
        public string CountryCode { get; set; }

        [Display(Name = "National Dialing Format")]
        public string PhoneNumberFormatted { get; set; }

        [Display(Name = "Mobile Dialing Format")]
        public string PhoneNumberMobileDialing { get; set; }

        public Caller Caller { get; set; }
    }

    public class Caller
    {
        [Display(Name = "Caller Name")]
        public string CallerName { get; set; }

        [Display(Name = "Caller Type")]
        public string CallerType { get; set; }

        public string ErrorCode { get; set; }
    }
}
