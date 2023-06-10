using System;

public class WebViewMessageDto
{
    public string type;

    public static bool TryParseType(string type, out WebViewMessageType webViewMessageType)
    {
        if (type.IsNullOrEmpty())
        {
            webViewMessageType = WebViewMessageType.Unknown;
            return false;
        }
        
        foreach (WebViewMessageType typeEnum in EnumUtils.GetValuesAsList<WebViewMessageType>())
        {
            if (string.Equals(type, typeEnum.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                webViewMessageType = typeEnum;
                return true;
            }
        }

        webViewMessageType = WebViewMessageType.Unknown;
        return false;
    }
}
