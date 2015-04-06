using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private List<SubCategory> subCategories = new List<SubCategory>();
        public List<SubCategory> SubCategories
        {
            get { return this.subCategories; }
            set { this.subCategories = value; }
        }

        public Category(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}
