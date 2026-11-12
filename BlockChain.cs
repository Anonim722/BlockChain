using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Text;
using BlockChain_1;

namespace BlockChainP32
{
    public class BlockChain
    {
        //Список блоків у ланцюжку
        public List<Block> Chain { get; set; }
        public int Difficulty { get; set; } = 4;
        private readonly int _adjustmentInterval = 3;      
        private readonly double _targetBlockTime = 1.0;
        private readonly MiningService _miningService;

        //Хешування сервіс для обчислення хешів
        private readonly HashingService _hashingService;

        //Конструктор класу BlockChain
        public BlockChain()
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
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
                Data = "",
                Author = "System",
                PreviousHash = "0",
                DifficultyAtMining = 0
            };
            //Обчислюємо хеш генезис-блоку
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);
            //Додаємо генезис-блок до ланцюжка
            Chain.Add(genesisBlock);
        }

        public void AddBlock(string data, string author)
        {
            var lastBlock = Chain[Chain.Count - 1];

            var newBlock = new Block
            {
                Index = lastBlock.Index + 1,
                TimeStamp = DateTime.UtcNow,
                Data = data,
                Author = author,
                PreviousHash = lastBlock.Hash,
                DifficultyAtMining = Difficulty
            };

            _miningService.MineBlock(newBlock, Difficulty);

            Chain.Add(newBlock);

            if (newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }
        }

        public async Task AddBlockAsync(string data, string author)
        {
            var lastBlock = Chain[Chain.Count - 1];
            var newBlock = new Block
            {
                Index = lastBlock.Index + 1,
                TimeStamp = DateTime.UtcNow,
                Data = data,
                Author = author,
                PreviousHash = lastBlock.Hash
        };

            //Чекаємо майнінг, не блокуючи програму
            await _miningService.MineBlockParallelAsync(newBlock, Difficulty, Environment.ProcessorCount);

            Chain.Add(newBlock);
            if (newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }
        }

        public Block? FindBlockByIndex(int index)
        {
            return Chain.FirstOrDefault(block => block.Index == index);
        }

        public List<Block> FindBlocksByData(string text)
        {
            return Chain
                .Where(block => block.Data.Contains(text, StringComparison.OrdinalIgnoreCase))
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

                var target = new string('0', currentBlock.DifficultyAtMining);
                if (!currentBlock.Hash.StartsWith(target))
                {
                    return false;
                }

                if (currentBlock.Hash.Substring(0, Difficulty) != new string('0', Difficulty))
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
