using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.ePay.Models
{
    public class EasyPayCompletedModel : BaseNopModel
    {
        public int OrderId { get; set; }
        public bool OnePageCheckoutEnabled { get; set; }

        public string EasyPayCode { get; set; }
    }
}