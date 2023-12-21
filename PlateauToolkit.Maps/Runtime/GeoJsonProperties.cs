using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PropertyKeyValuePair
{
    public string m_Key;
    public string m_Value;
}

public class GeoJsonProperties : MonoBehaviour
{
    public List<PropertyKeyValuePair> m_PropertiesList = new List<PropertyKeyValuePair>();

    public void SetProperties(Dictionary<string, object> jsonProperties)
    {
        m_PropertiesList.Clear();

        foreach (KeyValuePair<string, object> property in jsonProperties)
        {
            m_PropertiesList.Add(new PropertyKeyValuePair { m_Key = property.Key, m_Value = property.Value.ToString() });
        }
    }
}
