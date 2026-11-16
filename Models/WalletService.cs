using System.Security.Cryptography;
using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    public class WalletService
    {
        //Створення нового гаманця
        public Wallet CreateWallet(string name)
        {
            using var ecdsa = ECDsa.Create();                        //нова пара ключів
            var privateKey = ecdsa.ExportECPrivateKey();             //приватний ключ
            var publicKey = ecdsa.ExportSubjectPublicKeyInfo();      //публічний ключ
            var address = GetAddress(publicKey);                     //адреса з публічного ключа

            return new Wallet(name, address, publicKey, privateKey);
        }

        //Адреса = перші 40 hex-символів SHA-256 від публічного ключа
        public static string GetAddress(byte[] publicKey)
        {
            var hash = SHA256.HashData(publicKey);
            return Convert.ToHexString(hash).ToLower().Substring(0, 40);
        }

        //Перевірка підпису за публічним ключем
        public bool VerifySignature(byte[] publicKey, byte[] data, byte[] signature)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                return false; //зіпсований ключ або підпис
            }
        }

        //Варіант з дошки — через гаманець
        public bool VerifySignature(Wallet wallet, byte[] data, byte[] signature)
        {
            return VerifySignature(wallet.PublicKey, data, signature);
        }
    }
}