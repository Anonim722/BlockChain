using BlockChain_1.Service;
using BlockChain_1;
using BlockChain_1.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var blockChain = new BlockChain(4);
var displayService = new BlockChainDisplayService();
var transactionService = new TransactionService();

var walletService = new WalletService();
var walletManager = new WalletManager();
var walletDisplay = new WalletDisplayService();

foreach (var name in new[] { "Alice", "Bob", "Charlie", "Dave", "Eve" })
{
    walletManager.CreateWallet(name);
}

//Гаманець за ім'ям або зрозуміла помилка
Wallet GetWallet(string name)
{
    return walletManager.FindByName(name)
        ?? throw new ArgumentException($"Гаманець \"{name}\" не знайдено");
}

//Підписана транзакція "з імені в ім'я"
Transaction Tx(string from, string to, decimal amount)
{
    return transactionService.CreateTransaction(GetWallet(from), GetWallet(to).Address, amount);
}


//Гаманці за іменем (без урахування регістру)
var wallets = new Dictionary<string, Wallet>(StringComparer.OrdinalIgnoreCase);
foreach (var name in new[] { "Alice", "Bob", "Charlie", "Dave", "Eve" })
{
    wallets[name] = walletService.CreateWallet(name);
    Console.WriteLine($"{name,-8} адреса: {wallets[name].Address}");
}


var hashingService = new HashingService();   //"зловмиснику" алгоритм хешування відомий

Console.WriteLine("Створюємо початковий ланцюг...");
blockChain.AddBlock(new List<Transaction>
{
    Tx("Alice", "Bob", 10),
    Tx("Bob", "Charlie", 5),
    Tx("Charlie", "Dave", 2)
});
blockChain.AddBlock(new List<Transaction>
{
    Tx("Dave", "Alice", 7),
    Tx("Eve", "Bob", 15),
    Tx("Alice", "Eve", 3.5m)
});
blockChain.AddBlock(new List<Transaction>
{
    Tx("Bob", "Alice", 4),
    Tx("Charlie", "Eve", 12),
    Tx("Eve", "Dave", 1.25m)
});
blockChain.AddBlock(new List<Transaction>
{
    Tx("Alice", "Charlie", 20),
    Tx("Dave", "Bob", 6),
    Tx("Bob", "Eve", 9)
});

//Транзакції, що чекають на включення в блок
var listTransaction = new List<Transaction>();

while (true)
{
    Console.WriteLine();
    Console.WriteLine($"Menu (в черзі транзакцій: {listTransaction.Count})");
    Console.WriteLine("addTr | add | display | validate | history | find | check | tamper | hide | remine | repair | experiment | rejectTest | clearTr | exit");
    Console.Write("> ");
    var enter = Console.ReadLine();

    if (enter == "exit")
    {
        break;
    }

    if (enter == "addTr")
    {
        Console.Write("Від (ім'я): ");
        var fromName = Console.ReadLine() ?? "";
        Console.Write("Кому (ім'я): ");
        var toName = Console.ReadLine() ?? "";

        if (!wallets.TryGetValue(fromName, out var fromWallet) ||
            !wallets.TryGetValue(toName, out var toWallet))
        {
            Console.WriteLine($"Гаманець не знайдено. Доступні: {string.Join(", ", wallets.Keys)}");
            continue;
        }

        Console.Write("Сума: ");
        if (!decimal.TryParse(Console.ReadLine(), out var amount))
        {
            Console.WriteLine("Сума має бути числом.");
            continue;
        }

        try
        {
            listTransaction.Add(transactionService.CreateTransaction(fromWallet, toWallet.Address, amount));
            Console.WriteLine("Транзакцію підписано і додано в чергу.");
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

        var result = await blockChain.AddBlockAsync(listTransaction);
        displayService.PrintBlockCreationResult(result);

        if (result.Success)
        {
            listTransaction.Clear(); //очищаємо чергу тільки після успіху
        }
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
        var address = wallets.TryGetValue(name, out var w) ? w.Address : name;
        var found = blockChain.FindBlocksByParticipant(address);
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
                Tx("Alice", "Bob", b)
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
    else if (enter == "clearTr")
    {
        listTransaction.Clear();
        Console.WriteLine("Чергу очищено.");
    }
    else if (enter == "rejectTest")
    {
        var existingTx = blockChain.Chain[1].Transactions[0];                 //вже є в ланцюгу
        var repeatedTx = Tx("Alice", "Bob", 1);

        var scenarios = new List<(string name, List<Transaction> txs)>
    {
        ("5 транзакцій, одна з від'ємною сумою", new List<Transaction>
        {
            Tx("Alice", "Bob", 1),
            Tx("Bob", "Charlie", 2),
            new Transaction("Eve", "Dave", -5),        //в обхід сервісу — неправильні дані
            Tx("Dave", "Alice", 3),
            Tx("Charlie", "Eve", 4)
        }),
        ("Повтор Id всередині блоку", new List<Transaction>
        {
            repeatedTx,
            Tx("Bob", "Eve", 2),
            repeatedTx                                  //той самий об'єкт двічі
        }),
        ("Транзакція вже є в Blockchain", new List<Transaction>
        {
            Tx("Eve", "Alice", 5),
            existingTx
        })
    };

        foreach (var (name, txs) in scenarios)
        {
            int blocksBefore = blockChain.Chain.Count;
            int miningBefore = blockChain.MiningRunsCount;

            Console.WriteLine($"\n=== {name} ({txs.Count} транзакцій) ===");
            var result = blockChain.AddBlock(txs);
            displayService.PrintBlockCreationResult(result);

            bool blocksSame = blockChain.Chain.Count == blocksBefore;
            bool miningSame = blockChain.MiningRunsCount == miningBefore;

            Console.WriteLine($"Блоків: {blocksBefore} -> {blockChain.Chain.Count} " +
                              (blocksSame ? "✔ не змінилось" : "✘ змінилось!"));
            Console.WriteLine($"Запусків майнінгу: {miningBefore} -> {blockChain.MiningRunsCount} " +
                              (miningSame ? "✔ майнінг не запускався" : "✘ майнінг запускався!"));
        }

        Console.WriteLine($"\nIs Valid: {blockChain.IsChainValid()}");
    }
    else if (enter == "signTest")
    {
        //1. Суму змінили ПІСЛЯ підпису
        var changed = Tx("Alice", "Bob", 5);
        changed.Amount = 500;

        //2. Eve підписує СВОЇМ ключем, але вказує адресу Alice
        var stolen = new Transaction(wallets["Alice"].Address, wallets["Eve"].Address, 50);
        stolen.SenderPublicKey = wallets["Eve"].PublicKey;
        stolen.Signature = wallets["Eve"].Sign(stolen.GetDataToSign());

        //3. Копія чужої транзакції з НОВИМ Id (спроба обійти перевірку повтору)
        var original = blockChain.Chain[1].Transactions[0];
        var replay = new Transaction(original.From, original.To, original.Amount); //Id новий
        replay.TimeStamp = original.TimeStamp;
        replay.SenderPublicKey = original.SenderPublicKey;
        replay.Signature = original.Signature;                                     //чужий підпис

        //4. Зовсім без підпису
        var unsigned = new Transaction(wallets["Bob"].Address, wallets["Alice"].Address, 1);

        var cases = new (string name, Transaction tx)[]
        {
        ("Сума змінена після підпису", changed),
        ("Eve підписала від імені Alice", stolen),
        ("Копія транзакції з новим Id", replay),
        ("Без підпису", unsigned)
        };

        foreach (var (name, tx) in cases)
        {
            Console.WriteLine($"\n=== {name} ===");
            displayService.PrintBlockCreationResult(blockChain.AddBlock(new List<Transaction> { tx }));
        }

        Console.WriteLine("\n=== Чесна транзакція Alice -> Bob ===");
        displayService.PrintBlockCreationResult(blockChain.AddBlock(new List<Transaction> { Tx("Alice", "Bob", 1) }));
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