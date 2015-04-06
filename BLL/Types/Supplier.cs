using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class Supplier
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private List<Category> categories = new List<Category>();
        public List<Category> Categories
        {
            get { return this.categories; }
            set { this.categories = value; }
        }
    }
}
