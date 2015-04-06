using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrabbingParts.BLL.Types;
using GrabbingParts.Util.StringHelpers;
using System.Diagnostics;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace GrabbingParts.BLL.ScraperLibrary
{
    public class DigikeyScraper : Scraper
    {
        private const string DIGIKEYHOMEURL = "http://www.digikey.cn";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Supplier supplier = new Supplier();

        public static HtmlDocument baseHtmlDoc = new HtmlDocument();

        static Dictionary<string, Dictionary<string, string>> categoryListDict = new Dictionary<string, Dictionary<string, string>>();
        static Dictionary<string, IEnumerable<string>> categoryIndexListDict = new Dictionary<string, IEnumerable<string>>();
        static Dictionary<string, string> failDetailedLinks = new Dictionary<string, string>();        

        public override void ScrapePage()
        {
            Stopwatch sw = Stopwatch.StartNew();
            log.Debug("Before the method GetBaseHtmlDocument.................................................................");

            GetBaseHtmlDocument();

            sw.Stop();
            log.DebugFormat("GetBaseHtmlDocument finish.cost:{0}ms.................................................................", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();            
            GetCategory();

            sw.Stop();
            log.DebugFormat("GetCategory finish.cost:{0}ms.................................................................", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetSubCategoryAndWidget();

            sw.Stop();
            log.DebugFormat("GetSubCategoryAndWidget finish.cost:{0}ms.................................................................", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetPartGroup();

            sw.Stop();
            log.DebugFormat("GetPartGroup finish.cost:{0}ms.................................................................", sw.ElapsedMilliseconds);
        }

        private void GetBaseHtmlDocument()
        {            
            //string textHome = HttpHelpers.HttpHelpers.GetText(homeUrl, 10000);          
            //string url = "http://www.digikey.com.cn/search/zh?c=406&f=408&f=409&f=410&f=411&f=412&f=413&f=414";
            //string text = HttpHelpers.HttpHelpers.GetText(url,10000);

            HtmlWeb htmlWeb = new HtmlWeb();
            baseHtmlDoc = htmlWeb.Load(DIGIKEYHOMEURL);
        }

        /// <summary>
        /// Get category
        /// </summary>
        private void GetCategory()
        {
            string xpathForCategory = "/html/body/div[@id='wrapper']/div[@id='top_categories']/a[@id='topnav-link']/table[@id='toprow']/tr/td[img]";
            HtmlNodeCollection uiCategoryList = baseHtmlDoc.DocumentNode.SelectNodes(xpathForCategory);
            int key = 1;
            string categoryName = "";

            foreach(HtmlNode uiCategory in uiCategoryList)
            {
                categoryName = uiCategory.InnerText;
                supplier.Categories.Add(new Category(key.ToString(), categoryName));
                key++;
            }
        }

        /// <summary>
        /// Get sub category and widget
        /// </summary>
        private void GetSubCategoryAndWidget()
        {
            string xpathForSubCategory = "/html/body/div[@id='wrapper']/div[@id='top_categories']/div[@id='topnav']/table[@id='bottomrow']/tr/td[a]";
            HtmlNodeCollection uiSubCategoryList = baseHtmlDoc.DocumentNode.SelectNodes(xpathForSubCategory);
            string subCategoryId = "";
            string subCategoryName = "";
            int widgetId = 1;
            string widgetName = "";
            string widgetUrl = "";
            int i = 0;
            HtmlNode anchorNode;

            foreach (HtmlNode uiSubCategory in uiSubCategoryList)
            {
                HtmlNodeCollection uiAnchorList = uiSubCategory.SelectNodes("a[@id!='viewall-link']");
                foreach(HtmlNode uiAnchor in uiAnchorList)
                {
                    subCategoryId = uiAnchor.Attributes["id"].Value;
                    subCategoryName = uiAnchor.InnerText;
                    SubCategory subCategory = new SubCategory(subCategoryId, subCategoryName);
                    HtmlNode ul = uiAnchor.NextSibling.NextSibling;
                    HtmlNodeCollection liList = ul.SelectNodes("li");
                    widgetId = 1;

                    foreach(HtmlNode li in liList)
                    {
                        anchorNode = li.SelectSingleNode("a");
                        widgetName = anchorNode.InnerText;
                        widgetUrl = anchorNode.Attributes["href"].Value;
                        subCategory.Widgets.Add(new Widget(widgetId.ToString(), widgetName, widgetUrl));
                        widgetId++;
                    }

                    supplier.Categories[i].SubCategories.Add(subCategory);
                }

                i++;
            }
        }

        /// <summary>
        /// Get part group
        /// </summary>
        private void GetPartGroup()
        {
            Task getPartGroupForSemiconductorProducts = Task.Factory.StartNew(() => { GetPartGroupForSemiconductorProducts(); });
            Task getPartGroupForPassiveComponents = Task.Factory.StartNew(() => { GetPartGroupForPassiveComponents(); });
            Task getPartGroupForInterconnectProducts = Task.Factory.StartNew(() => { GetPartGroupForInterconnectProducts(); });
            Task getPartGroupForMechanicalElectronicProducts = Task.Factory.StartNew(() => { GetPartGroupForMechanicalElectronicProducts(); });
            Task getPartGroupForPhotoelectricElement = Task.Factory.StartNew(() => { GetPartGroupForPhotoelectricElement(); });

            Task[] taskList = { getPartGroupForSemiconductorProducts, getPartGroupForPassiveComponents, getPartGroupForInterconnectProducts,
                                getPartGroupForMechanicalElectronicProducts, getPartGroupForPhotoelectricElement};

            try
            {
                Task.WaitAll(taskList);
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten();
            }
        }

        /// <summary>
        /// 半导体产品
        /// </summary>
        private void GetPartGroupForSemiconductorProducts()
        {
            GetPartGroupForCategory(0);
        }

        /// <summary>
        /// 无源元件
        /// </summary>
        private void GetPartGroupForPassiveComponents()
        {
            GetPartGroupForCategory(1);
        }

        /// <summary>
        /// 互连产品
        /// </summary>
        private void GetPartGroupForInterconnectProducts()
        {
            GetPartGroupForCategory(2);
        }

        /// <summary>
        /// 机电产品
        /// </summary>
        private void GetPartGroupForMechanicalElectronicProducts()
        {
            GetPartGroupForCategory(3);
        }

        /// <summary>
        /// 光电元件
        /// </summary>
        private void GetPartGroupForPhotoelectricElement()
        {
            GetPartGroupForCategory(4);
        }

        /// <summary>
        /// Get part group for each category
        /// </summary>
        /// <param name="categoryIndex"></param>
        private void GetPartGroupForCategory(int categoryIndex)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            string partGroupXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/ul[@id='productIndexList']/li[@class='catfilteritem']/ul[@class='catfiltersub']/li";
            string partXpath = "//table[@id='productTable']//tbody/tr";
            string partGroupName;
            string partGroupUrl;
            HtmlNode anchorNode;
            string partId;
            string partUrl;
            string manufacturer;
            string description;
            string zoomImageUrl;
            string imageUrl;
            string datasheetUrl;
            HtmlAttribute zoomImageNode;

            foreach (SubCategory subCategory in supplier.Categories[categoryIndex].SubCategories)
            {
                foreach (Widget widget in subCategory.Widgets)
                {
                    bool noPartGroup = StringHelpers.IsInteger(StringHelpers.GetLastDirectory("/", widget.Url));
                    if (noPartGroup)
                    {
                        widget.PartGroups.Add(new PartGroup("1", "PartGroup", widget.Url));
                    }
                    else
                    {
                        HtmlDocument widgetHtmlDoc = htmlWeb.Load(widget.Url);
                        HtmlNodeCollection liList = widgetHtmlDoc.DocumentNode.SelectNodes(partGroupXpath);
                        int partGroupId = 1;

                        foreach (HtmlNode li in liList)
                        {
                            anchorNode = li.SelectSingleNode("a");
                            partGroupName = anchorNode.InnerText;
                            partGroupUrl = DIGIKEYHOMEURL + anchorNode.Attributes["href"].Value;
                            
                            PartGroup partGroup = new PartGroup(partGroupId.ToString(), partGroupName, partGroupUrl);
                            HtmlDocument partGroupHtmlDoc = htmlWeb.Load(partGroupUrl);

                            HtmlNodeCollection trList = partGroupHtmlDoc.DocumentNode.SelectNodes(partXpath);

                            if (trList != null)
                            {
                                foreach (HtmlNode tr in trList)
                                {
                                    partId = tr.SelectSingleNode("td[@class='mfg-partnumber']/a/span").InnerText;
                                    partUrl = DIGIKEYHOMEURL + tr.SelectSingleNode("td[@class='mfg-partnumber']/a").Attributes["href"].Value;
                                    manufacturer = tr.SelectSingleNode("td[@class='vendor']/span//span").InnerText;
                                    description = tr.SelectSingleNode("td[@class='description']").InnerText;

                                    zoomImageNode = tr.SelectSingleNode("td[@class='image']/a/img").Attributes["zoomimg"];
                                    if (zoomImageNode != null)
                                    {
                                        zoomImageUrl = zoomImageNode.Value;
                                    }
                                    else
                                    {
                                        zoomImageUrl = "";
                                    }
                                    
                                    imageUrl = tr.SelectSingleNode("td[@class='image']/a/img").Attributes["src"].Value;

                                    if (tr.SelectSingleNode("td[@class='rd-datasheet']/center/a") != null)
                                    {
                                        datasheetUrl = tr.SelectSingleNode("td[@class='rd-datasheet']/center/a").Attributes["href"].Value;
                                    }
                                    else
                                    {
                                        datasheetUrl = "";
                                    }

                                    partGroup.Parts.Add(new Part(partId, manufacturer, partUrl, description, zoomImageUrl,
                                        imageUrl, datasheetUrl));
                                }
                            }
                            else
                            {
                                //Todo: add the partGroupUrl to log file
                            }

                            widget.PartGroups.Add(partGroup);
                            partGroupId++;
                        }
                    }
                }
            }
        }
    }
}
