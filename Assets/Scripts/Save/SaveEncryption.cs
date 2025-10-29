using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Blue.Save
{
    /// <summary>
    /// セーブデータの暗号化・復号化を担当するクラス
    /// </summary>
    public static class SaveEncryption
    {
        // 暗号化キー（本番環境ではより安全な方法で管理すること）
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("BlueGame2025Key!!"); // 16バイト (AES-128)
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("BlueGameIV123456"); // 16バイト

        /// <summary>
        /// データを暗号化
        /// </summary>
        public static byte[] Encrypt(byte[] data)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using MemoryStream ms = new MemoryStream();
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }
            return ms.ToArray();
        }

        /// <summary>
        /// データを復号化
        /// </summary>
        public static byte[] Decrypt(byte[] encrypted_data)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream ms = new MemoryStream(encrypted_data);
            using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using MemoryStream output = new MemoryStream();
            cs.CopyTo(output);
            return output.ToArray();
        }
    }
}
