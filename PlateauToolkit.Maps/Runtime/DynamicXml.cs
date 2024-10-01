using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace PlateauToolkit.Maps
{
    public static class DynamicXml
    {
        public static readonly int[,] EpsgToJgd2011 = new int[,]
        {
            {6669, 1}, {6670, 2}, {6671, 3}, {6672, 4}, {6673, 5},
            {6674, 6}, {6675, 7}, {6676, 8}, {6677, 9}, {6678, 10},
            {6679, 11}, {6680, 12}, {6681, 13}, {6682, 14}, {6683, 15},
            {6684, 16}, {6685, 17}, {6686, 18}, {6687, 19}
        };
        
        static Dictionary<string, XmlNode> dictionary = new Dictionary<string, XmlNode>();

        public static void AttachMetadata(TextAsset xmlFile, GameObject rootObj)
        {
            dictionary = GetNodeList(xmlFile);
            // Exclude the root object
            foreach (Transform child in rootObj.transform)
            {
                TraverseGameObjects(child.gameObject);
            }
        }

        static void TraverseGameObjects(GameObject gameObject)
        {
            if (dictionary.TryGetValue(gameObject.transform.name, out var xmlNode))
            {
                XmlElementComponent elementComponent = gameObject.AddComponent<XmlElementComponent>();
                elementComponent.ID = gameObject.transform.name;
                foreach (XmlAttribute attribute in xmlNode.Attributes)
                {
                    if (attribute.Name != "id")
                        elementComponent.Properties.Add(attribute.Name + ": " + attribute.Value);
                }

                foreach (XmlNode childNode in xmlNode.ChildNodes)
                    elementComponent.ChildElements.Add(childNode.Name + ": " + childNode.OuterXml);
            }
            
            foreach (Transform child in gameObject.transform)
            {
                TraverseGameObjects(child.gameObject);
            }
        }

        static Dictionary<string, XmlNode> GetNodeList(TextAsset xmlFile)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlFile.text);
            Dictionary<string, XmlNode> dictionary = new Dictionary<string, XmlNode>();
            var root = xmlDocument.DocumentElement;
            TraverseNodes(root, ref dictionary);
            return dictionary;
        }

        static void TraverseNodes(XmlNode node, ref Dictionary<string, XmlNode> dictionary)
        {
            if (node.Attributes != null && node.Attributes["id"] != null)
            {
                dictionary.Add(node.Attributes["id"].Value, node);
            }
            foreach (XmlNode childNode in node.ChildNodes)
            {
                TraverseNodes(childNode, ref dictionary);
            }
        }

        public static IfcCoordinate GetCoordinateInformation(TextAsset xmlFile)
        {
            XmlDocument doc = new XmlDocument();
            string[] targetNames = { "ePSet_ProjectedCRS", "ePSet_MapConversion" };

            IfcCoordinate ifcCoordinate = new IfcCoordinate();

            try
            {
                doc.LoadXml(xmlFile.text);
            }
            catch
            {
                return ifcCoordinate;
            }
            foreach (string targetName in targetNames)
            {
                XmlNodeList targetNodes = doc.SelectNodes($"//IfcPropertySet[@Name='{targetName}']");

                foreach (XmlNode targetNode in targetNodes)
                {
                    XmlNodeList properties = targetNode.SelectNodes("IfcPropertySingleValue");
                    foreach (XmlNode property in properties)
                    {
                        try
                        {
                            string name = property.Attributes["Name"].InnerText;
                            string nominalValue = property.Attributes["NominalValue"].InnerText;
                            Debug.Log($"Target: {targetName}, Name: {name}, NominalValue: {nominalValue}");
                            if (name == "Eastings")
                            {
                                ifcCoordinate.Eastings = double.Parse(nominalValue) / 1000.0;
                                // We divide by 1000 here is because the value in the XML needs to be
                                // divided by 1000
                            }
                            if (name == "Northings")
                            {
                                ifcCoordinate.Northings = double.Parse(nominalValue) / 1000.0;
                            }
                            if (name == "OrthogonalHeight")
                            {
                                ifcCoordinate.Height = double.Parse(nominalValue);
                            }
                            if (name == "XAxisAbscissa")
                            {
                                ifcCoordinate.XAbscissa = double.Parse(nominalValue);
                            }
                            if (name == "XAxisOrdinate")
                            {
                                ifcCoordinate.XOrdinate = double.Parse(nominalValue);
                            }
                            if (name == "Scale")
                            {
                                ifcCoordinate.Scale = double.Parse(nominalValue);
                            }
                            if (targetName == "ePSet_ProjectedCRS" && name == "Name")
                            {
                                ifcCoordinate.EpsgCode = -1;
                                ifcCoordinate.Jgd2011Id = -1;
                                string[] parts = nominalValue.Split(':');

                                if (parts.Length > 1 && int.TryParse(parts[1], out int epsgNumber))
                                {
                                    ifcCoordinate.EpsgCode = epsgNumber;
                                    for (int i = 0; i < EpsgToJgd2011.GetLength(0); i++)
                                    {
                                        if (EpsgToJgd2011[i, 0] == epsgNumber)
                                        {
                                            ifcCoordinate.Jgd2011Id = EpsgToJgd2011[i, 1];
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            Debug.Log(property.Attributes["Name"].InnerText);
                        }
                    }
                }
            }
            return ifcCoordinate;
        }
    }
}