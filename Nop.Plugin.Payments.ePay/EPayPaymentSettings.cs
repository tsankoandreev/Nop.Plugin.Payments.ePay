using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.ePay
{
    public class EPayPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }

        public string CustomerNumber { get; set; }

        public string DealerEmail { get; set; }

        public string SecretKey { get; set; }

        public int ExpirationTimeDays { get; set; }

        public decimal AdditionalFee { get; set; }

        public bool AdditionalFeePercentage { get; set; }

        public bool EnableEpay { get; set; }

        public bool EnableEasyPay { get; set; }
    }
}