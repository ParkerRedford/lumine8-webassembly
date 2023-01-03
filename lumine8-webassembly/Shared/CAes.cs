using System.Security.Cryptography;
using System.Text;

namespace lumine8.Aes
{
    public class CAes
    {
        public byte[] iv, key;

        public CAes(string Password, string Key)
        {
            using SHA512 sha = SHA512.Create();
            iv = sha.ComputeHash(Encoding.UTF8.GetBytes(Password)).Take(16).ToArray();
            key = sha.ComputeHash(Encoding.UTF8.GetBytes(Key)).Take(16).ToArray();
        }

        public byte[] ConvertStringToKeyOrIV(string ki)
        {
            using SHA512 sha = SHA512.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(ki)).Take(16).ToArray();
        }

        public byte[] Encrypt(string info, byte[] iv, byte[] key)
        {
            using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream stream = new MemoryStream())
                {
                    using (CryptoStream crypto = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(crypto))
                        {
                            writer.Write(info);
                        }
                        return stream.ToArray();
                    }
                }
            }
        }

        public string Decrypt(byte[] encryptedInfo, byte[] iv, byte[] key)
        {
            using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream stream = new MemoryStream(encryptedInfo))
                {
                    using (CryptoStream crypto = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(crypto))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
