using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    public class TransactionService
    {
        public Transaction CreateTransaction(string from, string to, decimal amount)
        {
            var tx = new Transaction(from, to, amount);
            var validationResult = ValidateTransaction(tx);

            if (!validationResult.isValid)
            {
                throw new ArgumentException(validationResult.errorMessage);
            }

            return tx;
        }

        public (bool isValid, string errorMessage) ValidateTransaction(Transaction tx)
        {
            if (string.IsNullOrEmpty(tx.From))
            {
                return (false, "Invalid sender address");
            }

            if (string.IsNullOrEmpty(tx.To))
            {
                return (false, "Invalid recipient address");
            }

            if (tx.Amount <= 0)
            {
                return (false, "Invalid transaction amount");
            }

            return (true, string.Empty);
        }
    }
}
