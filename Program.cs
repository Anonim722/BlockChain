using BlockChain_1;
using BlockChainP32;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var miningService = new MiningService(new HashingService());
int difficulty = 5;
int threads = Environment.ProcessorCount;

//Однаковий тестовий блок для чесного порівняння
Block CreateTestBlock() => new Block
{
    Index = 1,
    TimeStamp = new DateTime(2026, 11, 9),
    Data = "Test block",
    PreviousHash = "0"
};

Console.WriteLine($"Складність: {difficulty}, потоків: {threads}");
Console.WriteLine(new string('=', 50));

//===== 1. Звичайний майнінг =====
var block1 = CreateTestBlock();
miningService.MineBlock(block1, difficulty);

//===== 2. Багатопотоковий майнінг =====
var block2 = CreateTestBlock();
miningService.MineBlockParallel(block2, difficulty, threads);

Console.WriteLine(new string('=', 50));
Console.WriteLine($"Звичайний:      {block1.MiningDuration:F3} s (Nonce={block1.Nonce})");
Console.WriteLine($"Багатопотоковий: {block2.MiningDuration:F3} s (Nonce={block2.Nonce})");
Console.WriteLine($"Прискорення:    x{block1.MiningDuration / block2.MiningDuration:F2}");

//===== 3. Асинхронність: програма "живе" під час майнінгу =====
Console.WriteLine();
Console.Write("Асинхронний майнінг");
var block3 = CreateTestBlock();
block3.Data = "Async block";

var miningTask = miningService.MineBlockParallelAsync(block3, difficulty, threads);
while (!miningTask.IsCompleted)
{
    Console.Write(".");          //головний потік вільний і працює
    await Task.Delay(200);
}
await miningTask;

Console.WriteLine();
var blockChain = new BlockChain();
var displayService = new BlockChainDisplayService();

string[] authors = { "Alex", "Bob", "Charlie" };
for (int i = 1; i <= 9; i++)
{
    await blockChain.AddBlockAsync($"Transaction #{i}", authors[i % authors.Length]);
}

Console.WriteLine();
displayService.PrintDifficultyHistory(blockChain);

//Перевіряємо, що складність дійсно змінювалась
var levels = blockChain.Chain
    .Where(b => b.Index > 0)
    .Select(b => b.DifficultyAtMining)
    .Distinct()
    .ToList();

Console.WriteLine();
Console.WriteLine($"Рівні складності: {string.Join(" -> ", levels)}");
Console.WriteLine(levels.Count > 1 ? "Складність змінювалась ✔" : "Складність НЕ змінювалась ✘");
Console.WriteLine("Is Valid: " + blockChain.IsChainValid());