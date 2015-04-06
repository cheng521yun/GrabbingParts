using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GrabbingParts.XmlHelpers
{
    /// <summary>
    /// XML Extension methods
    /// </summary>
    public static class XmlHelpersExtensions
    {
        /// <summary>
        /// Get an XElement from a XmlNode
        /// </summary>
        /// <param name="node">XmlNode to convert</param>
        /// <returns>XElement with node content</returns>
        public static XElement GetXElement(this XmlNode node)
        {
            XDocument xDoc = new XDocument();
            using (XmlWriter xmlWriter = xDoc.CreateWriter())
                node.WriteTo(xmlWriter);
            return xDoc.Root;
        }

        /// <summary>
        /// Get an XmlNode from an XElement
        /// </summary>
        /// <param name="element">XElement to convert</param>
        /// <returns>XmlNode containing element data</returns>
        public static XmlNode GetXmlNode(this XElement element)
        {
            using (XmlReader xmlReader = element.CreateReader())
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);
                return xmlDoc;
            }
        }

        /// <summary>
        /// Get the first text in the element, ignores child text content
        /// </summary>
        /// <param name="elem">XElement containing text value</param>
        /// <returns>Immediate child text</returns>
        public static string FirstValue(this XElement elem)
        {
            return elem.Nodes().OfType<XText>().First().Value;
        }
    }
}
