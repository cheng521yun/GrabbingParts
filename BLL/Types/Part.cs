using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class Part
    {
        public string Id { get; set; } //制造商零件编号
        public string Manufacturer { get; set; } //制造商
        public string Url { get; set; }
        public string Description { get; set; } //描述
        public string ZoomImageUrl { get; set; }
        public string ImageUrl { get; set; }
        public string DatasheetUrl { get; set; }
        public string Packing { get; set; } //包装 (标准包装)

        private List<ProductSpecification> productSpecifications = new List<ProductSpecification>();
        public List<ProductSpecification> ProductSpecifications
        {
            get { return this.productSpecifications; }
            set { this.productSpecifications = value; }
        }

        public Part(string id, string manufacturer, string url, string description, string zoomImageUrl,
            string imageUrl, string datasheetUrl, string packing)
        {
            this.Id = id;
            this.Manufacturer = manufacturer;
            this.Url = url;
            this.Description = description;
            this.ZoomImageUrl = zoomImageUrl;
            this.ImageUrl = imageUrl;
            this.DatasheetUrl = datasheetUrl;
            this.Packing = packing;
        }
    }
}
