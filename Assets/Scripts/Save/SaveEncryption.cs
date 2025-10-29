using System;
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
        // 確実に16バイトになるようにバイト配列で直接定義
        private static readonly byte[] Key = new byte[16]
        {
            0x42, 0x6C, 0x75, 0x65, 0x47, 0x61, 0x6D, 0x65,  // "BlueGame"
            0x32, 0x30, 0x32, 0x35, 0x4B, 0x65, 0x79, 0x21   // "2025Key!"
        };
        private static readonly byte[] IV = new byte[16]
        {
            0x42, 0x6C, 0x75, 0x65, 0x49, 0x56, 0x31, 0x32,  // "BlueIV12"
            0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30   // "34567890"
        };

        /// <summary>
        /// データを暗号化
        /// </summary>
        public static byte[] Encrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// データを復号化
        /// </summary>
        public static byte[] Decrypt(byte[] encrypted_data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream ms = new MemoryStream(encrypted_data))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cs.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }
}
