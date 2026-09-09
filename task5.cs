using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lab5_StringsAndCollections
{
	// ================================================================
	// Задание 5. Строки и коллекции
	//  1) Словарь «ошибочных слов» (опечатка -> правильное слово).
	//  2) Исправление опечаток в текстовых файлах указанной папки.
	//  3) Замена номеров телефонов (012) 345-67-89 -> +380 12 345 67 89
	//     регулярными выражениями в тех же файлах.
	// ================================================================

	// ----------------------------------------------------------------
	// 1. СЛОВАРЬ ОШИБОЧНЫХ СЛОВ
	// Главная коллекция — Dictionary<string, string> (опечатка -> норма).
	// Ключи сравниваются без учёта регистра. Группы удобно объявлять
	// методом AddGroup("правильное", "опечатка1", "опечатка2", ...).
	// ----------------------------------------------------------------
	class TypoDictionary
	{
		private readonly Dictionary<string, string> _map =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public TypoDictionary()
		{
			LoadDefaults();
		}

		/// <summary>Все пары «опечатка -> правильное слово» (только чтение).</summary>
		public IReadOnlyDictionary<string, string> Corrections => _map;

		/// <summary>Регистрирует группу: правильное слово и его типичные опечатки.</summary>
		public void AddGroup(string correct, params string[] typos)
		{
			if (string.IsNullOrWhiteSpace(correct)) return;
			foreach (string typo in typos)
			{
				if (string.IsNullOrWhiteSpace(typo)) continue;
				_map[typo.Trim().ToLowerInvariant()] = correct.Trim().ToLowerInvariant();
			}
		}

		private void LoadDefaults()
		{
			AddGroup("привет", "првиет", "пирвет", "привт");
			AddGroup("здравствуйте", "здраствуйте", "здравствуйти");
			AddGroup("спасибо", "спосибо", "спасиба");
			AddGroup("пожалуйста", "пожалуста", "пажалуйста", "пожалста");
			AddGroup("человек", "человекк", "чиловек");
			AddGroup("компьютер", "компютер", "компьутер");
			AddGroup("работа", "робота", "рабата");
			AddGroup("телефон", "тилефон", "телефн");
			AddGroup("сейчас", "сичас", "щас");
			AddGroup("корректно", "каректно", "коректно");
		}
	}

	// ----------------------------------------------------------------
	// 2. ИСПРАВЛЕНИЕ ОПЕЧАТОК В ТЕКСТЕ
	// Для каждого слова из словаря заранее строится регулярное
	// выражение \b<слово>\b — заменяется ЦЕЛОЕ слово, а не часть
	// другого. Регистр найденного слова сохраняется:
	// «Првиет» -> «Привет», «ПРВИЕТ» -> «ПРИВЕТ», «првиет» -> «привет».
	// ----------------------------------------------------------------
	class SpellFixer
	{
		private readonly List<KeyValuePair<Regex, string>> _rules =
			new List<KeyValuePair<Regex, string>>();

		public SpellFixer(TypoDictionary dictionary)
		{
			foreach (var pair in dictionary.Corrections)
			{
				string pattern = @"\b" + Regex.Escape(pair.Key) + @"\b";
				_rules.Add(new KeyValuePair<Regex, string>(
					new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
					pair.Value));
			}
		}

		/// <summary>Исправляет опечатки в text (передаётся по ссылке), возвращает число замен.</summary>
		public int Fix(ref string text)
		{
			int total = 0;
			foreach (var rule in _rules)
			{
				int count = rule.Key.Matches(text).Count;
				if (count == 0) continue;
				total += count;
				text = rule.Key.Replace(text, m => PreserveCase(m, rule.Value));
			}
			return total;
		}

		private static string PreserveCase(Match m, string replacement)
		{
			string word = m.Value;
			if (replacement.Length == 0) return replacement;

			// Слово полностью в верхнем регистре -> «ПРИВЕТ».
			bool allUpper = true;
			foreach (char c in word)
				if (char.IsLetter(c) && !char.IsUpper(c)) { allUpper = false; break; }
			if (allUpper) return replacement.ToUpperInvariant();

			// Первая буква заглавная -> «Привет».
			if (char.IsUpper(word[0]))
				return char.ToUpperInvariant(replacement[0]) + replacement.Substring(1);

			// Иначе — как в словаре: «привет».
			return replacement;
		}
	}

	// ----------------------------------------------------------------
	// 3. ЗАМЕНА НОМЕРОВ ТЕЛЕФОНОВ (регулярные выражения)
	// (012) 345-67-89 -> +380 12 345 67 89
	// Обобщение для мобильных кодов: (0XX) XXX-XX-XX -> +380 XX XXX XX XX.
	// Ведущий ноль кода «съедается» шаблоном и не попадает в группы.
	// ----------------------------------------------------------------
	static class PhoneNumberFormatter
	{
		public const string Pattern = @"\(0(\d{2})\)\s*(\d{3})-(\d{2})-(\d{2})";
		private const string Replacement = "+380 $1 $2 $3 $4";

		/// <summary>Заменяет телефоны в text (по ссылке), возвращает число замен.</summary>
		public static int Replace(ref string text)
		{
			int count = Regex.Matches(text, Pattern).Count;
			if (count > 0)
				text = Regex.Replace(text, Pattern, Replacement);
			return count;
		}
	}

	// ----------------------------------------------------------------
	// 4. ОБРАБОТКА ТЕКСТОВЫХ ФАЙЛОВ В ДИРЕКТОРИИ
	// Читает каждый *.txt (UTF-8), применяет выбранные исправления
	// и сохраняет файл обратно, ведя статистику.
	// ----------------------------------------------------------------
	class DirectoryTextProcessor
	{
		private readonly SpellFixer _spellFixer;

		public DirectoryTextProcessor(SpellFixer spellFixer)
		{
			_spellFixer = spellFixer;
		}

		public bool ProcessFile(string path, bool fixWords, bool fixPhones,
			out int wordFixes, out int phoneFixes, out string resultText)
		{
			wordFixes = 0;
			phoneFixes = 0;
			string text = File.ReadAllText(path, Encoding.UTF8);
			string original = text;

			if (fixWords) wordFixes = _spellFixer.Fix(ref text);
			if (fixPhones) phoneFixes = PhoneNumberFormatter.Replace(ref text);

			bool changed = text != original;
			if (changed)
				File.WriteAllText(path, text, new UTF8Encoding(false));

			resultText = text;
			return changed;
		}

		public void ProcessDirectory(string dir, bool fixWords, bool fixPhones)
		{
			string[] files = Directory.GetFiles(dir, "*.txt", SearchOption.TopDirectoryOnly);
			if (files.Length == 0)
			{
				Console.WriteLine("  В папке не найдено файлов *.txt.");
				return;
			}

			if (fixPhones)
			{
				Console.WriteLine($"  Шаблон телефонов: {PhoneNumberFormatter.Pattern}");
				Console.WriteLine("  Пример: (012) 345-67-89 -> +380 12 345 67 89");
			}

			int filesChanged = 0, totalWords = 0, totalPhones = 0;
			Console.WriteLine();
			foreach (string file in files)
			{
				string name = Path.GetFileName(file);
				bool changed = ProcessFile(file, fixWords, fixPhones,
					out int wf, out int pf, out string result);

				Console.WriteLine($"  [{name}]  слов исправлено: {wf}, телефонов заменено: {pf}" +
								  (changed ? "" : "  (изменений нет)"));
				if (changed) filesChanged++;
				totalWords += wf;
				totalPhones += pf;

				if (changed && result.Length <= 700)
				{
					Console.WriteLine("    --- содержимое после обработки ---");
					foreach (string line in result.Split('\n'))
						Console.WriteLine("    " + line.TrimEnd('\r'));
					Console.WriteLine("    ----------------------------------");
				}
			}

			Console.WriteLine();
			Console.WriteLine($"Итого: изменено файлов {filesChanged} из {files.Length}; " +
							  $"исправлено слов: {totalWords}; заменено телефонов: {totalPhones}.");
		}
	}

	// ----------------------------------------------------------------
	// 5. СОЗДАНИЕ ДЕМО-ФАЙЛОВ (чтобы было что обрабатывать)
	// Каждый запуск пункта 2 перезаписывает файлы «грязными» версиями,
	// так что эксперимент можно повторять сколько угодно раз.
	// ----------------------------------------------------------------
	static class DemoFiles
	{
		public static void Create(string dir)
		{
			Directory.CreateDirectory(dir);

			var files = new Dictionary<string, string>
			{
				["письмо.txt"] =
					"Првиет, дорогой друг!\n" +
					"Я получил твоё письмо и очень обрадовался.\n" +
					"Мой телефон: (095) 123-45-67 — звони в любое время.\n" +
					"Спосибо за помощь! Пажалуйста, ответь скорее.\n" +
					"Завтра куплю новый компютер, а пока пишу с телефона.\n",

				["контакты.txt"] =
					"Список контактов и заметок:\n" +
					"Тел: (012) 345-67-89 — главный номер.\n" +
					"Доп: (050) 111-22-33.\n" +
					"Здраствуйте! Это тестовый файл.\n" +
					"Пирвет всем! Робота завтра, сичас перезвоню.\n"
			};

			foreach (var f in files)
			{
				string path = Path.Combine(dir, f.Key);
				File.WriteAllText(path, f.Value, new UTF8Encoding(false));
				Console.WriteLine($"\nФайл: {f.Key}  ({Path.GetFullPath(path)})");
				Console.WriteLine("--- исходное содержимое ---");
				Console.Write(f.Value);
				Console.WriteLine("----------------------------");
			}
		}
	}

	// ----------------------------------------------------------------
	// 6. ГЛАВНАЯ ПРОГРАММА (меню)
	// ----------------------------------------------------------------
	class Program
	{
		private const string DefaultDir = "TestFiles";

		static void Main()
		{
			Console.OutputEncoding = Encoding.UTF8;

			var dictionary = new TypoDictionary();
			var fixer = new SpellFixer(dictionary);
			var processor = new DirectoryTextProcessor(fixer);

			while (true)
			{
				Console.WriteLine();
				Console.WriteLine("=== Лабораторная 5: Строки и коллекции ===");
				Console.WriteLine("1. Показать словарь ошибочных слов");
				Console.WriteLine("2. Создать демо-файлы в папке (с опечатками и телефонами)");
				Console.WriteLine("3. Исправить опечатки (словарь) в файлах папки");
				Console.WriteLine("4. Заменить телефоны (рег. выражения) в файлах папки");
				Console.WriteLine("5. Полная обработка файлов (слова + телефоны)");
				Console.WriteLine("6. Проверить регулярное выражение телефона на строке");
				Console.WriteLine("0. Выход");
				Console.Write("Ваш выбор: ");

				switch (ReadInt(0, 6))
				{
					case 1: ShowDictionary(dictionary); break;
					case 2: DemoFiles.Create(DefaultDir); break;
					case 3: ProcessDir(processor, fixWords: true, fixPhones: false); break;
					case 4: ProcessDir(processor, fixWords: false, fixPhones: true); break;
					case 5: ProcessDir(processor, fixWords: true, fixPhones: true); break;
					case 6: TestRegexOnString(); break;
					case 0: return;
				}
			}
		}

		static void ShowDictionary(TypoDictionary dictionary)
		{
			Console.WriteLine("\nСловарь «ошибочных слов» (опечатка -> правильное слово):");
			var groups = dictionary.Corrections
				.GroupBy(kv => kv.Value)
				.OrderBy(g => g.Key);

			foreach (var group in groups)
				Console.WriteLine($"  {group.Key,-15} <- {string.Join(", ", group.Select(kv => kv.Key))}");
		}

		static void ProcessDir(DirectoryTextProcessor processor, bool fixWords, bool fixPhones)
		{
			string dir = ReadDirectory();
			processor.ProcessDirectory(dir, fixWords, fixPhones);
		}

		static void TestRegexOnString()
		{
			Console.WriteLine($"\nШаблон: {PhoneNumberFormatter.Pattern}");
			Console.WriteLine("Заменяет (0XX) XXX-XX-XX на +380 XX XXX XX XX, например:");
			Console.WriteLine("  (012) 345-67-89  ->  +380 12 345 67 89");
			Console.Write("Введите строку с номером (Enter — пропустить): ");
			string line = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(line)) return;

			int count = PhoneNumberFormatter.Replace(ref line);
			Console.WriteLine($"Замен: {count}");
			Console.WriteLine("Результат: " + line);
		}

		static string ReadDirectory()
		{
			while (true)
			{
				Console.Write($"Путь к папке (Enter — «{DefaultDir}»): ");
				string dir = Console.ReadLine()?.Trim();
				if (string.IsNullOrEmpty(dir)) dir = DefaultDir;
				if (Directory.Exists(dir)) return dir;

				Console.WriteLine($"  Папка «{dir}» не найдена. " +
								  "Создайте её пунктом 2 или укажите существующий путь.");
			}
		}

		static int ReadInt(int min, int max)
		{
			while (true)
			{
				string line = Console.ReadLine();
				if (int.TryParse(line, out int value) && value >= min && value <= max)
					return value;
				Console.Write($"  Некорректный ввод. Введите число от {min} до {max}: ");
			}
		}
	}
}
