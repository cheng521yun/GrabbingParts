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
        private const int manufacturerLength = 32;
        private const int productSpecContentLength = 64;
        private const int descriptionLength = 64;
        private const int packingLength = 64;
        private const string ftpServerAddress = "ftp://120.25.220.49/";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<string, SqlGuid> manufacturerDictionary = new Dictionary<string, SqlGuid>();
        private Object obj = new Object();
        public static HtmlDocument baseHtmlDoc = new HtmlDocument();

        public override void ScrapePage()
        {
            Stopwatch sw = Stopwatch.StartNew();
            log.Debug("Before the method GetBaseHtmlDocument.");

            GetBaseHtmlDocument();

            sw.Stop();
            log.DebugFormat("GetBaseHtmlDocument finish.cost:{0}ms", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetPartGroup();

            sw.Stop();
            log.DebugFormat("GetPartGroup finish.cost:{0}ms", sw.ElapsedMilliseconds);
        }

        private void GetBaseHtmlDocument()
        {
            baseHtmlDoc = Common.Common.RetryRequest(DIGIKEYHOMEURL);
        }

        /// <summary>
        /// Get sub category and widget
        /// </summary>
        private void GetSubCategoryAndWidget(Category category, string categoryIndex)
        {
            string xpathForSubCategory = "/html/body/div[@id='wrapper']/div[@id='top_categories']/div[@id='topnav']/table[@id='bottomrow']/tr/td[a][" + categoryIndex + "]";
            HtmlNode uiSubCategory = baseHtmlDoc.DocumentNode.SelectSingleNode(xpathForSubCategory);
            string subCategoryId;
            string subCategoryName;
            int widgetId = 1;
            string widgetName;
            string widgetUrl;
            HtmlNode anchorNode;

            HtmlNodeCollection uiAnchorList = uiSubCategory.SelectNodes("a[@id!='viewall-link']");
            foreach (HtmlNode uiAnchor in uiAnchorList)
            {
                subCategoryId = XmlHelpers.GetAttribute(uiAnchor, "id");
                subCategoryName = XmlHelpers.GetText(uiAnchor);
                SubCategory subCategory = new SubCategory(subCategoryId, subCategoryName);
                HtmlNode ul = uiAnchor.NextSibling.NextSibling;
                HtmlNodeCollection liList = ul != null ? ul.SelectNodes("li") : null;
                widgetId = 1;

                if (liList != null)
                {
                    foreach (HtmlNode li in liList)
                    {
                        anchorNode = li.SelectSingleNode("a");
                        widgetName = XmlHelpers.GetText(anchorNode);
                        widgetUrl = XmlHelpers.GetAttribute(anchorNode, "href");
                        subCategory.Widgets.Add(new Widget(widgetId.ToString(), widgetName, widgetUrl));
                        widgetId++;
                    }
                }                

                category.SubCategories.Add(subCategory);
            }
        }

        /// <summary>
        /// Get part group, use task in Category level
        /// </summary>
        private void GetPartGroup()
        {
            Task getPartGroupForSemiconductorProducts = Task.Factory.StartNew(() => { HandleCategory("半导体产品", "1"); });
            Task getPartGroupForPassiveComponents = Task.Factory.StartNew(() => { HandleCategory("无源元件", "2"); });
            Task getPartGroupForInterconnectProducts = Task.Factory.StartNew(() => { HandleCategory("互连产品", "3"); });
            Task getPartGroupForMechanicalElectronicProducts = Task.Factory.StartNew(() => { HandleCategory("机电产品", "4"); });
            Task getPartGroupForPhotoelectricElement = Task.Factory.StartNew(() => { HandleCategory("光电元件", "5"); });
            Task insertDataToSupplier = Task.Factory.StartNew(() => { InsertDataToSupplier(); });

            Task[] taskList = { getPartGroupForSemiconductorProducts, getPartGroupForPassiveComponents, getPartGroupForInterconnectProducts,
                                getPartGroupForMechanicalElectronicProducts, getPartGroupForPhotoelectricElement, insertDataToSupplier};

            try
            {
                Task.WaitAll(taskList);
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten();
            }
        }

        private void HandleCategory(string categoryName, string categoryIndex)
        {
            Category category = new Category(categoryIndex, categoryName);
            
            Stopwatch sw = Stopwatch.StartNew();
            GetSubCategoryAndWidget(category, categoryIndex);

            sw.Stop();
            log.DebugFormat("GetSubCategoryAndWidget for category {0} finish.cost:{1}ms", categoryIndex, sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetPartGroupWithTask(category);

            sw.Stop();
            log.DebugFormat("GetPartGroupWithTask for category {0} finish.cost:{0}ms", categoryIndex, sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            XElement scrapedData = PrepareScrapedData(category);

            sw.Stop();
            log.DebugFormat("PrepareScrapedData for category {0} finish.cost:{0}ms", categoryIndex, sw.ElapsedMilliseconds);

            List<DataTable> categoryDataTables = new List<DataTable>();
            DataTable productSpecDataTable = new DataTable();
            DataTable manufacturerDataTable = new DataTable();
            DataTable productInfoDataTable = new DataTable();

            InitDataTables(categoryDataTables, productSpecDataTable, manufacturerDataTable, productInfoDataTable);

            PrepareDataTables(scrapedData, categoryDataTables, productSpecDataTable, manufacturerDataTable, productInfoDataTable);

            DataCenter.ExecuteTransaction(categoryDataTables, productSpecDataTable,
            manufacturerDataTable, productInfoDataTable);

            log.DebugFormat("Task for category {0} finished.", categoryName);
        }

        private void InsertDataToSupplier()
        {
            Stopwatch sw = Stopwatch.StartNew();

            DataTable supplierDataTable = new DataTable();
            supplierDataTable.Columns.Add("GUID", typeof(System.Data.SqlTypes.SqlGuid));
            supplierDataTable.Columns.Add("Supplier", typeof(string));
            supplierDataTable.Columns.Add("WebUrl", typeof(string));

            DataRow drSupplier = supplierDataTable.NewRow();
            drSupplier["GUID"] = (SqlGuid)System.Guid.NewGuid();
            drSupplier["Supplier"] = SUPPLIERNAME;
            drSupplier["WebUrl"] = DIGIKEYHOMEURL;
            supplierDataTable.Rows.Add(drSupplier);

            DataCenter.InsertDataToSupplier(supplierDataTable);

            sw.Stop();
            log.DebugFormat("Task InsertDataToSupplier finished, cost{0}ms.", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Use task in SubCategory level
        /// </summary>
        private void GetPartGroupWithTask(Category category)
        {
            List<Task> tasks = new List<Task>();

            foreach (SubCategory subCategory in category.SubCategories)
            {
                tasks.Add(Task.Factory.StartNew(() => GetPartGroupWithChildTask(subCategory)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Use task in Widget level
        /// </summary>
        private void GetPartGroupWithChildTask(SubCategory subCategory)
        {
            List<Task> tasks = new List<Task>();

            foreach (Widget widget in subCategory.Widgets)
            {
                tasks.Add(Task.Factory.StartNew(() => GetPartGroupForWidget(widget)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Get part group for each category
        /// </summary>
        /// <param name="categoryIndex"></param>
        private void GetPartGroupForWidget(Widget widget)
        {
            string partGroupXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/ul[@id='productIndexList']/li[@class='catfilteritem']/ul[@class='catfiltersub']/li";
            string partGroupName;
            string partsUrl;
            HtmlNode anchorNode;

            bool noPartGroup = StringHelpers.IsInteger(StringHelpers.GetLastDirectory(widget.Url));
            if (noPartGroup)
            {
                PartGroup partGroup = new PartGroup("1", "PartGroup", widget.Url);

                AddParts(partGroup, widget.Url);

                widget.PartGroups.Add(partGroup);
            }
            else
            {
                HtmlDocument widgetHtmlDoc = Common.Common.RetryRequest(widget.Url);
                if(widgetHtmlDoc != null)
                {
                    HtmlNodeCollection liList = widgetHtmlDoc.DocumentNode.SelectNodes(partGroupXpath);
                    if(liList != null)
                    {
                        int partGroupId = 1;

                        foreach (HtmlNode li in liList)
                        {
                            anchorNode = li.SelectSingleNode("a");
                            partGroupName = XmlHelpers.GetText(anchorNode);
                            partsUrl = DIGIKEYHOMEURL + XmlHelpers.GetAttribute(anchorNode, "href");

                            PartGroup partGroup = new PartGroup(partGroupId.ToString(), partGroupName, partsUrl);

                            AddParts(partGroup, partsUrl);

                            widget.PartGroups.Add(partGroup);
                            partGroupId++;
                        }
                    }           
                }                
            }
        }

        private void AddParts(PartGroup partGroup, string partsUrl, int currentPage = 1)
        {
            if (currentPage == 3) return;
            string partXpath = "//table[@id='productTable']//tbody/tr";
            string partDetailXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/table[@class='product-additional-info']/tr/td[@class='attributes-table-main']/table/tr[5]/td";
            string productSpecXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/table[@class='product-additional-info']/tr/td[@class='attributes-table-main']/table/tr";
            string currentPageXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/div[@class='paging'][1]/span[@class='current-page']";
            string partId;
            string partUrl;
            string manufacturer;
            string description;
            string zoomImageUrl;
            string imageUrl;
            string datasheetUrl;
            string packing;
            string productSpecName;
            string productSpecContent;

            HtmlDocument partsHtmlDoc = Common.Common.RetryRequest(AddPageSizeToPartsUrl(partsUrl));
            if(partsHtmlDoc != null)
            {
                HtmlNodeCollection trList = partsHtmlDoc.DocumentNode.SelectNodes(partXpath);

                if (trList != null)
                {
                    foreach (HtmlNode tr in trList)
                    {
                        partId = XmlHelpers.GetText(tr, "td[@class='mfg-partnumber']/a/span");
                        partUrl = DIGIKEYHOMEURL + XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='mfg-partnumber']/a"), "href");
                        manufacturer = XmlHelpers.GetText(tr, "td[@class='vendor']/span//span");
                        description = XmlHelpers.GetText(tr, "td[@class='description']");
                        if (description.Length > descriptionLength)
                        {
                            description = description.Substring(0, descriptionLength);
                        }

                        zoomImageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "zoomimg");
                        imageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "src");
                        datasheetUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='rd-datasheet']/center/a"), "href");

                        HtmlDocument partHtmlDoc = Common.Common.RetryRequest(partUrl);
                        HtmlNodeCollection productSpecList = null;

                        if (partHtmlDoc != null)
                        {
                            packing = XmlHelpers.GetText(partHtmlDoc.DocumentNode, partDetailXpath);
                            productSpecList = partHtmlDoc.DocumentNode.SelectNodes(productSpecXpath);
                            if (packing.Length > packingLength)
                            {
                                packing = packing.Substring(0, packingLength);
                            }
                        }
                        else
                        {
                            packing = "";
                        }

                        Part part = new Part(partId, manufacturer, partUrl, description, zoomImageUrl,
                            imageUrl, datasheetUrl, packing);

                        UpdateFileToFTP(imageUrl, partId, ".jpg");
                        UpdateFileToFTP(zoomImageUrl, partId, "_z.jpg");
                        UpdateFileToFTP(datasheetUrl, partId, ".pdf");

                        if (productSpecList != null)
                        {
                            foreach (HtmlNode node in productSpecList)
                            {
                                productSpecName = XmlHelpers.GetText(node, "th");
                                productSpecContent = XmlHelpers.GetText(node, "td");

                                if (!productSpecName.Contains(BAOZHUANG))
                                {
                                    if (productSpecContent.Length > productSpecContentLength)
                                    {
                                        productSpecContent = productSpecContent.Substring(0, productSpecContentLength);
                                    }
                                    part.ProductSpecifications.Add(new ProductSpecification(productSpecName, productSpecContent));
                                }
                            }
                        }

                        //Todo: add price information to part after 2015-04-10

                        partGroup.Parts.Add(part);
                    }

                    HtmlNode currentPageNode = partsHtmlDoc.DocumentNode.SelectSingleNode(currentPageXpath);
                    string currentPageValue = XmlHelpers.GetText(currentPageNode);
                    string tmpTotalPage = StringHelpers.GetLastDirectory(currentPageValue);
                    int totalPage = 0;
                    Int32.TryParse(tmpTotalPage, out totalPage);

                    if (totalPage > currentPage)
                    {
                        int nextPage = currentPage + 1;
                        string nextPageUrl;
                        if (partsUrl.IndexOf("/page/") > 0)
                        {
                            nextPageUrl = partsUrl.Replace("/page/" + currentPage.ToString(), "/page/" + nextPage.ToString());
                        }
                        else
                        {
                            nextPageUrl = partsUrl + "/page/" + nextPage.ToString();
                        }
                        AddParts(partGroup, nextPageUrl, nextPage);
                    }
                }
                else
                {
                    //Todo: add the partGroupUrl to log file
                }
            }            
        }

        private void UpdateFileToFTP(string targetAddress, string partId, string fileExtension)
        {
            if (targetAddress != "")
            {
                Task.Factory.StartNew(() => { Common.Common.UpFileToFTPAndGetFileBytes(targetAddress, ftpServerAddress + partId + fileExtension); });
            }
        }

        /// <summary>
        /// Add pagesize to parts url
        /// </summary>
        /// <param name="partsUrl"></param>
        /// <returns></returns>
        private string AddPageSizeToPartsUrl(string partsUrl)
        {
            if(partsUrl.IndexOf("?") > 0)
            {
                return partsUrl + "&pagesize=500";
            }
            else
            {
                return partsUrl + "?pagesize=500";
            }
        }

        /// <summary>
        /// Prepare scraped data
        /// </summary>
        private XElement PrepareScrapedData(Category category)
        {
            XElement result = new XElement("r");
            XElement xeCategory = new XElement("cat", new XAttribute("id", category.Id),
                                                                    new XAttribute("n", category.Name.TrimStart()),
                                                                    new XElement("subcats"));
            result.Add(xeCategory);

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
                                                                new XAttribute("ds", part.DatasheetUrl != "" ? (part.Id + ".pdf") : ""),
                                                                new XAttribute("pack", part.Packing.TrimStart().TrimEnd()));

                            foreach (ProductSpecification ps in part.ProductSpecifications)
                            {
                                xePart.Add(new XElement("s", new XAttribute("n", ps.Name),
                                                             new XAttribute("v", ps.Content)));
                            }

                            //Todo: add price information to part after 2015-04-10
                            xePartGroup.Element("ps").Add(xePart);
                            //partCount++;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Init category data tables
        /// </summary>
        private void InitDataTables(List<DataTable> categoryDataTables, DataTable productSpecDataTable,
            DataTable manufacturerDataTable, DataTable productInfoDataTable)
        {
            for (int i = 0; i < 4; i++)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("GUID", typeof(System.Data.SqlTypes.SqlGuid));
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("ParentID", typeof(string));
                dt.Columns.Add("Comment", typeof(string));
                categoryDataTables.Add(dt);
            }

            productSpecDataTable.Columns.Add("GUID", typeof(System.Data.SqlTypes.SqlGuid));
            productSpecDataTable.Columns.Add("PN", typeof(string));
            productSpecDataTable.Columns.Add("Name", typeof(string));
            productSpecDataTable.Columns.Add("Content", typeof(string));

            manufacturerDataTable.Columns.Add("GUID", typeof(System.Data.SqlTypes.SqlGuid));
            manufacturerDataTable.Columns.Add("Manufacturer", typeof(string));

            productInfoDataTable.Columns.Add("GUID", typeof(System.Data.SqlTypes.SqlGuid));
            productInfoDataTable.Columns.Add("PN", typeof(string));
            productInfoDataTable.Columns.Add("Manufacturer", typeof(string));
            productInfoDataTable.Columns.Add("ManufacturerID", typeof(System.Data.SqlTypes.SqlGuid));
            productInfoDataTable.Columns.Add("Description", typeof(string));
            productInfoDataTable.Columns.Add("Packing", typeof(string));
            productInfoDataTable.Columns.Add("Type1", typeof(string));
            productInfoDataTable.Columns.Add("Type2", typeof(string));
            productInfoDataTable.Columns.Add("Type3", typeof(string));
            productInfoDataTable.Columns.Add("Type4", typeof(string));
            productInfoDataTable.Columns.Add("Datasheets", typeof(string));
            productInfoDataTable.Columns.Add("Image", typeof(string));
        }

        private void PrepareDataTables(XElement scrapedData, List<DataTable> categoryDataTables, 
            DataTable productSpecDataTable, DataTable manufacturerDataTable, DataTable productInfoDataTable)
        {
            SqlGuid guid0;
            SqlGuid guid1;
            SqlGuid guid2;
            SqlGuid guid3;
            string manufacturer;
            SqlGuid manufacturerGuid;

            XElement category = scrapedData.Element("cat");

            guid0 = (SqlGuid)System.Guid.NewGuid();
            AddRow(categoryDataTables[0], category, guid0, "");

            foreach (XElement subCategory in category.XPathSelectElements("subcats/subcat"))
            {
                guid1 = (SqlGuid)System.Guid.NewGuid();
                AddRow(categoryDataTables[1], subCategory, guid1, guid0.ToString());

                foreach (XElement widget in subCategory.XPathSelectElements("wgts/wgt"))
                {
                    guid2 = (SqlGuid)System.Guid.NewGuid();
                    AddRow(categoryDataTables[2], widget, guid2, guid1.ToString());

                    foreach (XElement partGroup in widget.XPathSelectElements("pgs/pg"))
                    {
                        guid3 = (SqlGuid)System.Guid.NewGuid();
                        if (XmlHelpers.GetAttribute(partGroup, "n") != "PartGroup")
                        {
                            AddRow(categoryDataTables[3], partGroup, guid3, guid2.ToString());
                        }

                        foreach (XElement part in partGroup.XPathSelectElements("ps/p"))
                        {
                            manufacturer = XmlHelpers.GetAttribute(part, "mft");
                            if (manufacturer.Length > manufacturerLength)
                            {
                                manufacturer = manufacturer.Substring(0, manufacturerLength);
                            }
                            DataRow drProductInfo = productInfoDataTable.NewRow();
                            drProductInfo["GUID"] = (SqlGuid)System.Guid.NewGuid();
                            drProductInfo["PN"] = XmlHelpers.GetAttribute(part, "id");
                            drProductInfo["Manufacturer"] = manufacturer;

                            lock(obj)
                            {
                                if (!manufacturerDictionary.ContainsKey(manufacturer))
                                {
                                    manufacturerGuid = (SqlGuid)System.Guid.NewGuid();
                                    manufacturerDictionary.Add(manufacturer, manufacturerGuid);

                                    DataRow drManufacturer = manufacturerDataTable.NewRow();
                                    drManufacturer["GUID"] = manufacturerGuid;
                                    drManufacturer["Manufacturer"] = manufacturer;
                                    manufacturerDataTable.Rows.Add(drManufacturer);

                                    drProductInfo["ManufacturerID"] = manufacturerGuid;
                                }
                                else
                                {
                                    drProductInfo["ManufacturerID"] = manufacturerDictionary[manufacturer];
                                }
                            }                            

                            drProductInfo["Description"] = XmlHelpers.GetAttribute(part, "des");
                            drProductInfo["Packing"] = XmlHelpers.GetAttribute(part, "pack");
                            drProductInfo["Type1"] = guid0.ToString().ToUpper();
                            drProductInfo["Type2"] = guid1.ToString().ToUpper();
                            drProductInfo["Type3"] = guid2.ToString().ToUpper();
                            drProductInfo["Type4"] = guid3.ToString().ToUpper();
                            drProductInfo["Datasheets"] = XmlHelpers.GetAttribute(part, "ds");
                            drProductInfo["Image"] = XmlHelpers.GetAttribute(part, "img");
                            productInfoDataTable.Rows.Add(drProductInfo);

                            foreach (XElement spec in part.Elements("s"))
                            {
                                DataRow dr = productSpecDataTable.NewRow();
                                dr["GUID"] = (SqlGuid)System.Guid.NewGuid();
                                dr["PN"] = XmlHelpers.GetAttribute(part, "id");
                                dr["Name"] = XmlHelpers.GetAttribute(spec, "n");
                                dr["Content"] = XmlHelpers.GetAttribute(spec, "v");
                                productSpecDataTable.Rows.Add(dr);
                            }
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
    }
}
