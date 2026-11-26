using BlockChain_1.Service;
using BlockChain_1.Models;

namespace BlockChain_1
{
    public class BlockChainDisplayService
    {
        public void DisplayChain(BlockChain blockChain)
        {
            Console.WriteLine("Blockchain:");
            foreach (var block in blockChain.Chain)
            {
                DisplayBlock(block);
            }


        }
        public void DisplayBlock(Block block)
        {
            Console.WriteLine($"Index: {block.Index}");
            Console.WriteLine($"Timestamp: {block.TimeStamp}");
            Console.WriteLine($"Nonce: {block.Nonce}");
            Console.WriteLine($"Mining Duration: {block.MiningDuration} seconds");
            Console.WriteLine($"Transactions ({block.Transactions.Count}):");
            foreach (var tx in block.Transactions)
            {
                Console.WriteLine($"   - {tx.ToRawString()}");
            }
            Console.WriteLine($"Merkle Root: {block.MerkleRoot}");
            Console.WriteLine($"Previous Hash: {block.PreviousHash}");
            Console.WriteLine($"Hash: {block.Hash}");
            Console.WriteLine(new string('-', 50));
        }
        public void PrintDifficultyHistory(BlockChain blockChain)
        {
            Console.WriteLine("Історія складності:");
            Console.WriteLine($"{"Index",-6} | {"Difficulty",-10} | {"Attempts",-12} | {"Duration (s)",-12} | Hash");
            Console.WriteLine(new string('-', 80));

            foreach (var block in blockChain.Chain)
            {
                Console.WriteLine(
                    $"{block.Index,-6} | {block.DifficultyAtMining,-10} | {block.Attempts,-12} | " +
                    $"{block.MiningDuration,-12:F3} | {block.Hash.Substring(0, 16)}...");
            }
        }

        public void PrintValidationResult(BlockValidationResult r)
        {
            if (r.IsValid)
            {
                Console.WriteLine("✔ Ланцюг валідний, пошкоджень не знайдено.");
                return;
            }

            Console.WriteLine($"✘ Перший пошкоджений блок: #{r.BlockIndex}");
            Console.WriteLine($"  Причина: {r.Error}");
            Console.WriteLine($"  {r.Message}");
        }

        public void PrintRepairResult(RepairResult r)
        {
            Console.WriteLine("Відновлення завершено:");
            Console.WriteLine($"  Перемайнено блоків: {r.BlocksRemined}");
            Console.WriteLine($"  Загальна кількість спроб: {r.TotalAttempts}");
            Console.WriteLine($"  Витрачений час: {r.DurationSeconds:F3} с");
            Console.WriteLine($"  IsValid: {r.IsValidAfter}");
        }

        public void PrintBlockCreationResult(BlockCreationResult r)
        {
            if (r.Success)
            {
                Console.WriteLine($"✔ Блок #{r.Block!.Index} створено ({r.Block.Transactions.Count} транзакцій).");
                return;
            }

            Console.WriteLine($"✘ Блок відхилено. Причин: {r.Errors.Count}");
            foreach (var error in r.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        public void DisplayMempool(IReadOnlyList<Transaction> pending, int maxPerBlock)
        {
            Console.WriteLine($"Mempool: {pending.Count} транзакцій (у наступний блок піде до {maxPerBlock})");

            for (int i = 0; i < pending.Count; i++)
            {
                var tx = pending[i];
                var mark = i < maxPerBlock ? "→" : " ";   //стрілка = піде в наступний блок
                Console.WriteLine($" {mark} {i + 1}. {Cut(tx.From)} -> {Cut(tx.To)}: {tx.Amount}   Id {Cut(tx.Id)}");
            }
        }

        private static string Cut(string s)
        {
            return s.Length > 8 ? s.Substring(0, 8) + "..." : s;
        }

        public void PrintVerificationResult(VerificationResult r)
        {
            Console.WriteLine($"Id:       {r.TransactionId}");
            Console.WriteLine($"Статус:   {(r.IsValid ? "✔ Valid" : "✘ Invalid")}");
            Console.WriteLine($"Причина:  {r.Reason}");
            Console.WriteLine($"Де:       {r.Location}");
            Console.WriteLine(new string('-', 50));
        }

        public void PrintVerificationReport(TransactionVerificationCenter center)
        {
            Console.WriteLine("===== Звіт центру перевірки =====");
            Console.WriteLine($"Перевірено транзакцій: {center.TotalChecked}");
            Console.WriteLine($"Прийнято:              {center.AcceptedCount}");
            Console.WriteLine($"Відхилено:             {center.RejectedCount}");

            var byReason = center.Log
                .Where(r => !r.IsValid)
                .GroupBy(r => r.Reason)
                .ToList();

            if (byReason.Count > 0)
            {
                Console.WriteLine("Причини відмов:");
                foreach (var group in byReason)
                {
                    Console.WriteLine($"  {group.Count()} × {group.Key}");
                }
            }
        }

        public void PrintSecurityAudit(List<AuditResult> results)
        {
            Console.WriteLine();
            Console.WriteLine("========== SECURITY AUDIT ==========");
            foreach (var r in results)
            {
                Console.WriteLine($"{r.Name,-26}{(r.Passed ? "PASS" : "FAIL")}");
            }
            Console.WriteLine(new string('=', 36));

            int passed = results.Count(r => r.Passed);
            int percent = results.Count == 0 ? 0 : passed * 100 / results.Count;
            Console.WriteLine($"Security score: {passed}/{results.Count} ({percent}%)");

            //Деталі — чому саме PASS/FAIL
            Console.WriteLine("\nДеталі:");
            foreach (var r in results)
            {
                Console.WriteLine($"  [{(r.Passed ? "PASS" : "FAIL")}] {r.Name}: {r.Details}");
            }
        }
    }
}

