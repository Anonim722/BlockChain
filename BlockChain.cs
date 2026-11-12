using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BlockChain_1.Models;
using BlockChain_1.Service;

namespace BlockChain_1
{
    public class BlockChain
    {
        //Список блоків у ланцюжку
        public List<Block> Chain { get; set; }
        public int Difficulty { get; set; } = 4;
        private readonly int _adjustmentInterval = 3;      
        private readonly double _targetBlockTime = 1.0;
        private readonly MiningService _miningService;
        //Сервіс для обробки транзакцій
        private readonly TransactionService _transactionService;

        //Хешування сервіс для обчислення хешів
        private readonly HashingService _hashingService;

        //Конструктор класу BlockChain
        public BlockChain(int difficulty = 4)
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
            _transactionService = new TransactionService();
            Difficulty = difficulty;
            //Додаємо генезис-блок до ланцюжка
            AddGenesisBlock();
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

        public void AddBlock(List<Transaction> transactions)
        {
            var newBlock = CreateNewBlock(transactions);
            _miningService.MineBlock(newBlock, Difficulty);
            Chain.Add(newBlock);

            if (newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }
        }

        public async Task AddBlockAsync(List<Transaction> transactions)
        {
            var newBlock = CreateNewBlock(transactions);
            await _miningService.MineBlockParallelAsync(newBlock, Difficulty, Environment.ProcessorCount);
            Chain.Add(newBlock);

            if (newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }
        }
        //Перевіряє транзакції і готує новий блок (ще без майнінгу)
        private Block CreateNewBlock(List<Transaction> transactions)
        {
            foreach (var tx in transactions)
            {
                var result = _transactionService.ValidateTransaction(tx);
                if (!result.isValid)
                {
                    Console.WriteLine($"Invalid transaction: {tx}");
                    throw new ArgumentException($"{result.errorMessage}: {tx}");
                }
            }

            //Копія списку — щоб зміни ззовні не зіпсували блок
            var transactionCopy = new List<Transaction>(transactions);

            var lastBlock = Chain[Chain.Count - 1];
            return new Block
            {
                Index = lastBlock.Index + 1,
                TimeStamp = DateTime.UtcNow,
                Transactions = transactionCopy,          // ← копія, а не transactions!
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
        public bool IsChainValid()
        {

            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock))
                {
                    return false;
                }

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }

                var target = new string('0', currentBlock.DifficultyAtMining);
                if (!currentBlock.Hash.StartsWith(target))
                {
                    return false;
                }

            }
            return true;
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
