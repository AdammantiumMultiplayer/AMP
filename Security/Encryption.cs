using System.Text;

namespace AMP.Security {
    public class Encryption {
        public static string SHA256(string value) {
            if(value == null || value.Length == 0) return value;

            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(value));
            foreach(byte theByte in crypto) {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
