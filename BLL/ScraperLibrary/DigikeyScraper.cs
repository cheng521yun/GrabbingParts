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
        private const string XIANGGUANCHANPIN = "相关产品";
        private const int productSpecContentLength = 64;        
        private const string ftpServerAddress = "ftp://120.25.220.49/";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("WXH");
        private Dictionary<string, SqlGuid> manufacturerDictionary = new Dictionary<string, SqlGuid>();
        private Object obj = new Object();
        public static HtmlDocument baseHtmlDoc = new HtmlDocument();

        public override void ScrapePage()
        {
            Stopwatch sw = Stopwatch.StartNew();
            log.Debug("Before the method GetBaseHtmlDocument.");

            GetBaseHtmlDocument();

            sw.Stop();
            log.InfoFormat("GetBaseHtmlDocument finish.cost:{0}ms", sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetCategory();

            InsertDataToSupplier();

            sw.Stop();
            log.InfoFormat("GetPartGroup finish.cost:{0}ms", sw.ElapsedMilliseconds);
        }

        private void GetBaseHtmlDocument()
        {
            baseHtmlDoc = Common.Common.RetryRequest(DIGIKEYHOMEURL);
        }

        /// <summary>
        /// Get part group, use task in Category level
        /// </summary>
        private void GetCategory()
        {
            HandleCategory("半导体产品", "1");
            HandleCategory("无源元件", "2");
            HandleCategory("互连产品", "3");
            HandleCategory("机电产品", "4");
            HandleCategory("光电元件", "5");
        }

        private void HandleCategory(string categoryName, string categoryIndex)
        {
            Category category = new Category(categoryIndex, categoryName);
            
            Stopwatch sw = Stopwatch.StartNew();
            GetSubCategoryAndWidget(category, categoryIndex);

            sw.Stop();
            log.InfoFormat("GetSubCategoryAndWidget for category {0} finish.cost:{1}ms", categoryName, sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            GetPartGroup(category);

            sw.Stop();
            log.InfoFormat("GetPartGroupWithTask for category {0} finish.cost:{0}ms", categoryIndex, sw.ElapsedMilliseconds);

            sw = Stopwatch.StartNew();
            XElement scrapedData = PrepareScrapedData(category);

            sw.Stop();
            log.InfoFormat("PrepareScrapedData for category {0} finish.cost:{0}ms", categoryIndex, sw.ElapsedMilliseconds);

            SaveScrapedData(scrapedData, categoryIndex);

            List<DataTable> categoryDataTables = new List<DataTable>();

            InitCategoryDataTables(categoryDataTables);

            InsertCategoryToDatabase(scrapedData, categoryDataTables);

            PrepareDataTables(scrapedData);

            log.InfoFormat("Task for category {0} finished.", categoryName);
        }

        private void InsertCategoryToDatabase(XElement scrapedData, List<DataTable> categoryDataTables)
        {
            SqlGuid guid0;
            SqlGuid guid1;
            SqlGuid guid2;
            SqlGuid guid3;
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
                        if (XmlHelpers.GetAttribute(partGroup, "n") != "PartGroup")
                        {
                            guid3 = (SqlGuid)System.Guid.NewGuid();
                            AddRow(categoryDataTables[3], partGroup, guid3, guid2.ToString());
                        }
                    }
                }
            }

            DataCenter.InsertDataToCategory(categoryDataTables);
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
        /// Use task in SubCategory level
        /// </summary>
        private void GetPartGroup(Category category)
        {
            foreach (SubCategory subCategory in category.SubCategories)
            {
                foreach (Widget widget in subCategory.Widgets)
                {
                    GetPartGroupForWidget(widget);
                }
            }
        }

        private void SaveScrapedData(XElement scrapedData, string categoryIndex)
        {
            try
            {
                //Todo: remove no-used data

                string fileName = string.Format("Category_{0}.xml", categoryIndex);
                scrapedData.Save(@"c:\" + fileName);
            }
            catch(Exception ex)
            {
                log.Error("Error in SaveScrapedData", ex);
            }
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
            log.InfoFormat("Task InsertDataToSupplier finished, cost{0}ms.", sw.ElapsedMilliseconds);
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

                //AddParts(partGroup, widget.Url);

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

                            //AddParts(partGroup, partsUrl);

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
                        zoomImageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "zoomimg");
                        imageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "src");
                        datasheetUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='rd-datasheet']/center/a"), "href");

                        HtmlDocument partHtmlDoc = Common.Common.RetryRequest(partUrl);
                        HtmlNodeCollection productSpecList = null;

                        if (partHtmlDoc != null)
                        {
                            packing = XmlHelpers.GetText(partHtmlDoc.DocumentNode, partDetailXpath);
                            productSpecList = partHtmlDoc.DocumentNode.SelectNodes(productSpecXpath);                            
                        }
                        else
                        {
                            packing = "";
                        }

                        if(partId != "" && manufacturer != "")
                        {
                            CheckFieldLength(ref partId, ref manufacturer, ref description, ref packing);

                            Part part = new Part(partId, manufacturer, partUrl, description, zoomImageUrl,
                            imageUrl, datasheetUrl, packing);

                            //UpdateFileToFTP(imageUrl, partId, ".jpg");
                            //UpdateFileToFTP(zoomImageUrl, partId, "_z.jpg");
                            //UpdateFileToFTP(datasheetUrl, partId, ".pdf");

                            if (productSpecList != null)
                            {
                                foreach (HtmlNode node in productSpecList)
                                {
                                    productSpecName = XmlHelpers.GetText(node, "th");
                                    productSpecContent = XmlHelpers.GetText(node, "td");

                                    if (!productSpecName.Contains(BAOZHUANG) && !productSpecName.Contains(XIANGGUANCHANPIN))
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

        private void CheckFieldLength(ref string partId, ref string manufacturer, ref string description,
                                      ref string packing)
        {
            if (partId.Length > 64)
            {
                partId = partId.Substring(0, 64);
            }

            if (manufacturer.Length > 64)
            {
                manufacturer = manufacturer.Substring(0, 64);
            }

            if (description.Length > 64)
            {
                description = description.Substring(0, 64);
            }

            if (packing.Length > 64)
            {
                packing = packing.Substring(0, 64);
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
                                                                    new XAttribute("guid", System.Guid.NewGuid().ToString()),
                                                                    new XElement("subcats"));
            result.Add(xeCategory);

            foreach (SubCategory subCategory in category.SubCategories)
            {
                XElement xeSubCategory = new XElement("subcat", new XAttribute("id", subCategory.Id),
                                                                new XAttribute("n", subCategory.Name.TrimStart()),
                                                                new XAttribute("guid", System.Guid.NewGuid().ToString()),
                                                                new XElement("wgts"));
                xeCategory.Element("subcats").Add(xeSubCategory);

                foreach (Widget widget in subCategory.Widgets)
                {
                    XElement xeWidget = new XElement("wgt", new XAttribute("id", widget.Id),
                                                            new XAttribute("n", widget.Name),
                                                            new XAttribute("url", widget.Url),
                                                            new XAttribute("guid", System.Guid.NewGuid().ToString()),
                                                            new XElement("pgs"));
                    xeSubCategory.Element("wgts").Add(xeWidget);

                    foreach (PartGroup partGroup in widget.PartGroups)
                    {
                        XElement xePartGroup = new XElement("pg", new XAttribute("id", partGroup.Id),
                                                                  new XAttribute("n", partGroup.Name),
                                                                  new XAttribute("url", partGroup.Url),
                                                                  new XAttribute("guid", System.Guid.NewGuid().ToString()),
                                                                  new XElement("ps"));
                        xeWidget.Element("pgs").Add(xePartGroup);

                        /*foreach (Part part in partGroup.Parts)
                        {
                            XElement xePart = new XElement("p", new XAttribute("id", part.Id),
                                                                new XAttribute("mft", part.Manufacturer),
                                                                new XAttribute("url", part.Url),
                                                                new XAttribute("des", part.Description),
                                                                new XAttribute("zoo", part.ZoomImageUrl),
                                                                new XAttribute("img", part.ImageUrl),
                                                                new XAttribute("ds", part.DatasheetUrl),
                                                                new XAttribute("pack", part.Packing.TrimStart().TrimEnd()));

                            foreach (ProductSpecification ps in part.ProductSpecifications)
                            {
                                xePart.Add(new XElement("s", new XAttribute("n", ps.Name),
                                                             new XAttribute("v", ps.Content)));
                            }

                            //Todo: add price information to part after 2015-04-10
                            xePartGroup.Element("ps").Add(xePart);
                        }*/
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Init category data tables
        /// </summary>
        private void InitCategoryDataTables(List<DataTable> categoryDataTables)
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
        }

        /// <summary>
        /// Init category data tables
        /// </summary>
        private void InitDataTables(DataTable productSpecDataTable, DataTable manufacturerDataTable, DataTable productInfoDataTable)
        {
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

        private void PrepareDataTables(XElement scrapedData)
        {            
            SqlGuid guid1;
            SqlGuid guid2;
            string partsUrl;
            XElement category = scrapedData.Element("cat");
            SqlGuid guid0 = new SqlGuid(XmlHelpers.GetAttribute(category, "guid"));

            foreach (XElement subCategory in category.XPathSelectElements("subcats/subcat"))
            {
                guid1 = new SqlGuid(XmlHelpers.GetAttribute(subCategory, "guid"));

                foreach (XElement widget in subCategory.XPathSelectElements("wgts/wgt"))
                {
                    guid2 = new SqlGuid(XmlHelpers.GetAttribute(widget, "guid"));
                    foreach (XElement partGroup in widget.XPathSelectElements("pgs/pg"))
                    {
                        partsUrl = XmlHelpers.GetAttribute(partGroup, "url");
                        AddParts2(partGroup, guid0, guid1, guid2, partsUrl);
                    }
                }
            }
        }

        private void AddParts2(XElement partGroup, SqlGuid guid0, SqlGuid guid1, SqlGuid guid2, string partsUrl, int currentPage = 1)
        {
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
            SqlGuid guid3;
            SqlGuid manufacturerGuid;
            bool hasPartGroup = (XmlHelpers.GetAttribute(partGroup, "n") != "PartGroup");

            if (hasPartGroup)
            {
                guid3 = new SqlGuid(XmlHelpers.GetAttribute(partGroup, "guid"));
            }
            else
            {
                guid3 = new SqlGuid();
            }

            HtmlDocument partsHtmlDoc = Common.Common.RetryRequest(AddPageSizeToPartsUrl(partsUrl));
            if (partsHtmlDoc != null)
            {
                HtmlNodeCollection trList = partsHtmlDoc.DocumentNode.SelectNodes(partXpath);
                if (trList != null)
                {
                    DataTable productSpecDataTable = new DataTable();
                    DataTable manufacturerDataTable = new DataTable();
                    DataTable productInfoDataTable = new DataTable();

                    InitDataTables(productSpecDataTable, manufacturerDataTable, productInfoDataTable);

                    foreach (HtmlNode tr in trList)
                    {
                        partId = XmlHelpers.GetText(tr, "td[@class='mfg-partnumber']/a/span");
                        partUrl = DIGIKEYHOMEURL + XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='mfg-partnumber']/a"), "href");
                        manufacturer = XmlHelpers.GetText(tr, "td[@class='vendor']/span//span");
                        description = XmlHelpers.GetText(tr, "td[@class='description']");
                        zoomImageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "zoomimg");
                        imageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "src");
                        datasheetUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='rd-datasheet']/center/a"), "href");

                        HtmlDocument partHtmlDoc = Common.Common.RetryRequest(partUrl);
                        HtmlNodeCollection productSpecList = null;

                        if (partHtmlDoc != null)
                        {
                            packing = XmlHelpers.GetText(partHtmlDoc.DocumentNode, partDetailXpath);
                            productSpecList = partHtmlDoc.DocumentNode.SelectNodes(productSpecXpath);
                        }
                        else
                        {
                            packing = "";
                        }

                        if (partId != "" && manufacturer != "")
                        {
                            CheckFieldLength(ref partId, ref manufacturer, ref description, ref packing);
                            DataRow drProductInfo = productInfoDataTable.NewRow();
                            drProductInfo["GUID"] = (SqlGuid)System.Guid.NewGuid();
                            drProductInfo["PN"] = partId;
                            drProductInfo["Manufacturer"] = manufacturer;

                            lock (obj)
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

                            drProductInfo["Description"] = description;
                            drProductInfo["Packing"] = packing;
                            drProductInfo["Type1"] = guid0.ToString().ToUpper();
                            drProductInfo["Type2"] = guid1.ToString().ToUpper();
                            drProductInfo["Type3"] = guid2.ToString().ToUpper();
                            drProductInfo["Type4"] = hasPartGroup ? guid3.ToString().ToUpper() : "";
                            drProductInfo["Datasheets"] = (datasheetUrl != "" ? drProductInfo["PN"] + ".pdf" : "");
                            drProductInfo["Image"] = (imageUrl != "" ? drProductInfo["PN"] + ".jpg" : "");
                            productInfoDataTable.Rows.Add(drProductInfo);
                            
                            LogFtpFileUrls(partId, datasheetUrl, imageUrl, zoomImageUrl);

                            if (productSpecList != null)
                            {
                                foreach (HtmlNode node in productSpecList)
                                {
                                    productSpecName = XmlHelpers.GetText(node, "th");
                                    productSpecContent = XmlHelpers.GetText(node, "td");

                                    if (!productSpecName.Contains(BAOZHUANG) && !productSpecName.Contains(XIANGGUANCHANPIN))
                                    {
                                        if (productSpecContent.Length > productSpecContentLength)
                                        {
                                            productSpecContent = productSpecContent.Substring(0, productSpecContentLength);
                                        }

                                        DataRow dr = productSpecDataTable.NewRow();
                                        dr["GUID"] = (SqlGuid)System.Guid.NewGuid();
                                        dr["PN"] = partId;
                                        dr["Name"] = productSpecName;
                                        dr["Content"] = productSpecContent;
                                        productSpecDataTable.Rows.Add(dr);
                                    }
                                }
                            }
                        }
                    }

                    DataCenter.ExecuteTransaction(productSpecDataTable, manufacturerDataTable, productInfoDataTable);

                    HtmlNode currentPageNode = partsHtmlDoc.DocumentNode.SelectSingleNode(currentPageXpath);
                    string currentPageValue = XmlHelpers.GetText(currentPageNode);
                    string tmpTotalPage = StringHelpers.GetLastDirectory(currentPageValue);
                    int totalPage = 0;
                    Int32.TryParse(tmpTotalPage, out totalPage);

                    if (totalPage > currentPage)
                    {
                        List<System.Threading.Tasks.Task> tasks = new List<System.Threading.Tasks.Task>();

                        for (int nextPage = currentPage + 1; nextPage <= totalPage; nextPage++)
                        {
                            tasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() => AddPartWithTask(partGroup, partsUrl, currentPage, nextPage, guid0, guid1, guid2)));
                        }

                        Task.WaitAll(tasks.ToArray());
                    }
                }
                else
                {
                    //Todo: add the partGroupUrl to log file
                }
            }
        }

        private void AddPartWithTask(XElement partGroup, string partsUrl, int currentPage, int nextPage, 
                                     SqlGuid guid0, SqlGuid guid1, SqlGuid guid2)
        {
            string nextPageUrl;
            if (partsUrl.IndexOf("/page/") > 0)
            {
                nextPageUrl = partsUrl.Replace("/page/" + currentPage.ToString(), "/page/" + nextPage.ToString());
            }
            else
            {
                nextPageUrl = partsUrl + "/page/" + nextPage.ToString();
            }

            AddParts3(partGroup, guid0, guid1, guid2, nextPageUrl);
        }

        private void AddParts3(XElement partGroup, SqlGuid guid0, SqlGuid guid1, SqlGuid guid2, string partsUrl)
        {
            string partXpath = "//table[@id='productTable']//tbody/tr";
            string partDetailXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/table[@class='product-additional-info']/tr/td[@class='attributes-table-main']/table/tr[5]/td";
            string productSpecXpath = "/html[@id='responsiveTemplate']/body[@class='ltr']/div[@id='content']/table[@class='product-additional-info']/tr/td[@class='attributes-table-main']/table/tr";
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
            SqlGuid guid3;
            SqlGuid manufacturerGuid;
            bool hasPartGroup = (XmlHelpers.GetAttribute(partGroup, "n") != "PartGroup");

            if (hasPartGroup)
            {
                guid3 = new SqlGuid(XmlHelpers.GetAttribute(partGroup, "guid"));
            }
            else
            {
                guid3 = new SqlGuid();
            }

            HtmlDocument partsHtmlDoc = Common.Common.RetryRequest(AddPageSizeToPartsUrl(partsUrl));
            if (partsHtmlDoc != null)
            {
                HtmlNodeCollection trList = partsHtmlDoc.DocumentNode.SelectNodes(partXpath);
                if (trList != null)
                {
                    DataTable productSpecDataTable = new DataTable();
                    DataTable manufacturerDataTable = new DataTable();
                    DataTable productInfoDataTable = new DataTable();

                    InitDataTables(productSpecDataTable, manufacturerDataTable, productInfoDataTable);

                    foreach (HtmlNode tr in trList)
                    {
                        partId = XmlHelpers.GetText(tr, "td[@class='mfg-partnumber']/a/span");
                        partUrl = DIGIKEYHOMEURL + XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='mfg-partnumber']/a"), "href");
                        manufacturer = XmlHelpers.GetText(tr, "td[@class='vendor']/span//span");
                        description = XmlHelpers.GetText(tr, "td[@class='description']");
                        zoomImageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "zoomimg");
                        imageUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='image']/a/img"), "src");
                        datasheetUrl = XmlHelpers.GetAttribute(tr.SelectSingleNode("td[@class='rd-datasheet']/center/a"), "href");

                        HtmlDocument partHtmlDoc = Common.Common.RetryRequest(partUrl);
                        HtmlNodeCollection productSpecList = null;

                        if (partHtmlDoc != null)
                        {
                            packing = XmlHelpers.GetText(partHtmlDoc.DocumentNode, partDetailXpath);
                            productSpecList = partHtmlDoc.DocumentNode.SelectNodes(productSpecXpath);
                        }
                        else
                        {
                            packing = "";
                        }

                        if (partId != "" && manufacturer != "")
                        {
                            CheckFieldLength(ref partId, ref manufacturer, ref description, ref packing);
                            DataRow drProductInfo = productInfoDataTable.NewRow();
                            drProductInfo["GUID"] = (SqlGuid)System.Guid.NewGuid();
                            drProductInfo["PN"] = partId;
                            drProductInfo["Manufacturer"] = manufacturer;

                            lock (obj)
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

                            drProductInfo["Description"] = description;
                            drProductInfo["Packing"] = packing;
                            drProductInfo["Type1"] = guid0.ToString().ToUpper();
                            drProductInfo["Type2"] = guid1.ToString().ToUpper();
                            drProductInfo["Type3"] = guid2.ToString().ToUpper();
                            drProductInfo["Type4"] = hasPartGroup ? guid3.ToString().ToUpper() : "";
                            drProductInfo["Datasheets"] = (datasheetUrl != "" ? drProductInfo["PN"] + ".pdf" : "");
                            drProductInfo["Image"] = (imageUrl != "" ? drProductInfo["PN"] + ".jpg" : "");
                            productInfoDataTable.Rows.Add(drProductInfo);

                            LogFtpFileUrls(partId, datasheetUrl, imageUrl, zoomImageUrl);

                            if (productSpecList != null)
                            {
                                foreach (HtmlNode node in productSpecList)
                                {
                                    productSpecName = XmlHelpers.GetText(node, "th");
                                    productSpecContent = XmlHelpers.GetText(node, "td");

                                    if (!productSpecName.Contains(BAOZHUANG) && !productSpecName.Contains(XIANGGUANCHANPIN))
                                    {
                                        if (productSpecContent.Length > productSpecContentLength)
                                        {
                                            productSpecContent = productSpecContent.Substring(0, productSpecContentLength);
                                        }

                                        DataRow dr = productSpecDataTable.NewRow();
                                        dr["GUID"] = (SqlGuid)System.Guid.NewGuid();
                                        dr["PN"] = partId;
                                        dr["Name"] = productSpecName;
                                        dr["Content"] = productSpecContent;
                                        productSpecDataTable.Rows.Add(dr);
                                    }
                                }
                            }
                        }
                    }

                    DataCenter.ExecuteTransaction(productSpecDataTable, manufacturerDataTable, productInfoDataTable);
                }
                else
                {
                    //Todo: add the partGroupUrl to log file
                }
            }
        }

        private void LogFtpFileUrls(string partId, string datasheetUrl, string imageUrl, string zoomImageUrl)
        {
            //For FTP files later
            if (datasheetUrl != "")
            {
                log.InfoFormat("DS Url:{0},{1}", datasheetUrl, partId);
            }

            if (imageUrl != "")
            {
                log.InfoFormat("Image Url:{0},{1}", imageUrl, partId);
            }

            if (zoomImageUrl != "")
            {
                log.InfoFormat("Zoom Image Url:{0},{1}", zoomImageUrl, partId);
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
