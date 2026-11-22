using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    //Центр перевірки: приймає транзакції, перевіряє їх і веде статистику
    public class TransactionVerificationCenter
    {
        private readonly BlockChain _blockChain;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly WalletService _walletService = new WalletService();

        //Усі транзакції, що проходили через центр (і прийняті, і відхилені)
        private readonly Dictionary<string, Transaction> _submitted = new Dictionary<string, Transaction>();

        //Журнал перевірок при поданні
        private readonly List<VerificationResult> _log = new List<VerificationResult>();

        public TransactionVerificationCenter(BlockChain blockChain)
        {
            _blockChain = blockChain;
        }

        //Статистика для звіту
        public int TotalChecked => _log.Count;
        public int AcceptedCount => _log.Count(r => r.IsValid);
        public int RejectedCount => _log.Count(r => !r.IsValid);
        public IReadOnlyList<VerificationResult> Log => _log.AsReadOnly();

        //Подати транзакцію: усі перевірки, і тільки коректна йде в mempool
        public VerificationResult Submit(Transaction tx)
        {
            var (ok, message) = _blockChain.AddTransaction(tx);

            var result = new VerificationResult
            {
                TransactionId = tx.Id,
                IsValid = ok,
                Reason = ok ? "Прийнято в mempool" : message,
                Location = ok ? "Mempool" : "Відхилено (не потрапила в Blockchain)"
            };

            _submitted.TryAdd(tx.Id, tx);
            _log.Add(result);
            return result;
        }

        //Перевірити будь-яку транзакцію за Id — заново, за її поточними даними
        public VerificationResult Verify(string id)
        {
            id = (id ?? "").Trim();

            //1. Шукаємо в ланцюгу
            var tx = FindInChain(id, out var blockIndex);
            if (tx != null)
            {
                return Check(tx, $"Blockchain, блок #{blockIndex}");
            }

            //2. У mempool
            tx = _blockChain.PendingTransactions.FirstOrDefault(t => t.Id == id);
            if (tx != null)
            {
                return Check(tx, "Mempool");
            }

            //3. Серед відхилених
            if (_submitted.TryGetValue(id, out tx))
            {
                var result = Check(tx, "Відхилено (не в Blockchain)");
                if (result.IsValid)
                {
                    //Підпис у порядку, але транзакцію відхилили з іншої причини (баланс, повтор)
                    result.IsValid = false;
                    result.Reason = _log.Last(r => r.TransactionId == id).Reason;
                }
                return result;
            }

            return new VerificationResult
            {
                TransactionId = id,
                IsValid = false,
                Reason = "Транзакцію з таким Id не знайдено",
                Location = "—"
            };
        }

        //ТІЛЬКИ для демонстрації вразливості: перевірка лише підпису,
        //без зв'язку "адреса відправника <-> публічний ключ"
        public bool NaiveSignatureCheck(Transaction tx)
        {
            return tx.Signature != null
                && tx.SenderPublicKey.Length > 0
                && _walletService.VerifySignature(tx.SenderPublicKey, tx.GetDataToSign(), tx.Signature);
        }

        private VerificationResult Check(Transaction tx, string location)
        {
            var (isValid, error) = _transactionService.ValidateTransaction(tx);
            return new VerificationResult
            {
                TransactionId = tx.Id,
                IsValid = isValid,
                Reason = isValid ? "Підпис і дані коректні" : error,
                Location = location
            };
        }

        private Transaction? FindInChain(string id, out int blockIndex)
        {
            foreach (var block in _blockChain.Chain)
            {
                foreach (var t in block.Transactions)
                {
                    if (t.Id == id)
                    {
                        blockIndex = block.Index;
                        return t;
                    }
                }
            }
            blockIndex = -1;
            return null;
        }
    }
}