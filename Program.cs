using System;
using System.Collections.Generic;
using System.Linq;

namespace Autoservice
{
    internal class Program
    {
        static void Main(string[] args)
        {
            PartFactory partFactory = new PartFactory();
            StorageFactory storageFactory = new StorageFactory(partFactory);
            CarFactory carFactory = new CarFactory(partFactory);
            AutoserviceFactory autoserviceFactory = new AutoserviceFactory(storageFactory, carFactory);

            int money = 1000;
            int storageSize = 25;
            int clientsCount = 5;

            Autoservice autoservice = autoserviceFactory.Create(money, storageSize, clientsCount);

            autoservice.Execute();
        }
    }

    class AutoserviceFactory
    {
        private StorageFactory _storageFactory;
        private CarFactory _carFactory;

        public AutoserviceFactory(StorageFactory storageFactory, CarFactory carFactory)
        {
            _storageFactory = storageFactory;
            _carFactory = carFactory;
        }

        public Autoservice Create(int money, int storageSize, int clientsCount)
        {
            Queue<Car> clients = new Queue<Car>(clientsCount);

            for (int i = 0; i < clients.Count; i++)
            {
                clients.Enqueue(_carFactory.Create());
            }

            return new Autoservice(money, _storageFactory.Create(storageSize), clients);
        }
    }

    class Autoservice
    {
        private const int FixedPenalty = 100;
        private const int MaxPartsCountInCar = 5;
        private const int CommandExit = 0;

        private int _money;
        private Storage _storage;
        private Queue<Car> _carsQueue;
        private Car _currentCar;
        private int _maxBrokenParts;

        public Autoservice(int money, Storage storage, Queue<Car> carsQueue)
        {
            _money = money;
            _storage = storage;
            _carsQueue = carsQueue;

        }

        public void Execute()
        {
            while (_carsQueue.Count > 0)
            {
                bool tryRepare = true;

                ShowInfo();

                _currentCar = _carsQueue.Dequeue();
                _maxBrokenParts = _currentCar.GetBrokenParts.Count;

                while (_currentCar.GetBrokenParts.Count > 0 && tryRepare)
                {
                    _currentCar.ShowParts();

                    Console.WriteLine($"\nТекущий штраф за отказ ={PenaltyCalculate(_maxBrokenParts)}" +
                        $"\nЧтобы начать поиск детали на складе, введите её номер." +
                        $"\nЧтобы отказаться от ремонта введите цифру{CommandExit}");

                    int userInput = GetIntegerPositiveUserInput();

                    if (userInput != 0 && _storage.TryGetPart(_currentCar.GetBrokenParts[userInput - 1].Name, out Part part))
                    {
                        int latestBrokenPartsCount = _currentCar.GetBrokenParts.Count;
                        _currentCar.SetNewPart(userInput - 1, part);

                        if (IsRepare(latestBrokenPartsCount))
                        {
                            _money += part.Price;
                        }
                    }
                    else
                    {
                        tryRepare = false;
                        _money -= PenaltyCalculate(_maxBrokenParts);
                    }
                }
            }
        }

        private bool IsRepare(int previousBrokenPartsCount)
        {
            return _currentCar.GetBrokenParts.Count < previousBrokenPartsCount;
        }

        private int GetIntegerPositiveUserInput()
        {
            int userInput;

            do
            {
                Console.Write("Ваш выбор: ");
            }
            while (int.TryParse(Console.ReadLine(), out userInput) == false || userInput < 0 || userInput > MaxPartsCountInCar);

            return userInput;
        }

        private int PenaltyCalculate(int maxBrokenParts)
        {
            int finalPenalty = FixedPenalty;

            if (_currentCar.GetBrokenParts.Count % maxBrokenParts != 0)
            {
                finalPenalty = FixedPenalty * (_currentCar.GetBrokenParts.Count % maxBrokenParts);
            }

            return finalPenalty;
        }

        private void ShowInfo()
        {
            Console.WriteLine($"Денег на счету: {_money}\n" +
                $"Клиентов в очереди: {_carsQueue.Count}\n");
        }
    }

    class StorageFactory
    {
        private PartFactory _partFactory;

        public StorageFactory(PartFactory partFactory)
        {
            _partFactory = partFactory;
        }

        public Storage Create(int partsCount)
        {
            List<Part> parts = new List<Part>();

            for (int i = 0; i < partsCount; i++)
            {
                parts.Add(_partFactory.CreateRandomIntactPart());
            }

            return new Storage(parts);
        }
    }

    class Storage
    {
        private List<Part> _parts;

        public Storage(List<Part> parts)
        {
            _parts = parts;
        }

        public bool TryGetPart(string requiredName, out Part newPart)
        {
            bool isFind = false;
            newPart = null;

            if (_parts.Count > 0)
            {
                foreach (Part part in _parts)
                {
                    if (part.Name == requiredName)
                    {
                        newPart = part;
                        isFind = true;
                    }
                }
            }

            return isFind;
        }
    }

    class CarFactory
    {
        private PartFactory _partFactory;

        public CarFactory(PartFactory partFactory)
        {
            _partFactory = partFactory;
        }

        public Car Create()
        {
            int basicBrokenPartChance = 100;
            List<string> possibleParts = _partFactory.GetPartsNames;
            List<Part> parts = new List<Part>(possibleParts.Count);

            for (int i = 0; i < parts.Count; i++)
            {
                int currentBrokenPartChance = basicBrokenPartChance / (i + 1);

                parts.Add(_partFactory.CreatePart(possibleParts[i], currentBrokenPartChance > UserUtills.GenerateLimitedPositiveNumber(basicBrokenPartChance)));
            }

            return new Car(parts);
        }
    }

    class Car
    {
        private List<Part> _parts;

        public Car(List<Part> parts)
        {
            _parts = parts;
        }

        public List<Part> GetBrokenParts => _parts.Where(part => part.IsIntact == false).ToList();

        public void SetNewPart(int index, Part part)
        {
            _parts[index] = part;
        }

        public void ShowParts()
        {
            int count = 1;

            Console.WriteLine("\nЗаказ-наряд:\n");

            foreach (Part part in _parts)
            {
                Console.WriteLine($"{count} Деталь - {part.Name}. Цела ли - {part.IsIntact}");
                count++;
            }
        }

    }

    class PartFactory
    {
        private List<string> _partsNames = new List<string>()
        {
            "Двигатель", "Колесо", "Коробка Передач", "Стёкла", "Кузов"
        };

        public List<string> GetPartsNames => _partsNames.ToList();

        public Part CreatePart(string name, bool isIntact)
        {
            return new Part(name, GeneratePrice(), isIntact);
        }

        public Part CreateRandomIntactPart()
        {
            bool isIntact = true;

            return new Part(_partsNames[UserUtills.GenerateLimitedPositiveNumber(_partsNames.Count)], GeneratePrice(), isIntact);
        }

        private int GeneratePrice()
        {
            int min = 50;
            int max = 150;

            return UserUtills.GenerateLimitedNumber(min, max);
        }
    }

    class Part
    {
        public Part(string name, int price, bool isIntact)
        {
            Name = name;
            Price = price;
            IsIntact = isIntact;
        }

        public string Name { get; private set; }
        public int Price { get; private set; }
        public bool IsIntact { get; private set; }
    }

    class UserUtills
    {
        private static Random s_random = new Random();

        public static int GenerateLimitedPositiveNumber(int maxValueExclusive)
        {
            return s_random.Next(maxValueExclusive);
        }

        public static int GenerateLimitedNumber(int minValueInclusive, int maxValueExclusive)
        {
            return s_random.Next(minValueInclusive, maxValueExclusive);
        }
    }
}
