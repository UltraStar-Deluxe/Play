using System.Security.Cryptography;
using System.Text;

public static class Hashing
{
    //Very fast but collision-prone hashing
    public static uint Fnv1A(byte[] input)
    {
        const uint FNV32_PRIME = 16777619;
        const uint FNV32_OFFSETBASIS = 2166136261;
        uint hash = FNV32_OFFSETBASIS;
        for (int i = 0; i < input.Length; ++i)
        {
            hash = (hash * FNV32_PRIME) ^ input[i];
        }

        return hash;
    }

    //Standard MD5 hashing
    public static string Md5(byte[] input)
    {
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(input);

        StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
        for (int i = 0; i < hashBytes.Length; ++i)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }

        return sb.ToString();
    }
}
