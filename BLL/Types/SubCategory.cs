using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class SubCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private List<Widget> widgets = new List<Widget>();
        public List<Widget> Widgets
        {
            get { return this.widgets; }
            set { this.widgets = value; }
        }

        public SubCategory(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}