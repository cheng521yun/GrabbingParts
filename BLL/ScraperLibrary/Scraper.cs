using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabbingParts.BLL.ScraperLibrary
{
    public abstract class Scraper : IScraper
    {
        public bool Scrape()
        {
            this.ScrapePage();
            return true;
        }

        public abstract void ScrapePage();
    }
}
