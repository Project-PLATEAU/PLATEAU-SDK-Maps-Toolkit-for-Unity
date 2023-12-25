using UnityEngine;
using System.Xml;

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

        public static void AttachMetadata(TextAsset xmlFile, GameObject rootObj)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlFile.text);

            // Loop through all GameObjects in scene
            foreach (Transform childObj in rootObj.transform)
            {
                XmlNode foundNode = null;
                // Traverse through all the nodes recursively
                TraverseNodes(doc.DocumentElement, childObj.name, ref foundNode);

                if (foundNode != null)
                {
                    // If found, create a new XmlElementComponent
                    XmlElementComponent component = childObj.gameObject.AddComponent<XmlElementComponent>();
                    component.ID = childObj.name;

                    // Add properties and child elements to the lists
                    foreach (XmlAttribute attr in foundNode.Attributes)
                    {
                        if (attr.Name != "id")
                        {
                            component.Properties.Add($"{attr.Name}: {attr.Value}");
                        }
                    }
                    foreach (XmlNode child in foundNode.ChildNodes)
                    {
                        component.ChildElements.Add($"{child.Name}: {child.OuterXml}");
                    }
                }
            }
        }

        static void TraverseNodes(XmlNode node, string targetID, ref XmlNode foundNode)
        {
            if (node.Attributes != null && node.Attributes["id"] != null && node.Attributes["id"].Value == targetID)
            {
                foundNode = node;
                return;
            }
            else
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    TraverseNodes(childNode, targetID, ref foundNode);
                    if (foundNode != null)
                    {
                        return;
                    }
                }
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