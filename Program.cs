using BlockChainP32;

Console.OutputEncoding = System.Text.Encoding.UTF8;


var blockChain = new BlockChain();
var displayService = new BlockChainDisplayService();

blockChain.AddBlock("Alex send 1 BTC to Bob", "Alex");
blockChain.AddBlock("Bob send 0.5 BTC to Charlie", "Bob");
blockChain.AddBlock("Charlie send 0.2 BTC to Dave", "Charlie");
blockChain.AddBlock("Dave send 0.1 BTC to Eve", "Dave");
blockChain.AddBlock("Eve send 0.05 BTC to Frank", "Eve");



displayService.DisplayChain(blockChain);
Console.WriteLine("Is Valid: " + blockChain.IsChainValid());

blockChain.Chain[2].Data = "Bob send 100 BTC to Charlie";
displayService.DisplayChain(blockChain);
Console.WriteLine("Is Valid: " + blockChain.IsChainValid());


while (true)
{
    Console.WriteLine();
    Console.WriteLine("1 - Пошук блоку за Index");
    Console.WriteLine("2 - Пошук подій за текстом у Data");
    Console.WriteLine("0 - Вихід");
    Console.Write("Ваш вибір: ");
    var choice = Console.ReadLine();

    if (choice == "0")
    {
        break;
    }

    if (choice == "1")
    {
        Console.Write("Введіть Index: ");
        if (!int.TryParse(Console.ReadLine(), out var index))
        {
            Console.WriteLine("Index має бути цілим числом.");
            continue;
        }

        var block = blockChain.FindBlockByIndex(index);
        if (block == null)
        {
            Console.WriteLine($"Блок з Index {index} не знайдено.");
        }
        else
        {
            displayService.DisplayBlock(block);
        }
    }
    else if (choice == "2")
    {
        Console.Write("Введіть текст для пошуку: ");
        var text = Console.ReadLine() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("Текст для пошуку не може бути порожнім.");
            continue;
        }

        var found = blockChain.FindBlocksByData(text);
        if (found.Count == 0)
        {
            Console.WriteLine($"Подій з текстом \"{text}\" не знайдено.");
        }
        else
        {
            Console.WriteLine($"Знайдено блоків: {found.Count}");
            foreach (var block in found)
            {
                displayService.DisplayBlock(block);
            }
        }
    }
    else
    {
        Console.WriteLine("Невідома команда.");
    }
}