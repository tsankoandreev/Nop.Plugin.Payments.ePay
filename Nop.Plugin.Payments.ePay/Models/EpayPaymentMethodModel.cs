using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.ePay.Models
{
    public class EpayPaymentMethodModel: BaseNopModel
    {
        public PaymentType PaymentType { get; set; }

        public bool EnableEpay { get; set; }

        public bool EnableEasyPay { get; set; }
    }
}