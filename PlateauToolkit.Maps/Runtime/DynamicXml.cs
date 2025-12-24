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
                TraverseTransforms(child);
            }
        }

        static void TraverseTransforms(Transform transform)
        {
            if (dictionary.TryGetValue(transform.name, out var xmlNode))
            {
                XmlElementComponent elementComponent = transform.gameObject.AddComponent<XmlElementComponent>();
                elementComponent.ID = transform.name;
                foreach (XmlAttribute attribute in xmlNode.Attributes)
                {
                    if (attribute.Name != "id")
                        elementComponent.Properties.Add(attribute.Name + ": " + attribute.Value);
                }

                foreach (XmlNode childNode in xmlNode.ChildNodes)
                    elementComponent.ChildElements.Add(childNode.Name + ": " + childNode.OuterXml);
            }
            
            foreach (Transform child in transform)
            {
                TraverseTransforms(child);
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
            if (node.Attributes != null && node.Attributes["id"] != null && !dictionary.ContainsKey(node.Attributes["id"].Value))
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
            catch (XmlException ex)
            {
                Debug.LogError($"XMLファイルの読み込みに失敗しました: {ex.Message}");
                return ifcCoordinate;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"XMLファイルの処理中にエラーが発生しました: {ex.Message}");
                return ifcCoordinate;
            }
            
            foreach (string targetName in targetNames)
            {
                // 通常の検索
                XmlNodeList targetNodes = doc.SelectNodes($"//IfcPropertySet[@Name='{targetName}']");
                
                // 名前空間を考慮しない検索も試行
                if (targetNodes == null || targetNodes.Count == 0)
                {
                    targetNodes = doc.SelectNodes($"//*[local-name()='IfcPropertySet' and @Name='{targetName}']");
                }
                
                // 大文字小文字を区別しない検索も試行
                if (targetNodes == null || targetNodes.Count == 0)
                {
                    XmlNodeList allPS = doc.SelectNodes("//IfcPropertySet | //*[local-name()='IfcPropertySet']");
                    if (allPS != null)
                    {
                        foreach (XmlNode node in allPS)
                        {
                            XmlAttribute nameAttr = node.Attributes?["Name"];
                            if (nameAttr != null && string.Equals(nameAttr.Value, targetName, System.StringComparison.OrdinalIgnoreCase))
                            {
                                XmlNodeList properties = node.SelectNodes("IfcPropertySingleValue | .//*[local-name()='IfcPropertySingleValue']");
                                ProcessProperties(properties, targetName, ref ifcCoordinate);
                            }
                        }
                    }
                }
                else
                {
                    foreach (XmlNode targetNode in targetNodes)
                    {
                        XmlNodeList properties = targetNode.SelectNodes("IfcPropertySingleValue | .//*[local-name()='IfcPropertySingleValue']");
                        ProcessProperties(properties, targetName, ref ifcCoordinate);
                    }
                }
            }
            
            // IfcPropertySetから情報が取得できなかった場合、IfcSiteから座標情報を取得を試行
            if (ifcCoordinate.Height == -1d || ifcCoordinate.Eastings == -1d || ifcCoordinate.Northings == -1d)
            {
                XmlNodeList siteNodes = doc.SelectNodes("//IfcSite | //*[local-name()='IfcSite']");
                if (siteNodes != null && siteNodes.Count > 0)
                {
                    foreach (XmlNode siteNode in siteNodes)
                    {
                        XmlAttribute refLatAttr = siteNode.Attributes?["RefLatitude"];
                        XmlAttribute refLonAttr = siteNode.Attributes?["RefLongitude"];
                        XmlAttribute refElevAttr = siteNode.Attributes?["RefElevation"];
                        
                        if (refLatAttr != null && refLonAttr != null)
                        {
                            if (double.TryParse(refLatAttr.Value, out double latitude) &&
                                double.TryParse(refLonAttr.Value, out double longitude))
                            {
                                ifcCoordinate.Latitude = latitude;
                                ifcCoordinate.Longitude = longitude;
                                ifcCoordinate.IsWgs84 = true;
                                
                                if (refElevAttr != null && double.TryParse(refElevAttr.Value, out double elevation))
                                {
                                    ifcCoordinate.Height = elevation;
                                }
                                else
                                {
                                    ifcCoordinate.Height = 0.0;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            
            return ifcCoordinate;
        }
        
        static void ProcessProperties(XmlNodeList properties, string targetName, ref IfcCoordinate ifcCoordinate)
        {
            if (properties == null) return;
            
            foreach (XmlNode property in properties)
            {
                try
                {
                    XmlAttribute nameAttr = property.Attributes?["Name"];
                    XmlAttribute nominalValueAttr = property.Attributes?["NominalValue"];
                    
                    if (nameAttr == null || nominalValueAttr == null)
                    {
                        continue;
                    }
                    
                    string name = nameAttr.InnerText;
                    string nominalValue = nominalValueAttr.InnerText;
                    
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
                catch (System.Exception ex)
                {
                    string propertyName = property.Attributes?["Name"]?.InnerText ?? "不明";
                    Debug.LogWarning($"プロパティ '{propertyName}' の解析中にエラーが発生しました: {ex.Message}");
                }
            }
        }
    }
}