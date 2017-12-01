
namespace Nop.Plugin.Tax.Avalara.Models.Tax
{
    /// <summary>
    /// Represents an extended tax category model
    /// </summary>
    public class TaxCategoryModel : Nop.Web.Areas.Admin.Models.Tax.TaxCategoryModel
    {
        public string Description { get; set; }

        public string Type { get; set; }

        public string TypeId { get; set; }
    }
}