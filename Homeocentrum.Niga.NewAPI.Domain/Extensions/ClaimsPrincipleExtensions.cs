using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Xml;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Domain.Extensions
{
    public interface IClaimsProvider
    {
        int UserId { get; }

    }
    public class ClaimsProvider : IClaimsProvider 
    { 
        private readonly IHttpContextAccessor accessor;    

        public ClaimsProvider(IHttpContextAccessor accessor) 
        { 
            this.accessor = accessor;          
        }
                public int UserId => int.TryParse(accessor.HttpContext?.User?.Claims?.SingleOrDefault(x => x.Type == "UserId")?.Value, out var bob) ? bob : 0;

      
        public int ExpiryStatus => throw new NotImplementedException();

        public string DecryptData => throw new NotImplementedException();

       }
    public static class ClaimsPrincipleExtensions
    { 
        public static string GetUsername(this ClaimsPrincipal user)
        {
            
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }
        public static int GetUserId(this ClaimsPrincipal user)
        {
            if (user == null)
                return 0;

            foreach (var claim in user.Claims)
            {
                if (claim.Type != ClaimTypes.NameIdentifier
                    && claim.Type != "nameid"
                    && claim.Type != "UserId"
                    && claim.Type != "userId"
                    && claim.Type != "sub")
                    continue;
                if (int.TryParse(claim.Value, out var id) && id > 0)
                    return id;
            }

            return 0;
        }

        /// <summary>SEC-01.02 — DoctorID claim from JWT (doctors / reception).</summary>
        public static int? GetDoctorId(this ClaimsPrincipal user)
            => DoctorOwnership.GetDoctorId(user);

        /// <summary>M02 / SEC-04 — Admin or Management portal principal.</summary>
        public static bool IsAdminPortalUser(this ClaimsPrincipal user)
            => AdminAuthorizationPolicies.IsAdminPortalUser(user);
     
        private static string DecryptDataWithAes(string cipherText, string keyBase64, string vectorBase64)
            {
                using (Aes aesAlgorithm = Aes.Create())
                {
                    aesAlgorithm.Key = Convert.FromBase64String(keyBase64);
                    aesAlgorithm.IV = Convert.FromBase64String(vectorBase64);

                    Console.WriteLine($"Aes Cipher Mode : {aesAlgorithm.Mode}");
                    Console.WriteLine($"Aes Padding Mode: {aesAlgorithm.Padding}");
                    Console.WriteLine($"Aes Key Size : {aesAlgorithm.KeySize}");
                    Console.WriteLine($"Aes Block Size : {aesAlgorithm.BlockSize}");


                    ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();

                    byte[] cipher = Convert.FromBase64String(cipherText);

                    using (MemoryStream ms = new MemoryStream(cipher))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
       
        }
}