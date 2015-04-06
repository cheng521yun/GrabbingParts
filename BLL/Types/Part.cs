using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class Part
    {
        public string Id { get; set; }
        public string Manufacturer { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string ZoomImageUrl { get; set; }
        public string ImageUrl { get; set; }
        public string DatasheetUrl { get; set; }

        public Part(string id, string manufacturer, string url, string description, string zoomImageUrl,
            string imageUrl, string datasheetUrl)
        {
            this.Id = id;
            this.Manufacturer = manufacturer;
            this.Url = url;
            this.Description = description;
            this.ZoomImageUrl = zoomImageUrl;
            this.ImageUrl = imageUrl;
            this.DatasheetUrl = datasheetUrl;
        }
    }
}
