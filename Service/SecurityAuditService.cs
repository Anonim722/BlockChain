using BlockChain_1.Models;

namespace BlockChain_1.Service
{
    //Автоматичний аудит безпеки на окремому тестовому ланцюгу
    public class SecurityAuditService
    {
        private readonly TransactionService _transactionService = new TransactionService();
        private BlockChain _chain = null!;
        private WalletManager _wallets = null!;

        public async Task<List<AuditResult>> RunAsync()
        {
            //Тестовий ланцюг: низька складність, без автокорекції — швидко і передбачувано
            _chain = new BlockChain(2, autoAdjust: false);
            _wallets = new WalletManager();
            foreach (var name in new[] { "Alice", "Bob", "Carol", "Dave", "Eve", "Poor" })
            {
                _wallets.CreateWallet(name);
            }

            //Стартові кошти: Alice, Carol і Dave майнять по блоку. Poor навмисно без коштів.
            foreach (var name in new[] { "Alice", "Carol", "Dave" })
            {
                await _chain.AddBlockAsync(new List<Transaction>(), W(name).Address);
            }

            return new List<AuditResult>
            {
                await Safe("Insufficient balance",  () => Task.FromResult(CheckInsufficientBalance())),
                await Safe("Double spending",       CheckDoubleSpending),
                await Safe("Fake Coinbase",         () => Task.FromResult(CheckFakeCoinbase())),
                await Safe("Invalid sender",        () => Task.FromResult(CheckInvalidSender())),
                await Safe("Replay attack",         CheckReplayAttack),
                await Safe("Difficulty validation", CheckDifficultyValidation)
            };
        }

        //1. Користувач без коштів намагається переказати монети
        private AuditResult CheckInsufficientBalance()
        {
            var poor = W("Poor");
            var tx = _transactionService.CreateTransaction(poor, W("Alice").Address, 10);
            int blocksBefore = _chain.Chain.Count;

            var viaMempool = _chain.AddTransaction(tx);
            var viaBlock = _chain.AddBlock(new List<Transaction> { tx }, W("Alice").Address);

            bool passed = !viaMempool.isSuccess && !viaBlock.Success && _chain.Chain.Count == blocksBefore;
            return new AuditResult("Insufficient balance", passed,
                $"баланс Poor = {_chain.GetBalance(poor.Address)}; " +
                $"mempool: {(viaMempool.isSuccess ? "прийняв" : "відхилив")}, " +
                $"блок: {(viaBlock.Success ? "створено" : "відхилено")}");
        }

        //2. Спроба витратити більше, ніж є, через кілька транзакцій у mempool
        private async Task<AuditResult> CheckDoubleSpending()
        {
            var carol = W("Carol");
            var balance = _chain.GetBalance(carol.Address);
            var amount = Math.Floor(balance / 3.5m);          //3 транзакції вміщаються, 4-та — ні

            int accepted = 0, rejected = 0;
            for (int i = 0; i < 4; i++)
            {
                var tx = _transactionService.CreateTransaction(carol, W("Bob").Address, amount);
                if (_chain.AddTransaction(tx).isSuccess) accepted++;
                else rejected++;
            }

            var reserved = _chain.PendingTransactions
                .Where(t => t.From == carol.Address)
                .Sum(t => t.Amount);

            var mined = await _chain.MinePendingAsync(W("Alice").Address);
            var balanceAfter = _chain.GetBalance(carol.Address);

            bool passed = rejected >= 1 && reserved <= balance && mined.Success && balanceAfter >= 0;
            return new AuditResult("Double spending", passed,
                $"баланс {balance}, 4 × {amount}: прийнято {accepted}, відхилено {rejected}; " +
                $"після майнінгу баланс {balanceAfter}");
        }

        //3. Самостійно створена Coinbase на 1000 монет
        private AuditResult CheckFakeCoinbase()
        {
            var eve = W("Eve");
            var before = _chain.GetBalance(eve.Address);

            var fake = new Transaction("Coinbase", eve.Address, 1000) { IsCoinbase = true };
            var viaMempool = _chain.AddTransaction(fake);
            var viaBlock = _chain.AddBlock(new List<Transaction> { fake }, eve.Address);

            var after = _chain.GetBalance(eve.Address);
            bool passed = !viaMempool.isSuccess && !viaBlock.Success && after == before;
            return new AuditResult("Fake Coinbase", passed,
                $"баланс Eve: {before} → {after}");
        }

        //4. Bob переказує кошти Alice, підписавши транзакцію своїм ключем
        private AuditResult CheckInvalidSender()
        {
            var alice = W("Alice");
            var bob = W("Bob");

            //а) адреса Alice + публічний ключ і підпис Bob
            var forged1 = new Transaction(alice.Address, bob.Address, 10) { SenderPublicKey = bob.PublicKey };
            forged1.Signature = bob.Sign(forged1.GetDataToSign());

            //б) адреса і публічний ключ Alice, але підпис Bob
            var forged2 = new Transaction(alice.Address, bob.Address, 10) { SenderPublicKey = alice.PublicKey };
            forged2.Signature = bob.Sign(forged2.GetDataToSign());

            //Контроль: справжня транзакція Alice має пройти
            var genuine = _transactionService.CreateTransaction(alice, bob.Address, 10);

            var r1 = _chain.AddTransaction(forged1);
            var r2 = _chain.AddTransaction(forged2);
            var r3 = _chain.AddTransaction(genuine);
            _chain.ClearMempool();

            bool passed = !r1.isSuccess && !r2.isSuccess && r3.isSuccess;
            return new AuditResult("Invalid sender", passed,
                $"підробка (свій ключ): {(r1.isSuccess ? "прийнята" : "відхилена")}; " +
                $"підробка (ключ Alice): {(r2.isSuccess ? "прийнята" : "відхилена")}; " +
                $"справжня Alice: {(r3.isSuccess ? "прийнята" : "відхилена")}");
        }

        //5. Повторне надсилання вже підтвердженої транзакції при достатньому балансі
        private async Task<AuditResult> CheckReplayAttack()
        {
            var dave = W("Dave");
            var alice = W("Alice");

            var tx = _transactionService.CreateTransaction(dave, alice.Address, 5);
            _chain.AddTransaction(tx);
            var mined = await _chain.MinePendingAsync(alice.Address);
            bool confirmed = mined.Success && _chain.ContainsTransaction(tx.Id);

            var balance = _chain.GetBalance(dave.Address);   //коштів на повтор вистачає
            int blocksBefore = _chain.Chain.Count;

            var againMempool = _chain.AddTransaction(tx);
            var againBlock = _chain.AddBlock(new List<Transaction> { tx }, alice.Address);

            bool passed = confirmed && balance >= tx.Amount
                       && !againMempool.isSuccess && !againBlock.Success
                       && _chain.Chain.Count == blocksBefore;
            return new AuditResult("Replay attack", passed,
                $"транзакцію підтверджено: {confirmed}; баланс Dave {balance} ≥ {tx.Amount}; " +
                $"повтор: mempool {(againMempool.isSuccess ? "прийняв" : "відхилив")}, " +
                $"блок {(againBlock.Success ? "створено" : "відхилено")}");
        }

        //6. Після збільшення складності старі блоки лишаються валідними
        private async Task<AuditResult> CheckDifficultyValidation()
        {
            var oldDifficulty = _chain.Difficulty;
            bool validBefore = _chain.IsChainValid();

            _chain.Difficulty = oldDifficulty + 2;
            var result = await _chain.AddBlockAsync(new List<Transaction>(), W("Eve").Address);
            if (!result.Success || result.Block == null)
            {
                return new AuditResult("Difficulty validation", false, "не вдалося замайнити блок з новою складністю");
            }

            bool oldBlocksKept = _chain.Chain
                .Where(b => b.Index > 0 && b.Index < result.Block.Index)
                .All(b => b.DifficultyAtMining == oldDifficulty);
            bool newBlockHarder = result.Block.DifficultyAtMining == _chain.Difficulty;
            bool validAfter = _chain.IsChainValid();

            bool passed = validBefore && oldBlocksKept && newBlockHarder && validAfter;
            return new AuditResult("Difficulty validation", passed,
                $"складність {oldDifficulty} → {_chain.Difficulty}; " +
                $"ланцюг валідний до: {validBefore}, після: {validAfter}");
        }

        //Гаманець тестового набору за ім'ям
        private Wallet W(string name)
        {
            return _wallets.FindByName(name)!;
        }

        //Якщо перевірка "впала" з винятком — це FAIL, а не крах усього аудиту
        private static async Task<AuditResult> Safe(string name, Func<Task<AuditResult>> check)
        {
            try
            {
                return await check();
            }
            catch (Exception ex)
            {
                return new AuditResult(name, false, $"виняток: {ex.Message}");
            }
        }
    }
}