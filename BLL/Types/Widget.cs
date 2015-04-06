using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class Widget
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        private List<PartGroup> partGroups = new List<PartGroup>();
        public List<PartGroup> PartGroups
        {
            get { return this.partGroups; }
            set { this.partGroups = value; }
        }

        public Widget(string id, string name, string url)
        {
            this.Id = id;
            this.Name = name;
            this.Url = url;
        }
    }
}
