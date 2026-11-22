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
var center = new TransactionVerificationCenter(blockChain);

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



var hashingService = new HashingService();   //"зловмиснику" алгоритм хешування відомий

//Кожен гаманець майнить по блоку і отримує нагороду — це його стартові кошти
Console.WriteLine("Початкові кошти: кожен гаманець майнить блок...");
foreach (var w in walletManager.GetAll())
{
    blockChain.AddBlock(new List<Transaction>(), w.Address);
}

blockChain.AddBlock(new List<Transaction>
{
    Tx("Alice", "Bob", 10),
    Tx("Bob", "Charlie", 5),
    Tx("Charlie", "Dave", 2)
}, GetWallet("Alice").Address);
blockChain.AddBlock(new List<Transaction>
{
    Tx("Dave", "Alice", 7),
    Tx("Eve", "Bob", 15),
    Tx("Alice", "Eve", 3.5m)
}, GetWallet("Bob").Address);
blockChain.AddBlock(new List<Transaction>
{
    Tx("Bob", "Alice", 4),
    Tx("Charlie", "Eve", 12),
    Tx("Eve", "Dave", 1.25m) 
}, GetWallet("Charlie").Address);
blockChain.AddBlock(new List<Transaction>
{
    Tx("Alice", "Charlie", 20),
    Tx("Dave", "Bob", 6),
    Tx("Bob", "Eve", 9)
}, GetWallet("Dave").Address);



while (true)
{
    Console.WriteLine();
    Console.WriteLine($"Menu (у mempool: {blockChain.PendingTransactions.Count})");
    Console.WriteLine("addTr | add | display | validate | history | find | check | tamper | hide | remine | repair | experiment | rejectTest | signTest | clearTr | newWallet | wallets | findWallet | balance | balances | coinbaseTest | mempool | mempoolTest | vcSeed | verify | vcList | vcReport | forgeryTest | exit");
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

        var fromWallet = walletManager.FindByName(fromName);
        var toWallet = walletManager.FindByName(toName);

        if (fromWallet == null || toWallet == null)
        {
            Console.WriteLine($"Гаманець не знайдено. Доступні: {string.Join(", ", walletManager.GetAll().Select(x => x.Name))}");
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
            var tx = transactionService.CreateTransaction(fromWallet, toWallet.Address, amount);
            displayService.PrintVerificationResult(center.Submit(tx));
        }

        catch (ArgumentException ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
    }
    else if (enter == "add")
    {
        Console.Write("Хто майнить (ім'я): ");
        var miner = walletManager.FindByName(Console.ReadLine() ?? "");
        if (miner == null)
        {
            Console.WriteLine("Гаманець майнера не знайдено.");
            continue;
        }

        var result = await blockChain.MinePendingAsync(miner.Address);
        displayService.PrintBlockCreationResult(result);

        if (result.Success)
        {
            Console.WriteLine($"{miner.Name} отримав нагороду {BlockChain.MiningReward}. " +
                              $"У mempool лишилось: {blockChain.PendingTransactions.Count}");
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
        var address = walletManager.FindByName(name)?.Address ?? name;
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
            }, GetWallet("Dave").Address);
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
        blockChain.ClearMempool();
        Console.WriteLine("Mempool очищено.");
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
            var result = blockChain.AddBlock(txs, GetWallet("Alice").Address);
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
        var stolen = new Transaction(GetWallet("Alice").Address, GetWallet("Eve").Address, 50);
        stolen.SenderPublicKey = GetWallet("Eve").PublicKey;
        stolen.Signature = GetWallet("Eve").Sign(stolen.GetDataToSign());

        //3. Копія чужої транзакції з НОВИМ Id (спроба обійти перевірку повтору)
        var original = blockChain.Chain[1].Transactions[0];
        var replay = new Transaction(original.From, original.To, original.Amount); //Id новий
        replay.TimeStamp = original.TimeStamp;
        replay.SenderPublicKey = original.SenderPublicKey;
        replay.Signature = original.Signature;                                     //чужий підпис

        //4. Зовсім без підпису
        var unsigned = new Transaction(GetWallet("Bob").Address, GetWallet("Alice").Address, 1);

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
            displayService.PrintBlockCreationResult(blockChain.AddBlock(new List<Transaction> { tx }, GetWallet("Alice").Address));
        }

        Console.WriteLine("\n=== Чесна транзакція Alice -> Bob ===");
        displayService.PrintBlockCreationResult(blockChain.AddBlock(new List<Transaction> { Tx("Alice", "Bob", 1) }, GetWallet("Dave").Address));
    }
    else if (enter == "newWallet")
    {
        Console.Write("Ім'я нового гаманця: ");
        var (wallet, error) = walletManager.CreateWallet(Console.ReadLine() ?? "");

        if (wallet == null)
        {
            Console.WriteLine($"✘ {error}");
        }
        else
        {
            Console.WriteLine("✔ Гаманець створено:");
            walletDisplay.PrintWallet(wallet);
        }
    }
    else if (enter == "wallets")
    {
        walletDisplay.PrintWallets(walletManager.GetAll());
    }
    else if (enter == "findWallet")
    {
        Console.Write("Ім'я або адреса (від 4 символів): ");
        var query = (Console.ReadLine() ?? "").Trim();

        var byName = walletManager.FindByName(query);
        if (byName != null)
        {
            walletDisplay.PrintWallet(byName);
            continue;
        }

        var byAddress = walletManager.FindByAddress(query);
        if (byAddress.Count == 0)
        {
            Console.WriteLine($"Гаманець \"{query}\" не знайдено.");
        }
        else
        {
            walletDisplay.PrintWallets(byAddress);
        }
    }
    else if (enter == "balance")
    {
        Console.Write("Ім'я: ");
        var w = walletManager.FindByName(Console.ReadLine() ?? "");
        if (w == null)
        {
            Console.WriteLine("Гаманець не знайдено.");
            continue;
        }
        Console.WriteLine($"Баланс {w.Name}: {blockChain.GetBalance(w.Address)}");
    }
    else if (enter == "balances")
    {
        decimal total = 0;
        foreach (var w in walletManager.GetAll())
        {
            var b = blockChain.GetBalance(w.Address);
            total += b;
            Console.WriteLine($"{w.Name,-10} {b,10}");
        }
        Console.WriteLine($"{"Разом",-10} {total,10}");
    }
    else if (enter == "coinbaseTest")
    {
        var fake = new Transaction("Coinbase", GetWallet("Eve").Address, 1_000_000) { IsCoinbase = true };
        var r = blockChain.AddBlock(new List<Transaction> { fake }, GetWallet("Eve").Address);
        displayService.PrintBlockCreationResult(r);
        Console.WriteLine($"Баланс Eve: {blockChain.GetBalance(GetWallet("Eve").Address)}");
    }
    else if (enter == "mempool")
    {
        displayService.DisplayMempool(blockChain.PendingTransactions, blockChain.MaxTransactionsPerBlock);
    }
    else if (enter == "mempoolTest")
    {
        //1. П'ять транзакцій — у блок піде тільки 3
        foreach (var (from, to, sum) in new[] { ("Alice", "Bob", 1m), ("Bob", "Charlie", 2m),
             ("Charlie", "Dave", 3m), ("Dave", "Eve", 4m), ("Eve", "Alice", 5m) })
        {
            var (ok, msg) = blockChain.AddTransaction(Tx(from, to, sum));
            Console.WriteLine($"{from} -> {to}: {sum}  {(ok ? "✔" : "✘")} {msg}");
        }
        displayService.DisplayMempool(blockChain.PendingTransactions, blockChain.MaxTransactionsPerBlock);

        var mineResult = await blockChain.MinePendingAsync(GetWallet("Alice").Address);
        displayService.PrintBlockCreationResult(mineResult);
        Console.WriteLine($"Після майнінгу у mempool: {blockChain.PendingTransactions.Count}");

        //2. Подвійна витрата через mempool
        var bob = GetWallet("Bob");
        var half = Math.Floor(blockChain.GetBalance(bob.Address) / 2) + 1;   //трохи більше половини
        Console.WriteLine($"\nБаланс Bob: {blockChain.GetBalance(bob.Address)}, два перекази по {half}:");
        Console.WriteLine(blockChain.AddTransaction(Tx("Bob", "Eve", half)).message);
        Console.WriteLine(blockChain.AddTransaction(Tx("Bob", "Dave", half)).message);

        //3. Повторне додавання вже замайненої транзакції
        var mined = blockChain.Chain.SelectMany(b => b.Transactions).First(t => !t.IsCoinbase);
        Console.WriteLine($"\nПовтор замайненої: {blockChain.AddTransaction(mined).message}");
    }
    else if (enter == "vcSeed")
    {
        //Рівень 2: щонайменше 5 чесних транзакцій між різними користувачами
        Console.WriteLine("--- Чесні транзакції ---");
        var honest = new[]
        {
        ("Alice", "Bob", 3m), ("Bob", "Charlie", 2m), ("Charlie", "Dave", 4m),
        ("Dave", "Eve", 1m), ("Eve", "Alice", 2.5m), ("Alice", "Dave", 1.5m)
    };
        foreach (var (from, to, sum) in honest)
        {
            var r = center.Submit(Tx(from, to, sum));
            Console.WriteLine($"{from} -> {to}: {sum}   {(r.IsValid ? "✔" : "✘")} {r.Reason}");
        }

        //Підробки: зміна суми, зміна отримувача, відсутній підпис
        Console.WriteLine("\n--- Підроблені ---");
        var changedAmount = Tx("Alice", "Bob", 5);
        changedAmount.Amount = 500;                                  //змінили суму після підпису

        var changedTo = Tx("Bob", "Charlie", 2);
        changedTo.To = GetWallet("Eve").Address;                     //змінили отримувача після підпису

        var noSignature = Tx("Charlie", "Dave", 1);
        noSignature.Signature = null;                                //прибрали підпис

        var fakes = new[]
        {
        ("Змінена сума", changedAmount),
        ("Змінений отримувач", changedTo),
        ("Без підпису", noSignature)
    };
        foreach (var (name, tx) in fakes)
        {
            var r = center.Submit(tx);
            Console.WriteLine($"{name,-20} {(r.IsValid ? "✔" : "✘")} {r.Reason}");
        }

        //Чи не потрапила жодна підробка в mempool або Blockchain
        var leaked = fakes.Count(f =>
            blockChain.ContainsTransaction(f.Item2.Id) ||
            blockChain.PendingTransactions.Any(p => p.Id == f.Item2.Id));
        Console.WriteLine(leaked == 0
            ? "\n✔ Жодна некоректна транзакція не потрапила в mempool чи Blockchain"
            : $"\n✘ Потрапило некоректних: {leaked}");

        Console.WriteLine();
        displayService.PrintVerificationReport(center);
    }
    else if (enter == "verify")
    {
        Console.Write("Id транзакції: ");
        displayService.PrintVerificationResult(center.Verify(Console.ReadLine() ?? ""));
    }
    else if (enter == "vcList")
    {
        //Список перевірених — звідси зручно копіювати Id
        foreach (var r in center.Log)
        {
            Console.WriteLine($"{r.TransactionId}  {r.Status,-7}  {r.Reason}");
        }
    }
    else if (enter == "vcReport")
    {
        displayService.PrintVerificationReport(center);
    }
    else if (enter == "forgeryTest")
    {
        //Рівень 3: Bob намагається підписати транзакцію від імені Alice
        var alice = GetWallet("Alice");
        var bob = GetWallet("Bob");

        //Спроба 1: адреса Alice як відправник, публічний ключ і підпис — Bob
        var forged1 = new Transaction(alice.Address, bob.Address, 30);
        forged1.SenderPublicKey = bob.PublicKey;
        forged1.Signature = bob.Sign(forged1.GetDataToSign());

        //Спроба 2: адреса І публічний ключ Alice, але підписує Bob своїм ключем
        var forged2 = new Transaction(alice.Address, bob.Address, 30);
        forged2.SenderPublicKey = alice.PublicKey;
        forged2.Signature = bob.Sign(forged2.GetDataToSign());

        //Справжня транзакція Alice
        var genuine = Tx("Alice", "Bob", 30);

        var cases = new[]
        {
        ("Bob: адреса Alice + свій ключ", forged1),
        ("Bob: ключ Alice + свій підпис", forged2),
        ("Справжня транзакція Alice", genuine)
    };

        Console.WriteLine("1) Наївна перевірка — тільки підпис (як без зв'язку адреса↔ключ):");
        foreach (var (name, tx) in cases)
        {
            Console.WriteLine($"   {name,-32} {(center.NaiveSignatureCheck(tx) ? "✔ прийнято" : "✘ відхилено")}");
        }

        Console.WriteLine("\n2) Повна перевірка TransactionService (адреса = ключ, підпис = дані):");
        foreach (var (name, tx) in cases)
        {
            var r = center.Submit(tx);
            Console.WriteLine($"   {name,-32} {(r.IsValid ? "✔ Valid" : "✘ Invalid")}  {r.Reason}");
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