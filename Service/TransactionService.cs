using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    public class TransactionService
    {
        private readonly WalletService _walletService = new WalletService();
        public Transaction CreateTransaction(Wallet from, string toAddress, decimal amount)
        {
            var tx = new Transaction(from.Address, toAddress, amount);

            //Відправник додає свій публічний ключ і підписує транзакцію
            tx.SenderPublicKey = from.PublicKey;
            tx.Signature = from.Sign(tx.GetDataToSign());

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

            if (tx.SenderPublicKey == null || tx.SenderPublicKey.Length == 0)
            {
                return (false, "Missing sender public key");
            }

            if (tx.Signature == null || tx.Signature.Length == 0)
            {
                return (false, "Transaction is not signed");
            }

            //Адреса відправника має відповідати його публічному ключу
            if (tx.From != WalletService.GetAddress(tx.SenderPublicKey))
            {
                return (false, "Sender address does not match public key");
            }

            //Сам підпис
            if (!_walletService.VerifySignature(tx.SenderPublicKey, tx.GetDataToSign(), tx.Signature))
            {
                return (false, "Invalid signature");
            }

            return (true, string.Empty);
        }
    }
}
