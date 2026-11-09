using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Text;

namespace BlockChainP32
{
    public class BlockChain
    {
        //Список блоків у ланцюжку
        public List<Block> Chain { get; set; }

        //Хешування сервіс для обчислення хешів
        private readonly HashingService _hashingService;

        //Конструктор класу BlockChain
        public BlockChain()
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
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
                PreviousHash = "0"
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
                PreviousHash = lastBlock.Hash
            };

            newBlock.Hash = _hashingService.ComputeHash(newBlock);

            Chain.Add(newBlock);
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

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
