using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    public class BalanceService
    {
        //Баланс адреси на основі всіх блоків ланцюга
        public decimal GetBalance(string address, List<Block> chain)
        {
            var balance = 0m;
            foreach (var block in chain)
            {
                foreach (var transaction in block.Transactions)
                {
                    if (transaction.To == address)
                    {
                        //Адреса — отримувач: додаємо суму
                        balance += transaction.Amount;
                    }
                    if (transaction.From == address)
                    {
                        //Адреса — відправник: віднімаємо суму
                        balance -= transaction.Amount;
                    }
                }
            }
            return balance;
        }

        //Чи вистачить коштів з урахуванням ще не замайнених транзакцій (черги)
        public bool HasSufficientBalance(string address, decimal amount,
            List<Block> chain, List<Transaction> pending)
        {
            var balance = GetBalance(address, chain);

            foreach (var transaction in pending)
            {
                if (transaction.From == address)
                {
                    //Ці гроші вже "зарезервовані" в черзі
                    balance -= transaction.Amount;
                }
            }

            return balance >= amount;
        }
    }
}