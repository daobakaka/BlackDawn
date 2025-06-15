using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace GameFrame.BaseClass
{
    // —— 1. CRC32 工具类 —— 
    public static class Crc32Helper
    {
        // 预生成的 CRC 表（0x04C11DB7）
        private static readonly uint[] Table = GenerateTable();

        private static uint[] GenerateTable()
        {
            var table = new uint[256];
            const uint poly = 0xEDB88320;
            for (uint i = 0; i < table.Length; ++i)
            {
                uint crc = i;
                for (int j = 0; j < 8; ++j)
                    crc = (crc & 1) != 0 ? (poly ^ (crc >> 1)) : (crc >> 1);
                table[i] = crc;
            }
            return table;
        }

        public static uint Compute(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            uint crc = 0xFFFFFFFF;
            foreach (byte b in bytes)
                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFF;
        }
    }

    // —— 2. AES 工具类 —— 
    public static class AesHelper
    {
        // 16 字节密钥（AES-128）
        private static readonly byte[] KEY =
            Encoding.UTF8.GetBytes("1234567890ABCDEF"); // 一定要 16 字节
                                                        // 16 字节初始向量
        private static readonly byte[] IV =
            Encoding.UTF8.GetBytes("FEDCBA0987654321"); // 16 字节

        public static byte[] Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = KEY;
            aes.IV = IV;
            using var ms = new MemoryStream();
            using (var crypto = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var writer = new StreamWriter(crypto, Encoding.UTF8))
                writer.Write(plainText);
            return ms.ToArray();
        }

        public static string Decrypt(byte[] cipherBytes)
        {
            using var aes = Aes.Create();
            aes.Key = KEY;
            aes.IV = IV;
            using var ms = new MemoryStream(cipherBytes);
            using var crypto = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(crypto, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}