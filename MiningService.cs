using BlockChainP32;
using System;
using System.Diagnostics;

namespace BlockChain_1
{
    internal class MiningService
    {
        private readonly HashingService _hashingService;

        public MiningService(HashingService hashingService)
        {
            _hashingService = hashingService;
        }

        public void MineBlock(Block block, int difficulty)
        {
    
            var target = new string('0', difficulty); 

            var stopwatch = Stopwatch.StartNew();


            long attempts = 0;
            while (true)
            {
                block.Hash = _hashingService.ComputeHash(block);
                attempts++;

                if (block.Hash.Substring(0, difficulty) == target) 
                {
                    stopwatch.Stop();
                    block.MiningDuration = stopwatch.Elapsed.TotalSeconds;
                    Console.WriteLine($"Hash found for block {block.Index}: {block.Hash}: {block.MiningDuration} seconds");
                    block.Attempts = attempts;
                    break;
                }
                if (block.Nonce % 100000 == 0)
                {
                    Console.WriteLine($"Mining block {block.Index}: Nonce={block.Nonce}, Hash={block.Hash}");
                }

                block.Nonce++;
            }
        }

        public void MineBlockParallel(Block block, int difficulty, int threadCount)
        {

            var target = new string('0', difficulty);
            var stopwatch = Stopwatch.StartNew();

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            var locker = new object();
            bool found = false;
            long totalAttempts = 0;

            var tasks = new Task[threadCount];

            for ( int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    var candidate = new Block
                    {
                        Index = block.Index,
                        TimeStamp = block.TimeStamp,
                        Data = block.Data,
                        Author = block.Author,
                        PreviousHash = block.PreviousHash,
                        Nonce = threadId,
                        DifficultyAtMining = block.DifficultyAtMining,
                    };
                    long localAttempts = 0;
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            var hash = _hashingService.ComputeHash(candidate);

                            if (hash.Substring(0, difficulty) == target)
                            {
                                lock (locker)
                                {
                                    if (!found)
                                    {
                                        found = true;
                                        block.Nonce = candidate.Nonce;
                                        block.Hash = hash;
                                        cts.Cancel();
                                    }
                                }
                                return;
                            }
                            candidate.Nonce += threadCount;
                        }
                    }
                    finally
                    {
                        
                        Interlocked.Add(ref totalAttempts, localAttempts);
                    }
                });
            }
            Task.WaitAll(tasks);
            block.Attempts = totalAttempts;

            stopwatch.Stop();
            block.MiningDuration = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"[Parallel x{threadCount}] Hash found for block {block.Index}: Nonce={block.Nonce}, {block.MiningDuration:F3} s");
        }

        public Task MineBlockAsync(Block block, int difficulty)
        {
            return Task.Run(() => MineBlock(block, difficulty));
        }
        public Task MineBlockParallelAsync(Block block, int difficulty, int threadCount)
        {
            return Task.Run(() => MineBlockParallel(block, difficulty, threadCount));
        }

    }
}
