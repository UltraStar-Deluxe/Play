using System.Text;

public static class HashingUtils
{
    public static string Md5Hash(string input)
    {
        return Hashing.Md5(Encoding.UTF8.GetBytes(input));
    }
}
