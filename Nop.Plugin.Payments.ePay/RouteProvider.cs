using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.ePay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //PDT
            routeBuilder.MapRoute("Plugin.Payments.ePay.PDTHandler",
                 "Plugins/PaymentEpay/PDTHandler",
                 new { controller = "PaymentEpay", action = "PDTHandler" });

            routeBuilder.MapRoute("Plugin.Payments.ePay.CancelOrder",
                "Plugins/PaymentEpay/CancelOrder",
                new { controller = "PaymentEpay", action = "CancelOrder" });

            routeBuilder.MapRoute("Plugin.Payments.ePay.PaymentDone",
               "Plugins/PaymentEpay/PaymentDone",
               new { controller = "PaymentEpay", action = "PaymentDone" });

            routeBuilder.MapRoute("Plugin.Payments.ePay.EasyPayInfo",
              "Plugins/PaymentEpay/EasyPayInfo",
              new { controller = "PaymentEpay", action = "EasyPayInfo" });

            routeBuilder.MapRoute("Plugin.Payments.ePay.EasyPayError",
             "Plugins/PaymentEpay/EasyPayError",
             new { controller = "PaymentEpay", action = "EasyPayError" });
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
