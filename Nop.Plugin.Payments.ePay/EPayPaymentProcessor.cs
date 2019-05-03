using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.ePay.Controllers;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nop.Services.Messages;

namespace Nop.Plugin.Payments.ePay
{
    /// <summary>
    /// PayPalDirect payment processor
    /// </summary>
    public class EPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly INotificationService _notificationService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IWorkContext _workContext;
        private readonly EPayPaymentSettings _ePayPaymentSettings;
        private readonly IOrderService _orderService;
        private readonly ILocalizationService _localizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericAttributeService _genericAttributeService;

        #endregion

        #region Ctor

        public EPayPaymentProcessor(ISettingService settingService,
            ICurrencyService currencyService, ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            IStoreContext storeContext,
        INotificationService notificationService,
            IOrderTotalCalculationService orderTotalCalculationService, IWorkContext workContext, EPayPaymentSettings ePayPaymentSettings,
            IOrderService orderService, ILocalizationService localizationService, IHttpContextAccessor httpContextAccessor, IGenericAttributeService genericAttributeService,
            IPaymentService paymentService)
        {
            _settingService = settingService;
            _currencyService = currencyService;
            _customerService = customerService;
            _currencySettings = currencySettings;
            _notificationService = notificationService;
            _webHelper = webHelper;
            _storeContext = storeContext;
            _orderTotalCalculationService = orderTotalCalculationService;
            _workContext = workContext;
            _ePayPaymentSettings = ePayPaymentSettings;
            _orderService = orderService;
            _localizationService = localizationService;
            _httpContextAccessor = httpContextAccessor;
            _genericAttributeService = genericAttributeService;
            _paymentService = paymentService;
        }

        #endregion

        #region Utilities
        /// <summary>
        /// Gets Paypal URL
        /// </summary>
        /// <returns></returns>
        public string GetEpaylUrl()
        {
            const string sandboxUrl = "https://demo.epay.bg/";
            var productionUrl = "https://www.epay.bg/";

            if (_workContext.WorkingLanguage.UniqueSeoCode.ToLower() != "bg")
            {
                productionUrl = productionUrl + "en/";
            }

            return _ePayPaymentSettings.UseSandbox ? sandboxUrl :
                productionUrl;
        }

        public string GetEasyPayUrl()
        {
            const string sandboxUrl = "https://demo.epay.bg/ezp/reg_bill.cgi";
            const string productionUrl = "https://www.epay.bg/ezp/reg_bill.cgi";

            return _ePayPaymentSettings.UseSandbox ? sandboxUrl :
                productionUrl;

        }

        public string EncodeHMACSHA1(string message, string key)
        {
            //System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Encoding.GetEncoding(1251).GetBytes(key);
            HMACSHA1 hmacsha1 = new HMACSHA1(keyByte);
            byte[] messageBytes = Encoding.GetEncoding(1251).GetBytes(message);
            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);
            string result = ByteToString(hashmessage);
            return result;
        }

        public string ByteToString(byte[] buff)
        {
            string sbinary = string.Empty;
            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2"); // hex format
            }
            return (sbinary);
        }

        public string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.GetEncoding(1251).GetBytes(toEncode);
            string returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public string DecodeFrom64(string toDecode)
        {
            var data = Convert.FromBase64String(toDecode);
            var decodedString = Encoding.GetEncoding(1251).GetString(data);

            return decodedString;
        }


        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (!_ePayPaymentSettings.EnableEasyPay && !_ePayPaymentSettings.EnableEpay)
            {
                return;
            }

            var orderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);

            var orderDescription = _localizationService.GetResource("Plugins.Payments.ePay.Fields.Configure.OrderDescription") + postProcessPaymentRequest.Order.Id;

            var bulgarianCurrency = _currencyService.GetCurrencyByCode("BGN");

            if (bulgarianCurrency != null)
            {
                orderTotal = _currencyService.ConvertFromPrimaryStoreCurrency(orderTotal, bulgarianCurrency);
            }

            var expDate = DateTime.Today.AddDays(1);

            if (_ePayPaymentSettings.ExpirationTimeDays > 0)
            {
                expDate = DateTime.Today.AddDays(_ePayPaymentSettings.ExpirationTimeDays);
            }

            var expDateFormated = expDate.ToString("dd.MM.yyyy");

            var paymentRequest =
                String.Format(
                    "min={0}\nemail={1}\ninvoice={2}\namount={3}\nexp_time={4}\ndescr={5}\nencoding={6}\ncurrency={7}",
                    _ePayPaymentSettings.CustomerNumber, _ePayPaymentSettings.DealerEmail,
                    postProcessPaymentRequest.Order.Id, orderTotal.ToString("0.00", CultureInfo.InvariantCulture), expDateFormated, orderDescription, "cp1251", "BGN");

            var encoded = EncodeTo64(paymentRequest);

            var builder = new StringBuilder();


            //var paymentMethod =
            //    _workContext.CurrentCustomer.GetAttribute<PaymentType>(Constants.CurrentPaymentTypeAttributeKey);

            var paymentMethod =
            _genericAttributeService.GetAttribute<PaymentType>(_workContext.CurrentCustomer, Constants.CurrentPaymentTypeAttributeKey);

            if (paymentMethod == PaymentType.Epay)
            {
                var returnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentEpay/PDTHandler";
                var cancelReturnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentEpay/CancelOrder";

                builder.Append(GetEpaylUrl());
                builder.AppendFormat("?PAGE=paylogin&encoded={0}&checksum={1}&url_ok={2}&url_cancel={3}", WebUtility.UrlEncode(encoded), EncodeHMACSHA1(encoded, _ePayPaymentSettings.SecretKey), returnUrl, cancelReturnUrl);

                _httpContextAccessor.HttpContext.Response.Redirect(builder.ToString());
            }
            else if (paymentMethod == PaymentType.EasyPay)
            {
                builder.AppendFormat("?encoded={0}&checksum={1}", WebUtility.UrlEncode(encoded), EncodeHMACSHA1(encoded, _ePayPaymentSettings.SecretKey));

                var req = (HttpWebRequest)WebRequest.Create(GetEasyPayUrl() + builder);
                req.Method = "GET";

                string response;


                using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    response = WebUtility.UrlDecode(sr.ReadToEnd());
                }

                if (!String.IsNullOrEmpty(response))
                {
                    var responseInfo = response.Split('\n')[0];

                    var splitResponse = responseInfo.Split('=');

                    if (splitResponse[0].ToLower() == "idn")
                    {
                        var responseCode = splitResponse[1];

                        if (!String.IsNullOrEmpty(responseCode))
                        {
                            postProcessPaymentRequest.Order.OrderNotes.Add(new OrderNote
                            {
                                Note = "EasyPay payment code: " + responseCode,
                                DisplayToCustomer = false,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                            _orderService.UpdateOrder(postProcessPaymentRequest.Order);

                            var easyPayInfoUrl =
                                String.Format("Plugins/PaymentEpay/EasyPayInfo?orderId={0}&easyPayCode={1}",
                                    postProcessPaymentRequest.Order.Id, responseCode);

                            _httpContextAccessor.HttpContext.Response.Redirect(_webHelper.GetStoreLocation(false) + easyPayInfoUrl);
                        }
                    }
                    else
                    {
                        _httpContextAccessor.HttpContext.Response.Redirect(_webHelper.GetStoreLocation(false) + "Plugins/PaymentEpay/EasyPayError?orderId=" + postProcessPaymentRequest.Order.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = _paymentService.CalculateAdditionalFee(cart,
               _ePayPaymentSettings.AdditionalFee, _ePayPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {

            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {

            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var paymentMethodType = form["PaymentType"];

            if (!String.IsNullOrEmpty(paymentMethodType))
            {
                var type = (PaymentType)Enum.Parse(typeof(PaymentType), paymentMethodType);

                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, Constants.CurrentPaymentTypeAttributeKey, type);
            }

            var warnings = new List<string>();
            return warnings;
        }


        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentEpay/Configure";
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public string GetPublicViewComponentName()
        {
            return "PaymentEpay";
        }

        public Type GetControllerType()
        {
            return typeof(PaymentEpayController);
        }

        public override void Install()
        {
            //settings
            var settings = new EPayPaymentSettings
            {
                UseSandbox = true,
                EnableEasyPay = true,
                EnableEpay = true
            };

            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.UseSandbox", "Use Sandbox");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.CustomerNumber", "Dealer ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.CustomerNumber.Hint", "Specify the dealer ID.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.DealerEmail", "Dealer email");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.DealerEmail.Hint", "Specify the dealer email.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.SecretKey", "Dealer's secret key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.ExpirationTimeDays", "Payment request expiration time");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.ExpirationTimeDays.Hint", "Set the payment request expiration time in days.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.SecretKey.Hint", "Specify the dealer's secret key.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEasyPay", "Enable EasyPay");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEasyPay.Hint", "Check to enable the EasyPay payment method.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEpay", "Enable ePay");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEpay.Hint", "Check to enable the ePay payment method.");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message1", "If you're using this gateway ensure that your store supports the BGN currency. (Currency code should be BGN)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message2", "1. Log into your ePay account as a merchant.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message3", "2. Click on Personal Information link.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message4", "3. Fill the bottom form with the data from this page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message5", "4. Go to the Merchant Information field and click the Edit button.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message6", "5. Click the Edit button at the bottom.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message7", "6. For the notfication URL set http://www.yourStore.com/Plugins/PaymentEpay/PaymentDone");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message8", "NOTE: Please, don't forget to replace www.yourStore.com with the url of your store.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message9", "7. Click the Review button.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message10", "8. Click the Accept button.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message11", "Your ePay plugin is now configured. Please don't forget to enable the ePay as a payment provider in your administration.");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EasyPayCode", "EasyPay Code");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.NoPaymentMethodAvailable", "No payment method is available.");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EpayName", "Pay with ePay");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EasyPayName", "Pay with EasyPay");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EasyPayError", "There was an error with the easy pay provider. Please check if the ePay settings are configured correctly. And are set for the selected enviroment (Sandbox or production)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Title.Error", "Error");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.OrderDescription", "Payment for order ");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.PaymentMethodDescription", "Pay by Epay / EasyPay card");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Suffix.Days", "days");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<EPayPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.UseSandbox");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.UseSandbox.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.CustomerNumber");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.CustomerNumber.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.DealerEmail");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.DealerEmail.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.SecretKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.SecretKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.ExpirationTimeDays");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.ExpirationTimeDays.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEasyPay");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEasyPay.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEpay");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.EnableEpay.Hint");

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message1");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message2");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message3");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message4");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message5");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message6");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message7");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message8");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message9");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message10");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Message11");

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EasyPayCode");

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.NoPaymentMethodAvailable");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EpayName");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EasyPayName");

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.EasyPayError");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Title.Error");
            
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.OrderDescription");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.PaymentMethodDescription");
            
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ePay.Fields.Configure.Suffix.Days");


            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }      
        public CurrencySettings CurrencySettings
        {
            get { return _currencySettings; }
        }

        public ICustomerService CustomerService
        {
            get { return _customerService; }
        }

        public string PaymentMethodDescription
        {
            get
            {
                return _localizationService.GetResource("Plugins.Payments.ePay.PaymentMethodDescription");
            }
        }

        #endregion
    }
}