using System.Security.Cryptography;

namespace BlockChain_1.Models
{
    public class Wallet
    {
        //Ім'я користувача
        public string Name { get; set; }

        //Адреса гаманця (виробляється з публічного ключа)
        public string Address { get; set; }

        //Публічний ключ — його можна показувати всім
        public byte[] PublicKey { get; set; }

        //Приватний ключ — ніхто, крім гаманця, його не бачить
        private byte[] PrivateKey { get; set; }

        public Wallet(string name, string address, byte[] publicKey, byte[] privateKey)
        {
            Name = name;
            Address = address;
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        //Підписуємо дані приватним ключем
        public byte[] Sign(byte[] data)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(PrivateKey, out _);
            return ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }
    }
}
