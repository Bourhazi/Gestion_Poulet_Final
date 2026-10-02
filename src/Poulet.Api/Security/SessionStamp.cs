using System.Security.Cryptography;
using System.Text;

namespace Poulet.Api.Security;

internal static class SessionStamp
{
    public static string Create(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
}
