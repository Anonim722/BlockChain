using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


using BlockChain_1.Models;
using BlockChain_1.Service;

namespace BlockChain_1
{
    public class BlockChain
    {
        //Список блоків у ланцюжку
        public List<Block> Chain { get; set; }
        public int Difficulty { get; set; } = 4;
        public int MiningRunsCount { get; private set; }
        private readonly int _adjustmentInterval = 3;
        private readonly bool _autoAdjust;
        private readonly double _targetBlockTime = 1.0;
        private readonly MiningService _miningService;
        //Сервіс для обробки транзакцій
        private readonly TransactionService _transactionService;

        //Хешування сервіс для обчислення хешів
        private readonly HashingService _hashingService;

        //Конструктор класу BlockChain
        public BlockChain(int difficulty = 4, bool autoAdjust = true)
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
            _transactionService = new TransactionService();
            Difficulty = difficulty;
            //Додаємо генезис-блок до ланцюжка
            AddGenesisBlock();
            _autoAdjust = autoAdjust;
        }

        private void AddGenesisBlock()
        {
            //Створюємо перший блок (генезис-блок) з індексом 0 та порожніми даними
            var genesisBlock = new Block
            {
                Index = 0,
                TimeStamp = DateTime.UtcNow,
                Transactions = new List<Transaction>(),
                PreviousHash = "0",
                DifficultyAtMining = 0
            };
            //Обчислюємо хеш генезис-блоку
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);
            //Додаємо генезис-блок до ланцюжка
            Chain.Add(genesisBlock);
        }

        public BlockCreationResult AddBlock(List<Transaction> transactions)
        {
            var result = new BlockCreationResult();

            //1. Спочатку перевіряємо ВСЕ
            result.Errors.AddRange(ValidateTransactionsForNewBlock(transactions));
            if (!result.Success)
            {
                return result; //відмова: блок не створено, майнінг не запускався
            }

            //2. Тільки тепер — створення і майнінг
            var newBlock = CreateNewBlock(transactions);
            MiningRunsCount++;
            _miningService.MineBlock(newBlock, Difficulty);
            Chain.Add(newBlock);

            if (_autoAdjust && newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }

            result.Block = newBlock;
            return result;
        }

        public async Task<BlockCreationResult> AddBlockAsync(List<Transaction> transactions)
        {
            var result = new BlockCreationResult();

            result.Errors.AddRange(ValidateTransactionsForNewBlock(transactions));
            if (!result.Success)
            {
                return result;
            }

            var newBlock = CreateNewBlock(transactions);
            MiningRunsCount++;
            await _miningService.MineBlockParallelAsync(newBlock, Difficulty, Environment.ProcessorCount);
            Chain.Add(newBlock);

            if (_autoAdjust && newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }

            result.Block = newBlock;
            return result;
        }

        //Перевіряє ВЕСЬ список транзакцій нового блоку. Повертає всі знайдені проблеми
        public List<string> ValidateTransactionsForNewBlock(List<Transaction> transactions)
        {
            var errors = new List<string>();

            if (transactions == null || transactions.Count == 0)
            {
                errors.Add("Список транзакцій порожній");
                return errors;
            }

            //Id усіх транзакцій, які вже є в ланцюгу -> номер блоку
            var existingIds = new Dictionary<string, int>();
            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    existingIds.TryAdd(tx.Id, block.Index);
                }
            }

            //Id, які вже зустрілися в новому блоці
            var seenIds = new HashSet<string>();

            for (int i = 0; i < transactions.Count; i++)
            {
                var tx = transactions[i];

                if (tx == null)
                {
                    errors.Add($"Транзакція #{i + 1}: відсутня (null)");
                    continue;
                }

                var label = $"Транзакція #{i + 1} ({tx.From} -> {tx.To}: {tx.Amount})";

                //1. Правильність даних
                var (isValid, errorMessage) = _transactionService.ValidateTransaction(tx);
                if (!isValid)
                {
                    errors.Add($"{label}: {errorMessage}");
                }

                //2. Id взагалі заданий
                if (string.IsNullOrWhiteSpace(tx.Id))
                {
                    errors.Add($"{label}: порожній Id");
                    continue; //далі перевіряти Id немає сенсу
                }

                //3. Повтор Id всередині нового блоку
                if (!seenIds.Add(tx.Id))
                {
                    errors.Add($"{label}: Id {Short(tx.Id)} повторюється всередині нового блоку");
                }

                //4. Така транзакція вже є в Blockchain
                if (existingIds.TryGetValue(tx.Id, out var blockIndex))
                {
                    errors.Add($"{label}: транзакція з Id {Short(tx.Id)} вже є в блоці #{blockIndex}");
                }
            }

            return errors;
        }

        //Перевіряє транзакції і готує новий блок (ще без майнінгу)
        //Готує новий блок (без перевірки і без майнінгу)
        private Block CreateNewBlock(List<Transaction> transactions)
        {
            var transactionCopy = new List<Transaction>(transactions);

            var lastBlock = Chain[Chain.Count - 1];
            return new Block
            {
                Index = lastBlock.Index + 1,
                TimeStamp = DateTime.UtcNow,
                Transactions = transactionCopy,
                PreviousHash = lastBlock.Hash,
                DifficultyAtMining = Difficulty
            };
        }

        public Block? FindBlockByIndex(int index)
        {
            return Chain.FirstOrDefault(block => block.Index == index);
        }

        //Блоки, де людина була відправником або отримувачем
        public List<Block> FindBlocksByParticipant(string name)
        {
            return Chain
                .Where(block => block.Transactions.Any(tx =>
                    tx.From.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    tx.To.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        //Знаходить перший пошкоджений блок і причину. Якщо все добре — IsValid = true
        public BlockValidationResult FindFirstInvalidBlock()
        {
            for (int i = 0; i < Chain.Count; i++)
            {
                var block = Chain[i];

                //1. Чи відповідає збережений Hash даним блоку
                var recalculated = _hashingService.ComputeHash(block);
                if (block.Hash != recalculated)
                {
                    return new BlockValidationResult
                    {
                        BlockIndex = block.Index,
                        Error = ValidationError.InvalidHash,
                        Message = $"Блок #{block.Index}: збережений Hash {Short(block.Hash)} " +
                                  $"не відповідає обчисленому {Short(recalculated)} — дані змінено"
                    };
                }

                //2. Чи не порушено зв'язок з попереднім блоком
                if (i > 0 && block.PreviousHash != Chain[i - 1].Hash)
                {
                    return new BlockValidationResult
                    {
                        BlockIndex = block.Index,
                        Error = ValidationError.BrokenPreviousHash,
                        Message = $"Блок #{block.Index}: PreviousHash {Short(block.PreviousHash)} " +
                                  $"не дорівнює Hash блоку #{Chain[i - 1].Index} ({Short(Chain[i - 1].Hash)})"
                    };
                }

                //3. Proof of Work — за складністю, з якою блок майнили
                var target = new string('0', block.DifficultyAtMining);
                if (!block.Hash.StartsWith(target))
                {
                    return new BlockValidationResult
                    {
                        BlockIndex = block.Index,
                        Error = ValidationError.InvalidProofOfWork,
                        Message = $"Блок #{block.Index}: Hash {Short(block.Hash)} не починається " +
                                  $"з {block.DifficultyAtMining} нулів — Proof of Work не виконано"
                    };
                }
            }

            return new BlockValidationResult(); //пошкоджень немає
        }


        //Перемайнює блок startIndex і всі наступні, відновлюючи PreviousHash
        public RepairResult RepairChain(int startIndex)
        {
            if (startIndex < 1 || startIndex >= Chain.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    $"startIndex має бути від 1 до {Chain.Count - 1}");
            }

            var result = new RepairResult();
            var stopwatch = Stopwatch.StartNew();

            for (int i = startIndex; i < Chain.Count; i++)
            {
                var block = Chain[i];

                //Відновлюємо зв'язок: посилання на (вже новий) Hash попереднього
                block.PreviousHash = Chain[i - 1].Hash;
                block.Nonce = 0;

                //Майнимо з ПОЧАТКОВОЮ складністю цього блоку
                _miningService.MineBlockParallel(block, block.DifficultyAtMining, Environment.ProcessorCount);

                result.BlocksRemined++;
                result.TotalAttempts += block.Attempts;
            }

            stopwatch.Stop();
            result.DurationSeconds = stopwatch.Elapsed.TotalSeconds;
            result.IsValidAfter = IsChainValid();
            return result;
        }

        //Валідність = немає жодного пошкодженого блоку
        public bool IsChainValid()
        {
            return FindFirstInvalidBlock().IsValid;
        }

        //Скорочений хеш для повідомлень
        private static string Short(string hash)
        {
            return hash.Length > 12 ? hash.Substring(0, 12) + "..." : hash;
        }

        private void AdjustDifficulty()
        {
            var recent = Chain.Where(b => b.Index > 0).TakeLast(_adjustmentInterval).ToList();
            if (recent.Count == 0) return;

            var avgTime = recent.Average(b => b.MiningDuration);

            if (avgTime < _targetBlockTime)
            {
                Difficulty++;
                Console.WriteLine($"Difficulty increased to {Difficulty}");
            }
            else if (avgTime > _targetBlockTime)
            {
                Difficulty = Math.Max(1, Difficulty - 1);
                Console.WriteLine($"Difficulty decreased to {Difficulty}");
            }
        }
    }
}
