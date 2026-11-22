using BlockChain_1.Models;

namespace BlockChain_1
{
    public class WalletDisplayService
    {
        //Один гаманець детально
        public void PrintWallet(Wallet wallet)
        {
            Console.WriteLine($"Ім'я:           {wallet.Name}");
            Console.WriteLine($"Адреса:         {wallet.Address}");
            Console.WriteLine($"Публічний ключ: {ShortKey(wallet.PublicKey)}");
            Console.WriteLine(new string('-', 60));
        }

        //Список гаманців таблицею
        public void PrintWallets(IEnumerable<Wallet> wallets)
        {
            var list = wallets.ToList();
            if (list.Count == 0)
            {
                Console.WriteLine("Гаманців немає.");
                return;
            }

            Console.WriteLine($"{"Ім'я",-12} | {"Адреса",-40} | Публічний ключ");
            Console.WriteLine(new string('-', 80));
            foreach (var w in list)
            {
                Console.WriteLine($"{w.Name,-12} | {w.Address,-40} | {ShortKey(w.PublicKey)}");
            }
            Console.WriteLine($"Усього: {list.Count}");
        }

        //Скорочений публічний ключ: останні 16 символів Base64
        public static string ShortKey(byte[] publicKey)
        {
            var b64 = Convert.ToBase64String(publicKey);
            return b64.Length <= 16 ? b64 : "..." + b64.Substring(b64.Length - 16);
        }

    }
}