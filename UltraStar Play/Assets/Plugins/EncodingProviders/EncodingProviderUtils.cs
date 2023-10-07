using System.Text;
using UnityEngine;

/**
 * Registers additional encodings.
 * Background: Some users had issues loading the windows-1252 encoding
 * ("NotSupportedException: Encoding 1252 data could not be found."),
 * which is still used for many UltraStar txt files.
 * Thus, additional encodings are registered here via the DLL from the NuGet package
 * https://www.nuget.org/packages/System.Text.Encoding.CodePages/7.0.0
 *
 * See also https://stackoverflow.com/questions/50858209/system-notsupportedexception-no-data-is-available-for-encoding-1252
 */
public static class EncodingProviderUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        Debug.Log($"Registering encoding provider CodePagesEncodingProvider.Instance");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
