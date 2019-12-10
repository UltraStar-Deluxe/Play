using System.Security.Cryptography;
using System.Text;

public class Hashing
{
    //Very fast but collision-prone hashing
    public static uint FNV1a(byte[] input)
    {
        const uint FNV32_PRIME = 16777619;
        const uint FNV32_OFFSETBASIS = 2166136261;
        uint hash = FNV32_OFFSETBASIS;
        for (int i = 0; i < input.Length; ++i)
            hash = (hash * FNV32_PRIME) ^ input[i];
        
        return hash;
    }

    //Standard MD5 hashing
    public static string MD5(byte[] input)
    {
        var md5 = new MD5CryptoServiceProvider();
        var hashBytes = md5.ComputeHash(input);

        var sb = new StringBuilder(hashBytes.Length * 2);
        for(int i = 0; i < hashBytes.Length; ++i)
            sb.Append(hashBytes[i].ToString("x2"));

        return sb.ToString();
    }
}
