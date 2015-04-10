using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using GrabbingParts.BLL.Types;
using GrabbingParts.DAL.DataAccessCenter;
using GrabbingParts.Util.StringHelpers;
using GrabbingParts.Util.XmlHelpers;
using HtmlAgilityPack;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace GrabbingParts.BLL.ScraperLibrary
{
    public class DigikeyScraper : Scraper
    {
        private const string SUPPLIERNAME = "digikey";
        private const string DIGIKEYHOMEURL = "http://www.digikey.cn";
        private const string BAOZHUANG = "包装";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Supplier supplier = new Supplier();
        private XElement scrapedData = XElement.Parse(string.Format("<r name=\"{0}\" url=\"{1}\"><cats/></r>", SUPPLIERNAME, DIGIKEYHOMEURL));
        private int partCount;
        private List<DataTable> categoryDataTables = new List<DataTable>();

        public static HtmlDocument baseHtmlDoc = new HtmlDocument();

        //static Dictionary<string, Dictionary<string, string>> categoryListDict = new Dictionary<string, Dictionary<string, string>>();
        //static Dictionary<string, IEnumerable<string>> categoryIndexListDict = new Dictionary<string, IEnumerable<string>>();
        //static Dictionary<string, string> failDetailedLinks = new Dictionary<string, string>();

        public override void ScrapePage()
        {
            Stopwatch sw = Stopwatch.StartNew();
            log.Debug("Before the method GetBaseHtmlDocument.");

            GetBaseHtmlDocument();

            sw.Stop();
            log.DebugFormat("GetBaseHtmlDocument finish.cost:{0}ms", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetCategory();

            sw.Stop();
            log.DebugFormat("GetCategory finish.cost:{0}ms", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetSubCategoryAndWidget();

            sw.Stop();
            log.DebugFormat("GetSubCategoryAndWidget finish.cost:{0}ms", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetPartGroup();

            sw.Stop();
            log.DebugFormat("GetPartGroup finish.cost:{0}ms", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            PrepareScrapedData();

            sw.Stop();
            log.DebugFormat("PrepareScrapedData finish.cost:{0}ms", sw.ElapsedMilliseconds);
            log.DebugFormat("Part count:{0}", partCount);

            InitCategoryDataTables();

            PrepareCategoryDataTables();

            InsertDataToDatabase();
        }

        private void GetBaseHtmlDocument()
        {
            //string textHome = HttpHelpers.HttpHelpers.GetText(homeUrl, 10000);          
            //string url = "http://www.digikey.com.cn/search/zh?c=406&f=408&f=409&f=410&f=411&f=412&f=413&f=414";
            //string text = HttpHelpers.HttpHelpers.GetText(url,10000);

            baseHtmlDoc = Common.Common.RetryRequest(DIGIKEYHOMEURL);
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

            foreach (HtmlNode uiCategory in uiCategoryList)
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
                foreach (HtmlNode uiAnchor in uiAnchorList)
                {
                    subCategoryId = uiAnchor.Attributes["id"].Value;
                    subCategoryName = uiAnchor.InnerText;
                    SubCategory subCategory = new SubCategory(subCategoryId, subCategoryName);
                    HtmlNode ul = uiAnchor.NextSibling.NextSibling;
                    HtmlNodeCollection liList = ul.SelectNodes("li");
                    widgetId = 1;

                    foreach (HtmlNode li in liList)
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
            //Task getPartGroupForPassiveComponents = Task.Factory.StartNew(() => { GetPartGroupForPassiveComponents(); });
            //Task getPartGroupForInterconnectProducts = Task.Factory.StartNew(() => { GetPartGroupForInterconnectProducts(); });
            //Task getPartGroupForMechanicalElectronicProducts = Task.Factory.StartNew(() => { GetPartGroupForMechanicalElectronicProducts(); });
            //Task getPartGroupForPhotoelectricElement = Task.Factory.StartNew(() => { GetPartGroupForPhotoelectricElement(); });

            //Task[] taskList = { getPartGroupForSemiconductorProducts, getPartGroupForPassiveComponents, getPartGroupForInterconnectProducts,
            //                    getPartGroupForMechanicalElectronicProducts, getPartGroupForPhotoelectricElement};

            Task[] taskList = { getPartGroupForSemiconductorProducts };

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
            string partDetailXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/table[@class='product-additional-info']/tr/td[@class='attributes-table-main']/table/tr[5]/td";
            string productSpecXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/table[@class='product-additional-info']/tr/td[@class='attributes-table-main']/table/tr";
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
            string packing;
            string productSpecName;
            string productSpecContent;

            foreach (SubCategory subCategory in supplier.Categories[categoryIndex].SubCategories)
            {
                foreach (Widget widget in subCategory.Widgets)
                {
                    bool noPartGroup = StringHelpers.IsInteger(StringHelpers.GetLastDirectory(widget.Url));
                    if (noPartGroup)
                    {
                        widget.PartGroups.Add(new PartGroup("1", "PartGroup", widget.Url));
                    }
                    else
                    {
                        HtmlDocument widgetHtmlDoc = Common.Common.RetryRequest(widget.Url);
                        HtmlNodeCollection liList = widgetHtmlDoc.DocumentNode.SelectNodes(partGroupXpath);
                        int partGroupId = 1;

                        foreach (HtmlNode li in liList)
                        {
                            anchorNode = li.SelectSingleNode("a");
                            partGroupName = anchorNode.InnerText;
                            partGroupUrl = DIGIKEYHOMEURL + anchorNode.Attributes["href"].Value;

                            PartGroup partGroup = new PartGroup(partGroupId.ToString(), partGroupName, partGroupUrl);
                            HtmlDocument partGroupHtmlDoc = Common.Common.RetryRequest(partGroupUrl);

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

                                    HtmlDocument partHtmlDoc = Common.Common.RetryRequest(partUrl);
                                    HtmlNodeCollection productSpecList = null;

                                    if (partHtmlDoc != null)
                                    {
                                        HtmlNode packingNode = partHtmlDoc.DocumentNode.SelectSingleNode(partDetailXpath);
                                        packing = packingNode.InnerText;
                                        productSpecList = partHtmlDoc.DocumentNode.SelectNodes(productSpecXpath);
                                    }
                                    else
                                    {
                                        packing = "";
                                    }

                                    Part part = new Part(partId, manufacturer, partUrl, description, zoomImageUrl,
                                        imageUrl, datasheetUrl, packing);

                                    if (productSpecList != null)
                                    {
                                        foreach (HtmlNode node in productSpecList)
                                        {
                                            productSpecName = node.SelectSingleNode("th").InnerText;
                                            productSpecContent = node.SelectSingleNode("td").InnerText;

                                            if (!productSpecName.Contains(BAOZHUANG))
                                            {
                                                part.ProductSpecifications.Add(new ProductSpecification(productSpecName, productSpecContent));
                                            }
                                        }
                                    }

                                    //Todo: add price information to part after 2015-04-10

                                    partGroup.Parts.Add(part);
                                    break;
                                }
                            }
                            else
                            {
                                //Todo: add the partGroupUrl to log file
                            }

                            widget.PartGroups.Add(partGroup);
                            partGroupId++;
                            break;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Prepare scraped data
        /// </summary>
        private void PrepareScrapedData()
        {
            foreach (Category category in supplier.Categories)
            {
                XElement xeCategory = new XElement("cat", new XAttribute("id", category.Id),
                                                                    new XAttribute("n", category.Name.TrimStart()),
                                                                    new XElement("subcats"));
                scrapedData.Element("cats").Add(xeCategory);

                foreach (SubCategory subCategory in category.SubCategories)
                {
                    XElement xeSubCategory = new XElement("subcat", new XAttribute("id", subCategory.Id),
                                                                    new XAttribute("n", subCategory.Name.TrimStart()),
                                                                    new XElement("wgts"));
                    xeCategory.Element("subcats").Add(xeSubCategory);

                    foreach (Widget widget in subCategory.Widgets)
                    {
                        XElement xeWidget = new XElement("wgt", new XAttribute("id", widget.Id),
                                                                new XAttribute("n", widget.Name),
                                                                new XElement("pgs"));
                        xeSubCategory.Element("wgts").Add(xeWidget);

                        foreach (PartGroup partGroup in widget.PartGroups)
                        {
                            XElement xePartGroup = new XElement("pg", new XAttribute("id", partGroup.Id),
                                                                      new XAttribute("n", partGroup.Name),
                                                                      new XElement("ps"));
                            xeWidget.Element("pgs").Add(xePartGroup);

                            foreach (Part part in partGroup.Parts)
                            {
                                XElement xePart = new XElement("p", new XAttribute("id", part.Id),
                                                                    new XAttribute("mft", part.Manufacturer),
                                                                    new XAttribute("url", part.Url),
                                                                    new XAttribute("des", part.Description),
                                                                    new XAttribute("zoo", StringHelpers.GetLastDirectory(part.ZoomImageUrl)),
                                                                    new XAttribute("img", StringHelpers.GetLastDirectory(part.ImageUrl)),
                                                                    new XAttribute("ds", StringHelpers.GetLastDirectory(part.DatasheetUrl)),
                                                                    new XAttribute("pack", part.Packing.TrimStart().TrimEnd()));

                                //Todo: add price information to part after 2015-04-10
                                xePartGroup.Element("ps").Add(xePart);
                                partCount++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Init category data tables
        /// </summary>
        private void InitCategoryDataTables()
        {
            for (int i = 0; i < 4; i++)
            {
                DataTable dt = new DataTable();
                dt = new DataTable();
                dt.Columns.Add("GUID", typeof(System.Data.SqlTypes.SqlGuid));
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("ParentID", typeof(string));
                dt.Columns.Add("Comment", typeof(string));
                categoryDataTables.Add(dt);
            }
        }

        private void PrepareCategoryDataTables()
        {
            SqlGuid guid0;
            SqlGuid guid1;
            SqlGuid guid2;
            SqlGuid guid3;
            foreach (XElement category in scrapedData.XPathSelectElements("cats/cat"))
            {
                guid0 = (SqlGuid)System.Guid.NewGuid();
                AddRow(categoryDataTables[0], category, guid0, "");

                foreach(XElement subCategory in category.XPathSelectElements("subcats/subcat"))
                {
                    guid1 = (SqlGuid)System.Guid.NewGuid();
                    AddRow(categoryDataTables[1], subCategory, guid1, guid0.ToString());

                    foreach(XElement widget in category.XPathSelectElements("wgts/wgt"))
                    {
                        guid2 = (SqlGuid)System.Guid.NewGuid();
                        AddRow(categoryDataTables[2], widget, guid2, guid1.ToString());

                        foreach(XElement partGroup in category.XPathSelectElements("pgs/pg"))
                        {
                            guid3 = (SqlGuid)System.Guid.NewGuid();
                            AddRow(categoryDataTables[3], partGroup, guid3, guid2.ToString());
                        }
                    }
                }
            }
        }

        private void AddRow(DataTable dt, XElement type, SqlGuid currentGuid, string parentId)
        {
            DataRow dr = dt.NewRow();
            dr["GUID"] = currentGuid;
            dr["Name"] = XmlHelpers.GetAttribute(type, "n");
            dr["ParentID"] = parentId.ToUpper();
            dr["Comment"] = "";
            dt.Rows.Add(dr);
        }

        private void InsertDataToDatabase()
        {
            for (int i = 0; i < 4; i++)
            {
                DataCenter.InsertDataToDatabase(categoryDataTables[i]);
            }            
        }
    }
}
