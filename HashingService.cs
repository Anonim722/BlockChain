using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using BlockChain_1.Models;

namespace BlockChain_1
{
    public class HashingService
    {
        public string ComputeHash(Block block)
        {
            //Заголовок блоку: замість усіх транзакцій — один Merkle Root
            var blockData = $"{block.Index}{block.TimeStamp}{block.MerkleRoot}{block.PreviousHash}{block.Nonce}{block.DifficultyAtMining}";
            return ComputeHash(blockData);
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        //Усі рівні дерева Меркла: [0] — хеші транзакцій (листя), останній — корінь
        public List<List<string>> GetMerkleLevels(List<Transaction> transactions)
        {
            var levels = new List<List<string>>();
            if (transactions == null || transactions.Count == 0)
            {
                return levels;
            }

            //Листя: хеш кожної транзакції
            var hashes = transactions.Select(t => ComputeHash(t.ToRawString())).ToList();
            levels.Add(hashes);

            //Піднімаємось угору, поки не залишиться один хеш
            while (hashes.Count > 1)
            {
                var newHashes = new List<string>();
                for (int i = 0; i < hashes.Count; i += 2)
                {
                    if (i + 1 < hashes.Count)
                    {
                        //Об'єднуємо два хеші та обчислюємо новий хеш
                        newHashes.Add(ComputeHash(hashes[i] + hashes[i + 1]));
                    }
                    else
                    {
                        //Якщо кількість хешів непарна — останній переносимо без змін
                        newHashes.Add(hashes[i]);
                    }
                }
                hashes = newHashes;
                levels.Add(hashes);
            }

            return levels;
        }

        //Корінь Меркла (порожній рядок, якщо транзакцій немає)
        public string ComputeMerkleRoot(List<Transaction> transactions)
        {
            var levels = GetMerkleLevels(transactions);
            return levels.Count == 0 ? string.Empty : levels[levels.Count - 1][0];
        }
    }
}
