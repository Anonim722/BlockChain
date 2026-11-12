using BlockChain_1.Service;
using BlockChain_1;
using BlockChain_1;
using BlockChain_1.Models;
using BlockChain_1.Service;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var blockChain = new BlockChain(4);
var displayService = new BlockChainDisplayService();
var transactionService = new TransactionService();

//Транзакції, що чекають на включення в блок
var listTransaction = new List<Transaction>
{
    transactionService.CreateTransaction("Alice", "Bob", 10),
    transactionService.CreateTransaction("Bob", "Charlie", 5),
    transactionService.CreateTransaction("Charlie", "Dave", 2)
};

while (true)
{
    Console.WriteLine();
    Console.WriteLine($"Menu (в черзі транзакцій: {listTransaction.Count})");
    Console.WriteLine("addTr | add | display | validate | history | find | exit");
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
    else
    {
        Console.WriteLine("Unknown command. Please try again.");
    }
}