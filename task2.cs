using System;
using System.Collections.Generic;
using System.Globalization;

namespace ZooApp
{
    // ============================================================
    // БАЗОВЫЙ КЛАСС Animal
    // Содержит общие характеристики всех животных.
    // abstract: нельзя создать экземпляр Animal напрямую,
    // только через наследников.
    // ============================================================
    abstract class Animal
    {
        // Инкапсуляция: свойства только для чтения,
        // значения задаются один раз в конструкторе.
        public string Name { get; }        // кличка
        public int Age { get; }            // возраст
        public string Habitat { get; }     // среда обитания: «лес», «водоём», «пустыня»...
        public string Diet { get; }        // тип питания: «хищник», «травоядное»...
        public double Weight { get; }      // вес (дополнительное поле)
        public string Color { get; }       // окрас (дополнительное поле)

        protected Animal(string name, int age, string habitat, string diet,
                         double weight = 0, string color = "не указан")
        {
            Name = name;
            Age = age;
            Habitat = habitat;
            Diet = diet;
            Weight = weight;
            Color = color;
        }

        // Абстрактное свойство: каждый подтип обязан вернуть своё название
        // (это и есть полиморфизм — общий контракт, разная реализация).
        public abstract string TypeName { get; }

        // Общая часть описания. virtual — наследники могут переопределить,
        // но могут и оставить как есть (см. override в классах ниже).
        public virtual string GetInfo()
        {
            return $"Кличка: {Name}, Возраст: {Age}, Среда: {Habitat}, Питание: {Diet}, " +
                   $"Тип: {TypeName}, Вес: {Weight:0.#} кг, Окрас: {Color}";
        }
    }

    // ============================================================
    // НАСЛЕДНИКИ: каждый добавляет своё уникальное свойство
    // и расширяет GetInfo() через base.GetInfo() + уникальная часть.
    // ============================================================

    // Млекопитающее: наличие шерсти
    class Mammal : Animal
    {
        public bool HasFur { get; }

        public Mammal(string name, int age, string habitat, string diet, bool hasFur,
                      double weight = 0, string color = "не указан")
            : base(name, age, habitat, diet, weight, color)
        {
            HasFur = hasFur;
        }

        public override string TypeName => "Млекопитающее";

        public override string GetInfo() =>
            base.GetInfo() + $", Шерсть: {(HasFur ? "есть" : "нет")}";
    }

    // Птица: размах крыльев (метры, дробное число)
    class Bird : Animal
    {
        public double WingSpan { get; }

        public Bird(string name, int age, string habitat, string diet, double wingSpan,
                    double weight = 0, string color = "не указан")
            : base(name, age, habitat, diet, weight, color)
        {
            WingSpan = wingSpan;
        }

        public override string TypeName => "Птица";

        public override string GetInfo() =>
            base.GetInfo() + $", Размах крыльев: {WingSpan:0.##} м";
    }

    // Рыба: тип воды (пресная/морская)
    class Fish : Animal
    {
        public string WaterType { get; }

        public Fish(string name, int age, string habitat, string diet, string waterType,
                    double weight = 0, string color = "не указан")
            : base(name, age, habitat, diet, weight, color)
        {
            WaterType = waterType;
        }

        public override string TypeName => "Рыба";

        public override string GetInfo() =>
            base.GetInfo() + $", Тип воды: {WaterType}";
    }

    // Пресмыкающееся: ядовитость
    class Reptile : Animal
    {
        public bool IsVenomous { get; }

        public Reptile(string name, int age, string habitat, string diet, bool isVenomous,
                       double weight = 0, string color = "не указан")
            : base(name, age, habitat, diet, weight, color)
        {
            IsVenomous = isVenomous;
        }

        public override string TypeName => "Пресмыкающееся";

        public override string GetInfo() =>
            base.GetInfo() + $", Ядовитое: {(IsVenomous ? "да" : "нет")}";
    }

    // Земноводное: влажность кожи (строка или число — взяли строку)
    class Amphibian : Animal
    {
        public string SkinMoisture { get; }

        public Amphibian(string name, int age, string habitat, string diet, string skinMoisture,
                         double weight = 0, string color = "не указан")
            : base(name, age, habitat, diet, weight, color)
        {
            SkinMoisture = skinMoisture;
        }

        public override string TypeName => "Земноводное";

        public override string GetInfo() =>
            base.GetInfo() + $", Влажность кожи: {SkinMoisture}";
    }

    // ============================================================
    // SINGLETON AnimalManager
    // Гарантирует, что экземпляр менеджера ровно один на всю программу.
    // 1) приватный конструктор — снаружи new AnimalManager() невозможен;
    // 2) статическое свойство Instance — единственная точка доступа;
    // 3) ленивая инициализация + lock — потокобезопасно (бонус).
    // ============================================================
    class AnimalManager
    {
        private static AnimalManager _instance;
        private static readonly object SyncRoot = new object();

        private readonly List<Animal> _animals = new List<Animal>();

        private AnimalManager() { } // ключевой момент Singleton

        public static AnimalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                            _instance = new AnimalManager();
                    }
                }
                return _instance;
            }
        }

        // --- Добавление животного ---
        public void Add(Animal animal)
        {
            _animals.Add(animal);
            Console.WriteLine($"Животное «{animal.Name}» ({animal.TypeName}) успешно добавлено.\n");
        }

        // --- Вывод всех ---
        public void ShowAll()
        {
            if (_animals.Count == 0)
            {
                Console.WriteLine("В зоопарке пока никого нет.\n");
                return;
            }

            Console.WriteLine("Список животных в зоопарке:");
            for (int i = 0; i < _animals.Count; i++)
                Console.WriteLine($"[{i}] {_animals[i].GetInfo()}");
            Console.WriteLine();
        }

        // --- Поиск по имени ---
        public void ShowByName(string name)
        {
            bool found = false;
            foreach (Animal a in _animals)
            {
                if (a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(a.GetInfo());
                    found = true;
                }
            }
            Console.WriteLine(found
                ? ""
                : $"Животное с кличкой «{name}» не найдено.\n");
        }

        // --- Показ по индексу ---
        public void ShowByIndex(int index)
        {
            if (index < 0 || index >= _animals.Count)
            {
                Console.WriteLine($"Животного с индексом {index} нет (всего животных: {_animals.Count}).\n");
                return;
            }
            Console.WriteLine(_animals[index].GetInfo() + "\n");
        }

        // ============================================================
        // МЕНЮ: главный цикл взаимодействия с пользователем
        // ============================================================
        public void RunMenu()
        {
            while (true)
            {
                Console.WriteLine("============= ЗООПАРК =============");
                Console.WriteLine("1. Показать всех животных");
                Console.WriteLine("2. Найти животное по имени");
                Console.WriteLine("3. Показать животное по индексу");
                Console.WriteLine("4. Добавить новое животное");
                Console.WriteLine("5. Выход");
                Console.Write("Выберите пункт меню: ");

                // Некорректный ввод (не число) обрабатываем сразу:
                if (!int.TryParse(Console.ReadLine(), out int choice))
                {
                    Console.WriteLine("Ошибка: нужно ввести число от 1 до 5.\n");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        ShowAll();
                        break;

                    case 2:
                        Console.Write("Введите кличку: ");
                        ShowByName(Console.ReadLine());
                        break;

                    case 3:
                        ShowByIndex(ReadInt("Введите индекс животного: "));
                        break;

                    case 4:
                        AddAnimalByUser();
                        break;

                    case 5:
                        Console.WriteLine("До свидания!");
                        return;

                    default:
                        Console.WriteLine("Ошибка: пункта с номером " + choice + " не существует.\n");
                        break;
                }

                Console.WriteLine("Нажмите Enter, чтобы продолжить...");
                Console.ReadLine();
            }
        }

        // --- Добавление животного пользователем через консоль ---
        private void AddAnimalByUser()
        {
            Console.WriteLine("\nВыберите тип животного:");
            Console.WriteLine("1. Млекопитающее");
            Console.WriteLine("2. Птица");
            Console.WriteLine("3. Рыба");
            Console.WriteLine("4. Пресмыкающееся");
            Console.WriteLine("5. Земноводное");
            int type = ReadInt("Ваш выбор (1-5): ", 1, 5);

            // Общие поля для всех типов
            string name = ReadString("Кличка: ");
            int age = ReadInt("Возраст: ", 0, 200);
            string habitat = ReadString("Среда обитания (лес, водоём, пустыня...): ");
            string diet = ReadString("Тип питания (хищник, травоядное, всеядное...): ");

            // Поле, уникальное для выбранного типа
            switch (type)
            {
                case 1:
                    Add(new Mammal(name, age, habitat, diet, ReadBool("Наличие шерсти (да/нет): ")));
                    break;
                case 2:
                    Add(new Bird(name, age, habitat, diet, ReadDouble("Размах крыльев в метрах: ")));
                    break;
                case 3:
                    Add(new Fish(name, age, habitat, diet, ReadString("Тип воды (пресная/морская): ")));
                    break;
                case 4:
                    Add(new Reptile(name, age, habitat, diet, ReadBool("Ядовитое (да/нет): ")));
                    break;
                case 5:
                    Add(new Amphibian(name, age, habitat, diet, ReadString("Влажность кожи (влажная/сухая...): ")));
                    break;
            }
        }

        // ============================================================
        // ХЕЛПЕРЫ ВВОДА с защитой от некорректных значений
        // ============================================================
        private static string ReadString(string prompt)
        {
            Console.Write(prompt);
            string s = Console.ReadLine();
            return string.IsNullOrWhiteSpace(s) ? "безымянное" : s.Trim();
        }

        private static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
        {
            int value;
            do
            {
                Console.Write(prompt);
            } while (!int.TryParse(Console.ReadLine(), out value) || value < min || value > max);
            return value;
        }

        // Дробные числа: заменяем ',' на '.', чтобы работало
        // и при русской локали, где разделитель — запятая.
        private static double ReadDouble(string prompt)
        {
            double value;
            do
            {
                Console.Write(prompt);
            } while (!double.TryParse(
                         Console.ReadLine()?.Replace(',', '.'),
                         NumberStyles.Any,
                         CultureInfo.InvariantCulture,
                         out value) || value < 0);
            return value;
        }

        private static bool ReadBool(string prompt)
        {
            Console.Write(prompt);
            string answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            return answer == "да" || answer == "yes" || answer == "y" || answer == "1" || answer == "true";
        }
    }

    // ============================================================
    // ТОЧКА ВХОДА: демонстрация работы программы
    // ============================================================
    class Program
    {
        static void Main()
        {
            // Чтобы кириллица корректно выводилась в консоли Windows
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Единственная точка доступа к менеджеру — Singleton
            AnimalManager zoo = AnimalManager.Instance;

            // Демонстрация: создаём объекты разных подтипов и добавляем через Singleton
            zoo.Add(new Mammal("Барсик", 5, "лес", "хищник", true, 4.5, "рыжий"));
            zoo.Add(new Bird("Кеша", 2, "город", "всеядное", 0.4, 0.3, "зелёный"));
            zoo.Add(new Fish("Немо", 1, "водоём", "хищник", "морская", 0.2, "оранжевый"));
            zoo.Add(new Reptile("Василиск", 3, "пустыня", "хищник", true, 1.2, "зелёно-бурый"));
            zoo.Add(new Amphibian("Квака", 4, "водоём", "насекомоядное", "влажная", 0.1, "зелёная"));

            // Покажем, что полиморфизм работает:
            // список хранит Animal, но у каждого вызывается СВОЙ GetInfo()
            zoo.ShowAll();

            // Запускаем интерактивное меню
            zoo.RunMenu();
        }
    }
}
