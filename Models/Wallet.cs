using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlockChain_1.Models
{
    internal class Wallet
    {
        // имя, адреса гаманця, публічний та приватний ключі
        public string Name { get; set; }

        public string Address { get; set; }

        public byte[] PublicKey { get; set; }

        public byte[] PrivateKey { get; set; }

        public Wallet(string name, string address, byte[] publicKey, byte[] privateKey)
        {
            Name = name;
            Address = address;
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] Sign(byte[] data)
        {
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            ecdsa.ImportECPrivateKey(PrivateKey, out _);
            return ecdsa.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256);
        }

    }
}
