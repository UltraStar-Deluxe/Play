using System.IO;
using System.Xml.Serialization;

// Implements serialization to XML and deserialization from XML.
public class XmlConverter
{
    public static string ToXml<T>(T obj)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
        using (TextWriter textWriter = new StringWriter())
        {
            xmlSerializer.Serialize(textWriter, obj);
            string json = textWriter.ToString();
            return json;
        }
    }

    public static T FromXml<T>(string json)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
        using (TextReader reader = new StringReader(json))
        {
            T obj = (T)xmlSerializer.Deserialize(reader);
            return obj;
        }
    }
}
