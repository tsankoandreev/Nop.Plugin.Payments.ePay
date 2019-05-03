using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.ePay.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.ePay.Components
{
    [ViewComponent(Name = "PaymentEpay")]
    public class PaymentEpayComponent : NopViewComponent
    {
        private readonly EPayPaymentSettings _ePayPaymentSettings;

        public PaymentEpayComponent(EPayPaymentSettings ePayPaymentSettings)
        {
            _ePayPaymentSettings = ePayPaymentSettings;
        }

        public IViewComponentResult Invoke()
        {
            var model = new EpayPaymentMethodModel
            {
                EnableEpay = _ePayPaymentSettings.EnableEpay,
                EnableEasyPay = _ePayPaymentSettings.EnableEasyPay
            };

            if (_ePayPaymentSettings.EnableEpay)
            {
                model.PaymentType = PaymentType.Epay;
            }
            else
            {
                model.PaymentType = PaymentType.EasyPay;
            }

            return View("~/Plugins/Payments.ePay/Views/PaymentEpay/PaymentInfo.cshtml", model);
        }
    }
}