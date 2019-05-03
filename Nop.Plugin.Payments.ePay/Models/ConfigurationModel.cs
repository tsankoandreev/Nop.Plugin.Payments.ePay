using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.ePay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.CustomerNumber")]
        public string CustomerNumber { get; set; }
        public bool CustomerNumber_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.DealerEmail")]
        public string DealerEmail { get; set; }
        public bool DealerEmail_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.SecretKey")]
        public string SecretKey { get; set; }
        public bool SecretKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.ExpirationTimeDays")]
        public int ExpirationTimeDays { get; set; }
        public bool ExpirationTimeDays_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.EnableEpay")]
        public bool EnableEpay { get; set; }
        public bool EnableEpay_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ePay.Fields.EnableEasyPay")]
        public bool EnableEasyPay { get; set; }
        public bool EnableEasyPay_OverrideForStore { get; set; }
    }
}