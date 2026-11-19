using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    //Зберігає гаманці та керує ними
    public class WalletManager
    {
        private readonly WalletService _walletService = new WalletService();

        //Ім'я -> гаманець. Регістр імені не важливий: "alice" == "Alice"
        private readonly Dictionary<string, Wallet> _wallets =
            new Dictionary<string, Wallet>(StringComparer.OrdinalIgnoreCase);

        public int Count => _wallets.Count;

        //Створення гаманця з перевірками
        public (Wallet? wallet, string error) CreateWallet(string name)
        {
            name = (name ?? "").Trim();

            if (string.IsNullOrEmpty(name))
            {
                return (null, "Ім'я не може бути порожнім");
            }

            if (name.Length > 30)
            {
                return (null, "Ім'я задовге (максимум 30 символів)");
            }

            if (_wallets.ContainsKey(name))
            {
                return (null, $"Гаманець з іменем \"{_wallets[name].Name}\" вже існує");
            }

            var wallet = _walletService.CreateWallet(name);
            _wallets.Add(name, wallet);
            return (wallet, string.Empty);
        }

        //Усі гаманці (відсортовані за ім'ям, тільки для читання)
        public IReadOnlyList<Wallet> GetAll()
        {
            return _wallets.Values.OrderBy(w => w.Name).ToList();
        }

        //Пошук за точним ім'ям
        public Wallet? FindByName(string name)
        {
            _wallets.TryGetValue((name ?? "").Trim(), out var wallet);
            return wallet;
        }

        //Пошук за адресою: повною або її початком (від 4 символів)
        public List<Wallet> FindByAddress(string address)
        {
            address = (address ?? "").Trim();
            if (address.Length < 4)
            {
                return new List<Wallet>();
            }

            return _wallets.Values
                .Where(w => w.Address.StartsWith(address, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}