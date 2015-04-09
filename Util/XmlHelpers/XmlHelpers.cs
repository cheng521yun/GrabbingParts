using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using Microsoft.Practices.EnterpriseLibrary.Caching;
using Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;
using Mvp.Xml.Common.XPath;

namespace GrabbingParts.Util.XmlHelpers
{
    /// <summary>
    /// Xml helper functions
    /// </summary>
    public static class XmlHelpers
    {
        private static log4net.ILog cacheLog = log4net.LogManager.GetLogger("CacheItemEventLogger");

        // cache of XSL compiled transforms
        private static ICacheManager mxslCache;
        private static ICacheManager xslCache
        {
            get
            {
                if (mxslCache == null)
                {
                    mxslCache = CacheFactory.GetCacheManager("Xsl");
                }
                return mxslCache;
            }
            set
            {
                mxslCache = value;
            }
        }

        /// <summary>
        /// Default XML namespace
        /// </summary>
        public static string DefaultNamespace { get { return "http://www.w3.org/XML/1998/namespace"; } }

        /// <summary>
        /// Get the application configured Config directory.
        /// </summary>
        /// <returns>Full path of application xsl directory.</returns>
        public static string ConfigDir()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "App_Data\\Config");
        }

        /// <summary>
        /// Create an XPathDocument from an xml string.
        /// </summary>
        /// <param name="xml">Xml data.</param>
        /// <returns>XPathDocument containing the xml data.</returns>
        public static XPathDocument LoadXmlXPath(string xml)
        {
            return new XPathDocument(new XmlTextReader(new StringReader(xml)));
        }

        /// <summary>
        /// Create an XmlDocument from an xml string.
        /// </summary>
        /// <param name="xml">Xml data.</param>
        /// <returns>XmlDocument containing the xml data.</returns>
        public static XmlDocument LoadXml(string xml)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xml);
            return dom;
        }

        /// <summary>
        /// Create an XDocument from an xml string.
        /// </summary>
        /// <param name="xml">Xml data.</param>
        /// <returns>XDocument containing the xml data.</returns>
        public static XDocument LoadXmlXDocument(string xml)
        {
            return XDocument.Load(new XmlTextReader(new StringReader(xml)));
        }

        /// <summary>
        /// Create an XElement from an xml string.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static XElement Parse(string xml)
        {
            return XElement.Load(XmlReader.Create(new StringReader(xml)));
        }

        /// <summary>
        /// Create an XmlDocument from an xml string.
        /// </summary>
        /// <param name="reader">Xml data.</param>
        /// <returns>XmlDocument containing the xml data.</returns>
        public static XmlDocument LoadXml(XmlReader reader)
        {
            reader.MoveToContent();
            XmlDocument dom = new XmlDocument();
            dom.Load(reader);
            return dom;
        }

        /// <summary>
        /// Create an XmlDocument from an xml string.
        /// </summary>
        /// <param name="nav">Xml data.</param>
        /// <returns>XmlDocument containing the xml data.</returns>
        public static XmlDocument LoadXml(IXPathNavigable nav)
        {
            XmlDocument dom = new XmlDocument();
            dom.Load(nav.CreateNavigator().ReadSubtree());
            return dom;
        }

        /// <summary>
        /// Create an XmlDocument from an xml file.
        /// </summary>
        /// <param name="filename">Full path of xml file.</param>
        /// <returns>XmlDocument containing xml file contents.</returns>
        public static XmlDocument Load(string filename)
        {
            XmlDocument dom = new XmlDocument();
            dom.Load(filename);
            return dom;
        }

        /// <summary>
        /// Saves any xpathnavigable to a file
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="filename"></param>
        public static void Save(IXPathNavigable nav, string filename)
        {
            using (XmlWriter writer = XmlWriter.Create(filename))
            {
                nav.CreateNavigator().WriteSubtree(writer);
            }
        }

        /// <summary>
        /// Get text from an xml node.
        /// </summary>
        /// <param name="nav">Node to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetText(IXPathNavigable nav, string xpath)
        {
            return GetText(nav, xpath, string.Empty);
        }

        /// <summary>
        /// Set text to an xml node.
        /// </summary>
        /// <param name="nav">The xmlNode which you want set value to it. </param>
        /// <param name="text">The value you want to set to the xmlNode</param>
        public static void SetNodeValue(XmlNode node, string text)
        {
            if (node != null)
            {
                node.InnerText = text;
            }
        }
        /// <summary>
        /// Get text from an xml node.
        /// </summary>
        /// <param name="nav">Node to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <param name="defaultValue">default value</param>
        /// <returns>Node value or blank or default value if not found.</returns>
        public static string GetText(IXPathNavigable nav, string xpath, string defaultValue)
        {
            if (nav == null)
            {
                return (defaultValue != null ? defaultValue : "");
            }
            XPathNavigator node = nav.CreateNavigator().SelectSingleNode(xpath);
            return node != null ? node.Value : defaultValue != null ? defaultValue : "";
        }

        /// <summary>
        /// Get text separated with comma from an xml node that has mutiple child nodes .
        /// </summary>
        /// <param name="nav">Node to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetMutipleText(IXPathNavigable nav, string xpath, string targetXPath)
        {
            string mutiText = string.Empty;

            XPathNodeIterator iterator = nav.CreateNavigator().Select(xpath);

            foreach (XPathNavigator item in iterator)
            {
                mutiText += GetText(item, targetXPath) + ",";
            }

            return mutiText.TrimEnd(',');

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="xpath"></param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static string GetText(IXPathNavigable nav, string xpath, XmlNamespaceManager nsmgr)
        {
            XPathNavigator node = nav.CreateNavigator().SelectSingleNode(xpath, nsmgr);
            return node != null ? node.Value : "";
        }

        /// <summary>
        /// Get text from an xml node.
        /// </summary>
        /// <param name="elem">Node to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetText(XmlElement elem, string xpath)
        {
            XmlNode node = elem.SelectSingleNode(xpath);
            return node != null ? node.InnerText : "";
        }

        /// <summary>
        /// Get text from an attribute of a XElement.
        /// </summary>
        /// <param name="elem">Node to query.</param>
        /// <param name="attribID">name of the attribute.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetText(XElement elem, string xpath)
        {
            if (elem == null)
                return "";
            XPathNavigator node = elem.CreateNavigator().SelectSingleNode(xpath);
            return node != null ? node.Value : "";
        }

        /// <summary>
        /// Get text from an xml node.
        /// </summary>
        /// <param name="dom">Document to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetText(XmlDocument dom, string xpath)
        {
            XmlNode node = dom.SelectSingleNode(xpath);
            return node != null ? node.InnerText : "";
        }

        /// <summary>
        /// Get text from an xml node.
        /// </summary>
        /// <param name="elem">Element to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <param name="nsmgr">Namespace manager for document.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetText(XmlElement elem, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNode node = elem.SelectSingleNode(xpath, nsmgr);
            return node != null ? node.InnerText : "";
        }

        /// <summary>
        /// Get text from an xml node.
        /// </summary>
        /// <param name="dom">Document to query.</param>
        /// <param name="xpath">XPath query to find node.</param>
        /// <param name="nsmgr">Namespace manager for document.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetText(XmlDocument dom, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNode node = dom.SelectSingleNode(xpath, nsmgr);
            return node != null ? node.InnerText : "";
        }

        /// <summary>
        /// Get text from an attribute of a XElement.
        /// </summary>
        /// <param name="elem">Node to query.</param>
        /// <param name="attribID">name of the attribute.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetAttribute(XElement elem, string attribName)
        {
            if (elem == null)
            {
                return "";
            }
            XAttribute attr = elem.Attribute(attribName);
            return attr != null ? attr.Value : "";
        }
        
        /// <summary>
        /// Get text from an XElement
        /// </summary>
        /// <param name="elem">Node to query.</param>
        /// <param name="elementName">name of the XElement.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetElementValue(XElement elem, string elementName, string defaultValue = "")
        {
            if (elem == null)
            {
                return (defaultValue != null ? defaultValue : "");
            }
            XElement element = elem.Element(elementName);
            return element != null ? element.Value : (defaultValue != null ? defaultValue : "");
        }

        /// <summary>
        /// Get text from an XElement with Namespace
        /// </summary>
        /// <param name="elem">Node to query.</param>
        /// <param name="elementName">name of the XElement.</param>
        /// <returns>Node value or blank if not found.</returns>
        public static string GetElementValueWithNamespace(XElement elem, string elementName, XNamespace ns = null)
        {
            if (elem == null)
            {
                return "";
            }
            XElement element = (ns == null ? elem.Element(elementName) : elem.Element(ns + elementName));
            return element != null ? element.Value : "";
        }


        /// <summary>
        /// Add or update an attribute.  Will remove attribute if blank value.
        /// </summary>
        /// <param name="elem">Element to update.</param>
        /// <param name="name">Attribute name to add/update.</param>
        /// <param name="attribValue">Attribute value, will remove attribute if blank.</param>
        public static void UpdateAttribute(XmlElement elem, string name, string attribValue)
        {
            if (elem != null)
            {
                if (attribValue == null || attribValue == "")
                    elem.RemoveAttribute(name);
                else
                    elem.SetAttribute(name, attribValue);
            }
        }

        /// <summary>
        /// Add an attribute.if exist, update the attribute.  Will remove the attribute if blank or null.
        /// </summary>
        /// <param name="elem">Element to update.</param>
        /// <param name="name">Attribute name to add/update.</param>
        /// <param name="attribValue">Attribute value, will remove attribute if blank.</param>
        public static void UpdateAttribute(XElement elem, string name, string attribValue)
        {
            if (elem != null)
            {
                elem.SetAttributeValue(name, attribValue);
            }
        }

        /// <summary>
        /// update the value of an element
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="elemValue">new value</param>
        public static void UpdateValue(XElement elem, string elemValue)
        {
            if (elem != null)
                elem.SetValue(elemValue);
        }

        /// <summary>
        /// Add an element do the node.
        /// </summary>
        /// <param name="node">Node to add element to.</param>
        /// <param name="name">Element name.</param>
        /// <param name="elemValue">Element value, may be blank for empty element.</param>
        /// <returns>Newly added element.</returns>
        public static XmlElement AddElement(XmlNode node, string name, string elemValue)
        {
            XmlElement child = node.OwnerDocument.CreateElement(name);
            child.InnerXml = elemValue;
            node.AppendChild(child);
            return child;
        }

        /// <summary>
        /// add an element to the node
        /// </summary>
        /// <param name="xel"></param>
        /// <param name="newElemName"></param>
        /// <param name="newElemValue"></param>
        /// <returns></returns>
        public static void AddElement(XElement xel, string newElemName, object newElemValue)
        {
            XElement newXel = xel.Element(newElemName);
            if (newXel == null)
            {
                xel.Add(new XElement(newElemName, newElemValue));
            }
            else
            {
                newXel.SetValue(newElemValue);
            }
        }

        /// <summary>
        /// Create an element and append as a child.
        /// </summary>
        /// <param name="parent">Parent of new element.</param>
        /// <param name="name">Element name.</param>
        /// <returns>Newly created element.</returns>
        public static XmlElement CreateAppendElement(XmlNode parent, string name)
        {
            XmlElement child = parent.OwnerDocument.CreateElement(name);
            parent.AppendChild(child);
            return child;
        }

        /// <summary>
        /// Copy a child element value from one element to another
        /// </summary>
        /// <param name="copyTo">Element to copy the child to</param>
        /// <param name="copyFrom">Element to copy the child from</param>
        /// <param name="childName">Child element name to copy value</param>
        public static void CopyChildElementValue(XElement copyTo, XElement copyFrom, string childName)
        {
            XElement elem = copyFrom.Element(childName);
            if (elem != null)
            {
                // create a new element so we don't reference a potentially large document
                XElement newElem = new XElement(childName);
                copyTo.Add(newElem);
                newElem.SetValue(elem.Value);
            }
        }

        /// <summary>
        /// Check if document has any content.
        /// </summary>
        /// <param name="nav">Document or fragment to inspect.</param>
        /// <returns>True if document has content.</returns>
        public static bool HasContent(IXPathNavigable nav)
        {
            //return nav != null && nav.CreateNavigator().MoveToFollowing(XPathNodeType.Element);

            // maybe there's a more optimal way to do this
            return nav != null && nav.CreateNavigator().OuterXml.Length > 0;
        }

        /// <summary>
        /// Gets nearest ancestor element with given name.
        /// </summary>
        /// <param name="elem">Element to start with.</param>
        /// <param name="name">Name of ancestor element.</param>
        /// <returns>Ancestor element or null if not found.</returns>
        public static XmlElement GetAncestorElement(XmlElement elem, string name)
        {
            XmlElement ancestorElem = elem.ParentNode is XmlElement ? (XmlElement)elem.ParentNode : null;
            while (ancestorElem != null && ancestorElem.Name != name)
                ancestorElem = ancestorElem.ParentNode is XmlElement ? (XmlElement)ancestorElem.ParentNode : null;

            return ancestorElem;
        }

        /// <summary>
        /// Get attribute in current element or nearest ancestor element.
        /// </summary>
        /// <param name="elem">Element to start with.</param>
        /// <param name="attribName">Attribute name to find.</param>
        /// <returns>Returns attribute value or empty string if not found.</returns>
        public static string GetCurrentOrAncestorAttribute(XmlElement elem, string attribName)
        {
            string retval = "";
            while (elem != null)
            {
                XmlNode node = elem.Attributes.GetNamedItem(attribName);
                if (node != null)
                {
                    retval = node.InnerText;
                    break;
                }

                elem = elem.ParentNode is XmlElement ? (XmlElement)elem.ParentNode : null;
            }

            return retval;
        }

        /// <summary>
        /// Get an xsl full path file name from the standard directory.
        /// </summary>
        /// <param name="filename">XSL file name</param>
        /// <returns></returns>
        public static string GetXslFilename(string filename)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/xslt/" + filename);
        }

       

        /// <summary>
        /// Loads an XmlReader into an XPathDocument, closes the XmlReader, and return IXPathNavigable interface.
        /// </summary>
        /// <param name="reader">Xml data to load.</param>
        /// <returns>XPathDocument containing data from reader.</returns>
        public static IXPathNavigable LoadXPathDocumentCloseReader(XmlReader reader)
        {
            XPathDocument doc;
            try
            {
                reader.MoveToContent();
                doc = new XPathDocument(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return doc;
        }

        /// <summary>
        /// Loads an XmlReader into an XmlDocument, closes the XmlReader, and return XmlDocument interface.
        /// </summary>
        /// <param name="reader">Xml data to load.</param>
        /// <returns>XmlDocument containing data from reader.</returns>
        public static XmlDocument LoadXmlDocumentCloseReader(XmlReader reader)
        {
            XmlDocument doc;
            try
            {
                doc = new XmlDocument();
                reader.MoveToContent();
                doc.Load(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return doc;
        }

        /// <summary>
        /// Loads an XmlReader into an XmlDocument, closes the XmlReader, and return XmlDocument interface.
        /// </summary>
        /// <param name="reader">Xml data to load.</param>
        /// <returns>XmlDocument containing data from reader.</returns>
        public static XDocument LoadXmlXDocumentCloseReader(XmlReader reader)
        {
            XDocument doc;
            try
            {
                reader.MoveToContent();
                doc = XDocument.Load(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return doc;
        }

        /// <summary>
        /// Loads an XmlReader into an XElement, closes the XmlReader, and return XElement interface.
        /// </summary>
        /// <param name="reader">Xml data to load.</param>
        /// <returns>XElement containing data from reader.</returns>
        public static XElement LoadXmlXElementCloseReader(XmlReader reader)
        {
            XElement doc;
            try
            {
                reader.MoveToContent();
                doc = XElement.Load(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return doc;
        }

        /// <summary>
        /// Creates an XmlWriter to the specified MemoryStream.  
        /// The XmlWriter will not close the MemoryStream, allowing subsequent use by an XmlReader.
        /// </summary>
        /// <param name="ms">Memory stream to use for writing.</param>
        /// <returns>XmlWriter based on memory stream.</returns>
        public static XmlWriter CreateXmlWriterForReading(MemoryStream ms)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CloseOutput = false;  // leave the memory stream open for subsequent reading
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            return XmlWriter.Create(ms, settings);
        }

        /// <summary>
        /// Creates an XmlReader from the specified MemoryStream.
        /// The MemoryStream position will be reset before reading, ideal if the MemoryStream was just written to.
        /// Closing the XmlReader will also close the underlying MemoryStream.
        /// </summary>
        /// <param name="ms">Memory stream to read from.</param>
        /// <returns>Reader based on memory stream.</returns>
        public static XmlReader CreateXmlReaderFromMemoryStream(MemoryStream ms)
        {
            ms.Position = 0; // reset stream position now that writing is done

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.CloseInput = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            return XmlReader.Create(ms, settings);
        }

        /// <summary>
        /// Aggregates xml data defined in different parameter types into a single XmlWriter stream and then returns the data as an XmlReader
        /// </summary>
        /// <param name="rootName">The root element name, if null or an empty string is passed, a root element is not created in the output.</param>
        /// <param name="paramObjects">Supported types are XmlReader, IXPathNavigable, XPathNodeIterator, XmlAttribute, XmlAttributeCollection, and string. String parameters are considered to be raw Xml data.</param>
        /// <returns></returns>
        static public XmlReader AggregateXml(string rootName, params object[] paramObjects)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream();
                using (XmlWriter writer = XmlHelpers.CreateXmlWriterForReading(stream))
                {
                    if (!string.IsNullOrEmpty(rootName))
                        writer.WriteStartElement(rootName);

                    foreach (object obj in paramObjects)
                    {
                        if (null != obj)
                        {
                            if (obj is XmlReader)
                            {
                                ((XmlReader)obj).MoveToContent();
                                writer.WriteNode((XmlReader)obj, false);
                            }
                            else if (obj is IXPathNavigable)
                            {
                                IXPathNavigable nav = (IXPathNavigable)obj;
                                writer.WriteNode(nav.CreateNavigator(), false);
                            }
                            else if (obj is string)
                                writer.WriteRaw((string)obj);
                            else if (obj is XPathNodeIterator)
                            {
                                XPathNodeIterator iter = (XPathNodeIterator)obj;
                                foreach (XPathNavigator nav in iter)
                                {
                                    writer.WriteNode(nav.CreateNavigator(), false);
                                }
                            }
                            else if (obj is XmlAttribute)
                            {
                                XmlAttribute attr = (XmlAttribute)obj;
                                writer.WriteAttributeString(attr.Prefix, attr.LocalName, attr.NamespaceURI, attr.Value);
                            }
                            else if (obj is XmlAttributeCollection)
                            {
                                XmlAttributeCollection coll = (XmlAttributeCollection)obj;
                                foreach (XmlAttribute attr in coll)
                                {
                                    writer.WriteAttributeString(attr.Prefix, attr.LocalName, attr.NamespaceURI, attr.Value);
                                }
                            }
                            else    //throw an exception if it is not one of the supported types
                                throw new ArgumentException("Unsupported parameter type");
                        }
                    }

                    if (!string.IsNullOrEmpty(rootName))
                        writer.WriteEndElement();
                }
                return XmlHelpers.CreateXmlReaderFromMemoryStream(stream);
            }
            catch (Exception e)
            {
                if (stream != null)
                    stream.Close();
                throw e;
            }
        }

        /// <summary>
        /// Get an empty reader.
        /// </summary>
        /// <returns>Empty xml reader</returns>
        static public XmlReader EmptyXmlReader()
        {
            MemoryStream stream = new MemoryStream();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.CloseInput = true;
            return XmlReader.Create(stream, settings);
        }

        /// <summary>
        /// Get an empty document.
        /// </summary>
        /// <returns>Empty xpath document</returns>
        static public XPathDocument EmptyXPathDocument()
        {
            using (XmlReader reader = EmptyXmlReader())
                return new XPathDocument(reader);
        }

        /// <summary>
        /// Get an empty iterator.
        /// </summary>
        /// <returns>Empty xpath node iterator</returns>
        static public XPathNodeIterator EmptyXPathNodeIterator()
        {
            return EmptyXPathDocument().CreateNavigator().Select("/");
        }

        /// <summary>
        /// Convert a string to base64.
        /// </summary>
        /// <param name="data">String to be converted to base64</param>
        /// <returns>Base64 encoded value</returns>
        static public string ToBase64(string data)
        {
            return Convert.ToBase64String(new UnicodeEncoding().GetBytes(data));
        }

        /// <summary>
        /// Convert a string from base64.
        /// </summary>
        /// <param name="data">Base64 encoded data</param>
        /// <returns>String converted form base64</returns>
        static public string FromBase64(string data)
        {
            byte[] arr = Convert.FromBase64String(data);
            return new UnicodeEncoding().GetString(arr);
        }

        /// <summary>
        /// Convert a base64 string to a byte array.
        /// </summary>
        /// <param name="data">Base64 string</param>
        /// <returns>Binary data</returns>
        static public byte[] FromBase64ToBytes(string data)
        {
            return Convert.FromBase64String(data);
        }

        /// <summary>
        /// Serialize an object into an XDocument
        /// </summary>
        /// <param name="pObject">Object to serialize</param>
        /// <param name="objectType">Type of the object</param>
        /// <returns>XDocument containing serialized object</returns>
        static public XDocument XmlSerializeObject(Object pObject, Type objectType)
        {
            MemoryStream memoryStream = new MemoryStream();
            XmlSerializer xs = new XmlSerializer(objectType);
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, new UTF8Encoding(false));
            xs.Serialize(xmlTextWriter, pObject);
            memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
            string xml = new UTF8Encoding(false).GetString(memoryStream.ToArray());
            return XDocument.Parse(xml);
        }

        /// <summary>
        /// Get a hash value of a string.
        /// </summary>
        /// <param name="s">Input string</param>
        /// <returns>Hexadecimal hash value</returns>
        static public string Hash(string s)
        {
            byte[] arr = new SHA1Managed().ComputeHash(new UnicodeEncoding().GetBytes(s));
            return ToHexString(arr);
        }

        /// <summary>
        /// Convert a byte array to a hex string
        /// </summary>
        /// <param name="bytes">Input byte array</param>
        /// <returns>Hex string</returns>
        static private string ToHexString(byte[] bytes)
        {
            char[] hexDigits = {   '0', '1', '2', '3', '4', '5', '6', '7',
									'8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};
            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }

            return new string(chars);
        }

        /// <summary>
        /// Replace special characters
        /// </summary>
        /// <param name="xml">String to check</param>
        /// <returns>String with special chars replaced</returns>
        static public string XmlEncode(string xml)
        {
            return xml.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace(">", "&gt;").Replace("<", "&lt;");
        }

        /// <summary>
        /// Evaluate an xpath expression against a list of xpath variables.
        /// </summary>
        /// <param name="xpath">XPath expression to evaluate</param>
        /// <param name="xpathVars">XPathVariable array to evaluate against</param>
        /// <returns>Object containing results of evaluation</returns>
        static public object Evaluate(string xpath, XPathVariable[] xpathVars)
        {
            XmlDocument emptyDoc = new XmlDocument();
            emptyDoc.LoadXml("<root/>");

            return XPathCache.Evaluate(xpath, emptyDoc.CreateNavigator(), xpathVars);
        }

        /// <summary>
        /// Evaluate an xpath filter, if it exists, against xpath variables
        /// </summary>
        /// <param name="nav">Navigator that may have an child XPathFilter element</param>
        /// <param name="xpathVars">XPathVariable array to evaluate against, may be null</param>
        /// <returns>Result of evaluation</returns>
        static public bool EvaluateXPathFilter(XPathNavigator nav, XPathVariable[] xpathVars)
        {
            string filter = XmlHelpers.GetText(nav, "XPathFilter");
            if (xpathVars == null || string.IsNullOrEmpty(filter))
                return true;

            filter = "boolean( " + filter + " )";
            bool result = (bool)Evaluate(filter, xpathVars);
            return result;
        }

        /// <summary>
        /// Add an attribute.if exist, update the attribute.  Will remove the attribute if blank or null.
        /// </summary>
        /// <param name="node">XmlNode to update.</param>
        /// <param name="name">Attribute name to add/update.</param>
        /// <param name="attribValue">Attribute value, will remove attribute if blank.</param>
        public static void UpdateAttribute(XmlNode node, string name, string attribValue)
        {
            if (node != null)
            {
                XmlAttribute attribute = node.Attributes[name];
                if (attribute != null)
                    node.Attributes.Remove(attribute);

                if (!string.IsNullOrEmpty(attribValue))
                {
                    attribute = node.OwnerDocument.CreateAttribute(name);
                    node.Attributes.Append(attribute);
                    attribute.Value = attribValue;
                }
            }
        }

        /// <summary>
        /// Add an attribute.if exist, update the attribute.  Will remove the attribute if blank or null.
        /// </summary>
        /// <param name="node">XmlNode to update.</param>
        /// <param name="name">Attribute name to add/update.</param>
        /// <param name="attribValue">Attribute value, will remove attribute if null.</param>
        public static void AddAttribute(XmlNode node, string name, string attribValue)
        {
            if (node != null)
            {
                XmlAttribute attribute = node.Attributes[name];
                if (attribute != null)
                    node.Attributes.Remove(attribute);



                if (attribValue != null)
                {
                    attribute = node.OwnerDocument.CreateAttribute(name);
                    node.Attributes.Append(attribute);
                    attribute.Value = attribValue;
                }
            }
        }

 
        /// <summary>
        /// Get an attribute of a XmlNode.
        /// </summary>
        /// <param name="node">Node to query.</param>
        /// <param name="name">name of the attribute.</param>
        /// <returns>Attribute value or blank if not found.</returns>
        public static string GetAttribute(XmlNode node, string name)
        {
            XmlAttribute attribute = node.Attributes[name];
            return attribute != null ? attribute.Value : string.Empty;
        }

        /// <summary>
        /// Update the attribute name of a Xml Node
        /// </summary>
        /// <param name="element">Node to update</param>
        /// <param name="oldAttrName">the old attrbute name</param>
        /// <param name="newAttrName">the new attrbute name</param>
        public static void UpdateAttributeName(XElement element, string oldAttrName, string newAttrName)
        {
            if (element != null)
            {
                XAttribute[] attributes = element.Attributes().ToArray();
                for (int i = 0; i < attributes.Length; i++)
                {
                    string attName = attributes[i].Name.LocalName;
                    if (attName.Equals(oldAttrName))
                    {
                        XAttribute fcm = new XAttribute(newAttrName, attributes[i].Value);
                        attributes[i] = fcm;
                    }
                }
                element.ReplaceAttributes(attributes);
            }
        }
    }
}
