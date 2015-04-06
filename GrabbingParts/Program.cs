using GrabbingParts.BLL.ScraperLibrary;

namespace GrabbingParts
{
    static class Program
    {
        static void Main(string[] args)
        {
            Scraper scraper = new DigikeyScraper();
            scraper.Scrape();
        }
    }
}