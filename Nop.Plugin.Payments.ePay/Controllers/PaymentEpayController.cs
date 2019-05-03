using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.ePay.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.ePay.Controllers
{
    public class PaymentEpayController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IStoreContext _storeContext;
        private readonly OrderSettings _orderSettings;

        public PaymentEpayController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            ILocalizationService localizationService,
            IPaymentService paymentService, 
            IOrderService orderService,
        INotificationService notificationService,
            IOrderProcessingService orderProcessingService, 
            IStoreContext storeContext,
            OrderSettings orderSettings)
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _orderService = orderService;
            _notificationService = notificationService;
            _orderProcessingService = orderProcessingService;
            _storeContext = storeContext;
            _orderSettings = orderSettings;
        }

        [Area(AreaNames.Admin)]
        [AuthorizeAdmin]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var ePaySettings = _settingService.LoadSetting<EPayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = ePaySettings.UseSandbox,
                CustomerNumber = ePaySettings.CustomerNumber,
                DealerEmail = ePaySettings.DealerEmail,
                SecretKey = ePaySettings.SecretKey,
                ExpirationTimeDays = ePaySettings.ExpirationTimeDays,
                EnableEasyPay = ePaySettings.EnableEasyPay,
                EnableEpay = ePaySettings.EnableEpay,
                ActiveStoreScopeConfiguration = storeScope,
                AdditionalFee = ePaySettings.AdditionalFee,
                AdditionalFeePercentage = ePaySettings.AdditionalFeePercentage
            };

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.UseSandbox, storeScope);
                model.CustomerNumber_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.CustomerNumber, storeScope);
                model.DealerEmail_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.DealerEmail, storeScope);
                model.SecretKey_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.SecretKey, storeScope);
                model.ExpirationTimeDays_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.ExpirationTimeDays, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.AdditionalFeePercentage, storeScope);
                model.EnableEasyPay_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.EnableEasyPay, storeScope);
                model.EnableEpay_OverrideForStore = _settingService.SettingExists(ePaySettings, x => x.EnableEpay, storeScope);
            }

            return View("~/Plugins/Payments.ePay/Views/PaymentEpay/Configure.cshtml", model);
        }

        [HttpPost]
        [Area(AreaNames.Admin)]
        [AuthorizeAdmin]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var ePayPaymentSettings = _settingService.LoadSetting<EPayPaymentSettings>(storeScope);

            //save settings
            ePayPaymentSettings.UseSandbox = model.UseSandbox;
            ePayPaymentSettings.CustomerNumber = model.CustomerNumber;
            ePayPaymentSettings.DealerEmail = model.DealerEmail;
            ePayPaymentSettings.SecretKey = model.SecretKey;
            ePayPaymentSettings.ExpirationTimeDays = model.ExpirationTimeDays;
            ePayPaymentSettings.AdditionalFee = model.AdditionalFee;
            ePayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            ePayPaymentSettings.EnableEpay = model.EnableEpay;
            ePayPaymentSettings.EnableEasyPay = model.EnableEasyPay;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.CustomerNumber_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.CustomerNumber, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.CustomerNumber, storeScope);

            if (model.DealerEmail_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.DealerEmail, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.DealerEmail, storeScope);

            if (model.SecretKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.SecretKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.SecretKey, storeScope);

            if (model.ExpirationTimeDays_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.ExpirationTimeDays, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.ExpirationTimeDays, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.EnableEasyPay_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.EnableEasyPay, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.EnableEasyPay, storeScope);


            if (model.EnableEpay_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(ePayPaymentSettings, x => x.EnableEpay, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(ePayPaymentSettings, x => x.EnableEpay, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }


        public ActionResult EasyPayInfo(int orderId, string easyPayCode)
        {
            if (orderId > 0 && !String.IsNullOrEmpty(easyPayCode))
            {
                var model = new EasyPayCompletedModel
                {
                    OrderId = orderId,
                    OnePageCheckoutEnabled = _orderSettings.OnePageCheckoutEnabled,
                    EasyPayCode = easyPayCode
                };

                return View("~/Plugins/Payments.ePay/Views/PaymentEpay/Completed.cshtml", model);

            }

            return RedirectToAction("Index", "Home", new { area = "" });

        }

        public ActionResult EasyPayError(int orderId)
        {
            var model = new EasyPayErrorModel
            {
                OrderId = orderId
            };

            return View("~/Plugins/Payments.ePay/Views/PaymentEpay/EasyPayError.cshtml", model);
        }

        [HttpPost]
        public ActionResult PaymentDone(FormCollection form)
        {
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var ePayPaymentSettings = _settingService.LoadSetting<EPayPaymentSettings>(storeScope);
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.ePay/EasyPay") as EPayPaymentProcessor;
            var encoded = form["encoded"];
            var checksum = form["checksum"];

            const string messageString = "INVOICE={0}:STATUS={1}\n";
            var message = String.Empty;


            if (!String.IsNullOrEmpty(encoded) && !String.IsNullOrEmpty(checksum) && processor != null)
            {
                var hmac = processor.EncodeHMACSHA1(encoded, ePayPaymentSettings.SecretKey);

                if (hmac.ToLower() == checksum)
                {
                    var decodedData = processor.DecodeFrom64(encoded);

                    var invoiceNumberString = String.Empty;
                    var status = String.Empty;

                    // Get the separated data
                    foreach (var line in decodedData.Split('\n'))
                    {
                        var splitLines = line.Split(':');
                        if (splitLines.Length > 1)
                        {
                            foreach (var splitLine in splitLines)
                            {
                                var separatedValues = splitLine.Split('=');

                                if (separatedValues.Length > 1)
                                {
                                    if (separatedValues[0].ToLower() == "invoice")
                                    {
                                        invoiceNumberString = separatedValues[1];
                                    }
                                    else if (separatedValues[0].ToLower() == "status")
                                    {
                                        status = separatedValues[1];
                                    }
                                }
                            }
                        }
                    }

                    // Set the order status based on the data
                    if (!String.IsNullOrEmpty(invoiceNumberString) && !String.IsNullOrEmpty(status))
                    {
                        int invoiceNumber;
                        if (Int32.TryParse(invoiceNumberString, out invoiceNumber))
                        {
                            var order = _orderService.GetOrderById(invoiceNumber);
                            if (order != null)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine("Epay PDT:");
                                sb.AppendLine("invoice: " + invoiceNumber);

                                if (status.ToLower() == "paid")
                                {
                                    sb.AppendLine("status: " + status);

                                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                                    {
                                        _orderProcessingService.MarkOrderAsPaid(order);
                                    }

                                    message = String.Format(messageString, invoiceNumber, "OK");

                                }
                                else if (status.ToLower() == "denied")
                                {
                                    sb.AppendLine("status: " + status);

                                    VoidOrder(order);

                                    message = String.Format(messageString, invoiceNumber, "OK");
                                }
                                else if (status.ToLower() == "expired")
                                {
                                    sb.AppendLine("status: " + status);

                                    VoidOrder(order);

                                    message = String.Format(messageString, invoiceNumber, "OK");
                                }
                                else
                                {
                                    sb.AppendLine("status: Error");
                                }

                                order.OrderNotes.Add(new OrderNote
                                {
                                    Note = sb.ToString(),
                                    DisplayToCustomer = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                                _orderService.UpdateOrder(order);

                                if (!String.IsNullOrEmpty(message))
                                {
                                    return Content(message);
                                }
                            }
                            message = String.Format(messageString, invoiceNumber, "ERR");
                        }
                    }
                }
            }

            // Send a response to the epay server
            if (!String.IsNullOrEmpty(message))
            {
                return Content(message);
            }

            return new EmptyResult();
        }

        public ActionResult PDTHandler(FormCollection form)
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                .FirstOrDefault();
            if (order != null)
            {
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public ActionResult CancelOrder(FormCollection form)
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                .FirstOrDefault();
            if (order != null)
            {
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        private void VoidOrder(Order order)
        {
            order.PaymentStatusId = (int)PaymentStatus.Voided;
            _orderService.UpdateOrder(order);

            //add a note
            order.OrderNotes.Add(new OrderNote
            {
                Note = "Order has been marked as voided",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);
        }
    }
}