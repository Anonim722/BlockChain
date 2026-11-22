using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    //Транзакції, які чекають на включення в блок
    public class Mempool
    {
        private readonly List<Transaction> _pending = new List<Transaction>();
        private readonly TransactionService _transactionService = new TransactionService();

        //Кількість транзакцій у mempool
        public int Count => _pending.Count;

        //Тільки для читання — змінювати можна лише через методи нижче
        public IReadOnlyList<Transaction> PendingTransactions => _pending.AsReadOnly();

        //Додати транзакцію в mempool
        public (bool isSuccess, string message) AddTransaction(Transaction transaction)
        {
            if (transaction.IsCoinbase)
            {
                return (false, "Coinbase-транзакції не потрапляють у mempool");
            }

            var (isValid, error) = _transactionService.ValidateTransaction(transaction);
            if (!isValid)
            {
                return (false, $"Транзакція недійсна: {error}");
            }

            if (_pending.Any(x => x.Id == transaction.Id))
            {
                return (false, "Транзакція вже є в mempool");
            }

            _pending.Add(transaction);
            return (true, "Транзакцію додано в mempool");
        }

        //Перші maxCount транзакцій для наступного блоку (без видалення)
        public List<Transaction> Peek(int maxCount)
        {
            return _pending.Take(maxCount).ToList();
        }

        //Видалити транзакції, які потрапили в блок
        public void RemoveTransactions(List<Transaction> transactions)
        {
            var ids = transactions.Select(t => t.Id).ToHashSet();
            _pending.RemoveAll(t => ids.Contains(t.Id));
        }

        public void Clear()
        {
            _pending.Clear();
        }
    }
}