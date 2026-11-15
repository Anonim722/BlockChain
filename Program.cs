using BlockChain_1.Service;
using BlockChain_1;
using BlockChain_1.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var blockChain = new BlockChain(4);
var displayService = new BlockChainDisplayService();
var transactionService = new TransactionService();

var hashingService = new HashingService();   //"зловмиснику" алгоритм хешування відомий

Console.WriteLine("Створюємо початковий ланцюг...");
blockChain.AddBlock(new List<Transaction>
{
    transactionService.CreateTransaction("Alice", "Bob", 10),
    transactionService.CreateTransaction("Bob", "Charlie", 5),
    transactionService.CreateTransaction("Charlie", "Dave", 2)
});
blockChain.AddBlock(new List<Transaction>
{
    transactionService.CreateTransaction("Dave", "Alice", 7),
    transactionService.CreateTransaction("Eve", "Bob", 15),
    transactionService.CreateTransaction("Alice", "Eve", 3.5m)
});
blockChain.AddBlock(new List<Transaction>
{
    transactionService.CreateTransaction("Bob", "Alice", 4),
    transactionService.CreateTransaction("Charlie", "Eve", 12),
    transactionService.CreateTransaction("Eve", "Dave", 1.25m)
});
blockChain.AddBlock(new List<Transaction>
{
    transactionService.CreateTransaction("Alice", "Charlie", 20),
    transactionService.CreateTransaction("Dave", "Bob", 6),
    transactionService.CreateTransaction("Bob", "Eve", 9)
});

//Транзакції, що чекають на включення в блок
var listTransaction = new List<Transaction>();

while (true)
{
    Console.WriteLine();
    Console.WriteLine($"Menu (в черзі транзакцій: {listTransaction.Count})");
    Console.WriteLine("addTr | add | display | validate | history | find | check | tamper | hide | remine | repair | experiment | exit");
    Console.Write("> ");
    var enter = Console.ReadLine();

    if (enter == "exit")
    {
        break;
    }

    if (enter == "addTr")
    {
        Console.Write("From: ");
        var from = Console.ReadLine() ?? "";
        Console.Write("To: ");
        var to = Console.ReadLine() ?? "";
        Console.Write("Amount: ");
        if (!decimal.TryParse(Console.ReadLine(), out var amount))
        {
            Console.WriteLine("Amount має бути числом.");
            continue;
        }

        try
        {
            listTransaction.Add(transactionService.CreateTransaction(from, to, amount));
            Console.WriteLine("Transaction added.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
    }
    else if (enter == "add")
    {
        if (listTransaction.Count == 0)
        {
            Console.WriteLine("Немає транзакцій для блоку.");
            continue;
        }

        await blockChain.AddBlockAsync(listTransaction);
        listTransaction.Clear(); //безпечно: блок зберіг власну копію
        Console.WriteLine("Block added.");
    }
    else if (enter == "display")
    {
        displayService.DisplayChain(blockChain);
    }
    else if (enter == "validate")
    {
        Console.WriteLine("Is Valid: " + blockChain.IsChainValid());
    }
    else if (enter == "history")
    {
        displayService.PrintDifficultyHistory(blockChain);
    }
    else if (enter == "find")
    {
        Console.Write("Ім'я учасника: ");
        var name = Console.ReadLine() ?? "";
        var found = blockChain.FindBlocksByParticipant(name);
        Console.WriteLine($"Знайдено блоків: {found.Count}");
        foreach (var block in found)
        {
            displayService.DisplayBlock(block);
        }
    }
    else if (enter == "check")
    {
        displayService.PrintValidationResult(blockChain.FindFirstInvalidBlock());
    }
    else if (enter == "tamper")
    {
        //Атака, етап 1: змінюємо суму першої транзакції блоку
        var block = AskBlock(blockChain);
        if (block == null || block.Transactions.Count == 0) continue;

        Console.Write("Нова сума: ");
        if (!decimal.TryParse(Console.ReadLine(), out var newAmount)) continue;

        var tx = block.Transactions[0];
        Console.WriteLine($"Було: {tx.From} -> {tx.To}: {tx.Amount}");
        tx.Amount = newAmount;
        Console.WriteLine($"Стало: {tx.From} -> {tx.To}: {tx.Amount}");
    }
    else if (enter == "hide")
    {
        //Атака, етап 2: просто перераховуємо Hash, без майнінгу
        var block = AskBlock(blockChain);
        if (block == null) continue;

        block.Hash = hashingService.ComputeHash(block);
        Console.WriteLine($"Новий Hash блоку #{block.Index}: {block.Hash}");
    }
    else if (enter == "remine")
    {
        //Атака, етап 3: чесно перемайнюємо ТІЛЬКИ один блок
        var block = AskBlock(blockChain);
        if (block == null) continue;

        block.Nonce = 0;
        new MiningService(hashingService)
            .MineBlockParallel(block, block.DifficultyAtMining, Environment.ProcessorCount);
    }
    else if (enter == "repair")
    {
        var check = blockChain.FindFirstInvalidBlock();
        if (check.IsValid)
        {
            Console.WriteLine("Ланцюг і так валідний.");
            continue;
        }

        Console.WriteLine($"Відновлюємо з блоку #{check.BlockIndex}...");
        var repair = blockChain.RepairChain(check.BlockIndex);
        displayService.PrintRepairResult(repair);
    }
    else if (enter == "experiment")
    {
        //Як складність впливає на вартість підробки
        var results = new List<(int difficulty, RepairResult result)>();

        foreach (var d in new[] { 3, 4, 5 })
        {
            Console.WriteLine($"\n--- Складність {d} ---");
            var testChain = new BlockChain(d, autoAdjust: false);
            for (int b = 1; b <= 4; b++)
            {
                await testChain.AddBlockAsync(new List<Transaction>
            {
                transactionService.CreateTransaction("Alice", "Bob", b)
            });
            }

            testChain.Chain[1].Transactions[0].Amount = 1000; //підробка в блоці 1
            results.Add((d, testChain.RepairChain(1)));
        }

        Console.WriteLine();
        Console.WriteLine($"{"Difficulty",-10} | {"Блоків",-6} | {"Спроб",-12} | {"Час (с)",-8} | IsValid");
        Console.WriteLine(new string('-', 60));
        foreach (var (d, r) in results)
        {
            Console.WriteLine($"{d,-10} | {r.BlocksRemined,-6} | {r.TotalAttempts,-12} | {r.DurationSeconds,-8:F3} | {r.IsValidAfter}");
        }
    }
    else
    {
        Console.WriteLine("Unknown command. Please try again.");
    }
}

//Питає номер блоку і повертає його (або null)
static Block? AskBlock(BlockChain chain)
{
    Console.Write("Номер блоку: ");
    if (!int.TryParse(Console.ReadLine(), out var index))
    {
        Console.WriteLine("Потрібне число.");
        return null;
    }

    var block = chain.FindBlockByIndex(index);
    if (block == null)
    {
        Console.WriteLine($"Блок #{index} не знайдено.");
    }
    return block;
}