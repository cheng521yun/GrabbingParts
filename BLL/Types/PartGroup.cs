using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class PartGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        private List<Part> parts = new List<Part>();
        public List<Part> Parts
        {
            get { return this.parts; }
            set { this.parts = value; }
        }

        public PartGroup(string id, string name, string url)
        {
            this.Id = id;
            this.Name = name;
            this.Url = url;
        }
    }
}
