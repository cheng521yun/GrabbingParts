using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.Types
{
    //产品规格
    public class ProductSpecification
    {
        public string Name { get; set; } //规格名称
        public string Content { get; set; } //规格内容

        public ProductSpecification(string name, string content)
        {
            this.Name = name;
            this.Content = content;
        }
    }
}
