// Лабораторная работа 4. Стандартный ввод-вывод (файлы, C#)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Lab4FileIO
{
	// ================= 1. Класс текстового файла =================
	[Serializable]
	public class TextFileDocument
	{
		public string FilePath { get; set; }
		public string Content { get; set; }

		// Конструктор без параметров нужен XmlSerializer'у
		public TextFileDocument() { FilePath = ""; Content = ""; }

		public TextFileDocument(string filePath, string content)
		{
			FilePath = filePath ?? "";
			Content = content ?? "";
		}

		// ---------- обычная работа с файлом ----------
		public void Save() { SaveTo(FilePath); }

		public void SaveTo(string path)
		{
			File.WriteAllText(path, Content, new UTF8Encoding(false));
			FilePath = path;
		}

		public static TextFileDocument Load(string path)
		{
			return new TextFileDocument(path, File.ReadAllText(path));
		}

		public override string ToString()
		{
			return string.Format("Файл: {0}\nСимволов: {1}\n{2}", FilePath, Content.Length, Content);
		}

		// ---------- XML сериализация / десериализация ----------
		public void SaveXml(string path)
		{
			var xs = new XmlSerializer(typeof(TextFileDocument));
			using (var fs = File.Create(path))
			{
				xs.Serialize(fs, this);
			}
			FilePath = path;
		}

		public static TextFileDocument LoadXml(string path)
		{
			var xs = new XmlSerializer(typeof(TextFileDocument));
			using (var fs = File.OpenRead(path))
			{
				return (TextFileDocument)xs.Deserialize(fs);
			}
		}

		// ---------- Бинарная сериализация / десериализация ----------
		// Классический BinaryFormatter объявлен устаревшим (небезопасен)
		// и удалён в современных версиях .NET, поэтому бинарный формат
		// реализован вручную: длина строки + байты строки (UTF-8).
		// Вариант через BinaryFormatter (для .NET Framework):
		//   var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
		//   bf.Serialize(fs, this);                    // запись
		//   doc = (TextFileDocument)bf.Deserialize(fs); // чтение
		public void SaveBinary(string path)
		{
			using (var fs = File.Create(path))
			using (var bw = new BinaryWriter(fs, new UTF8Encoding(false)))
			{
				bw.Write(FilePath ?? "");
				bw.Write(Content ?? "");
			}
			FilePath = path;
		}

		public static TextFileDocument LoadBinary(string path)
		{
			using (var fs = File.OpenRead(path))
			using (var br = new BinaryReader(fs, new UTF8Encoding(false)))
			{
				return new TextFileDocument(br.ReadString(), br.ReadString());
			}
		}
	}

	// ================= Результат поиска по одному файлу =================
	public class FileSearchResult
	{
		public string FilePath { get; set; }

		// слово -> сколько раз встретилось в файле (регистр не важен)
		public Dictionary<string, int> KeywordCounts { get; set; }

		public int TotalMatches
		{
			get
			{
				int sum = 0;
				foreach (var v in KeywordCounts.Values) sum += v;
				return sum;
			}
		}

		public FileSearchResult(string filePath)
		{
			FilePath = filePath;
			KeywordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
	}

	// ================= 2. Поиск текстовых файлов =================
	public class TextFileSearcher
	{
		// Возвращает файлы (по маске pattern в каталоге directory),
		// в которых встретилось хотя бы одно из ключевых слов.
		public List<FileSearchResult> Search(
			string directory,
			IEnumerable<string> keywords,
			bool recursive = true,
			string pattern = "*.txt")
		{
			var result = new List<FileSearchResult>();
			if (!Directory.Exists(directory))
				throw new DirectoryNotFoundException("Каталог не найден: " + directory);

			var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			string[] files;
			try
			{
				files = Directory.GetFiles(directory, pattern, option);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				Console.WriteLine("Не удалось прочитать каталог: " + ex.Message);
				return result;
			}

			foreach (string file in files)
			{
				try
				{
					string text = File.ReadAllText(file);
					var hit = new FileSearchResult(file);
					bool foundAny = false;
					foreach (string kw in keywords)
					{
						int count = CountOccurrences(text, kw);
						if (count > 0) foundAny = true;
						hit.KeywordCounts[kw] = count;
					}
					if (foundAny) result.Add(hit);
				}
				catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
				{
					// файл занят другим процессом или нет прав — пропускаем
				}
			}
			return result;
		}

		// Подсчёт вхождений слова в текст (без учёта регистра)
		public static int CountOccurrences(string text, string keyword)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword)) return 0;
			int count = 0, pos = 0;
			while ((pos = text.IndexOf(keyword, pos, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				count++;
				pos += keyword.Length;
			}
			return count;
		}
	}

	// ================= 4. Индекс директории =================
	public class FileIndex
	{
		// полный список: файл -> слова с количеством вхождений
		private readonly List<FileSearchResult> _entries = new List<FileSearchResult>();

		// инвертированный индекс: ключевое слово -> файлы, где оно есть
		private readonly Dictionary<string, List<string>> _inverted =
			new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		public string RootDirectory { get; private set; }
		public int IndexedFiles { get { return _entries.Count; } }
		public int UniqueKeywords { get { return _inverted.Count; } }

		// перечисление записей и ключевых слов — для отчётов
		public IReadOnlyList<FileSearchResult> Entries { get { return _entries; } }
		public IEnumerable<string> IndexedKeywords { get { return _inverted.Keys; } }

		// Построение индекса по заданному словарю ключевых слов
		public void Build(string directory, IEnumerable<string> keywords,
						  bool recursive = true, string pattern = "*.txt")
		{
			_entries.Clear();
			_inverted.Clear();
			RootDirectory = Path.GetFullPath(directory);

			var searcher = new TextFileSearcher();
			foreach (var r in searcher.Search(directory, keywords, recursive, pattern))
			{
				_entries.Add(r);
				foreach (var kv in r.KeywordCounts)
				{
					if (kv.Value <= 0) continue;
					List<string> list;
					if (!_inverted.TryGetValue(kv.Key, out list))
					{
						list = new List<string>();
						_inverted[kv.Key] = list;
					}
					list.Add(r.FilePath);
				}
			}
		}

		// Запрос: файлы, где есть конкретное слово
		public List<string> FindByKeyword(string keyword)
		{
			List<string> list;
			return _inverted.TryGetValue(keyword, out list)
				? new List<string>(list)
				: new List<string>();
		}

		// Запрос И: файлы, где есть ВСЕ перечисленные слова
		public List<string> FindByAll(IEnumerable<string> keywords)
		{
			List<string> result = null;
			foreach (string kw in keywords)
			{
				List<string> current = FindByKeyword(kw);
				result = result == null
					? current
					: result.Intersect(current, StringComparer.OrdinalIgnoreCase).ToList();
			}
			return result ?? new List<string>();
		}

		// Запрос ИЛИ: файлы, где есть ЛЮБОЕ из перечисленных слов
		public List<string> FindByAny(IEnumerable<string> keywords)
		{
			var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string kw in keywords)
				foreach (string file in FindByKeyword(kw))
					set.Add(file);
			return set.ToList();
		}

		// Детали по конкретному файлу (для отчёта)
		public FileSearchResult GetEntry(string filePath)
		{
			foreach (var e in _entries)
				if (string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
					return e;
			return null;
		}
	}

	// ================= 3. Memento =================
	// Снимок состояния (Memento): неизменяемая копия содержимого
	public class EditorMemento
	{
		public string Content { get; private set; }
		public DateTime CreatedAt { get; private set; }

		public EditorMemento(string content)
		{
			Content = content;
			CreatedAt = DateTime.Now;
		}

		public override string ToString()
		{
			return CreatedAt.ToString("HH:mm:ss");
		}
	}

	// Хранитель (Caretaker): хранит стек снимков, ничего не знает о документе
	public class EditorHistory
	{
		private readonly Stack<EditorMemento> _snapshots = new Stack<EditorMemento>();

		public int Count { get { return _snapshots.Count; } }
		public bool CanUndo { get { return _snapshots.Count > 0; } }

		public void Push(EditorMemento memento) { _snapshots.Push(memento); }
		public EditorMemento Pop() { return _snapshots.Pop(); }
		public void Clear() { _snapshots.Clear(); }
	}

	// Редактор (Originator): хранит документ, создаёт снимки и откатывается
	public class TextFileEditor
	{
		public TextFileDocument Document { get; private set; }
		public EditorHistory History { get; private set; }

		public TextFileEditor()
		{
			History = new EditorHistory();
			Document = new TextFileDocument("", "");
		}

		// ---------- управление документом ----------
		public void NewDocument(string path)
		{
			Document = new TextFileDocument(path, "");
			History.Clear();
		}

		public void Open(string path) { Document = TextFileDocument.Load(path); History.Clear(); }
		public void OpenXml(string path) { Document = TextFileDocument.LoadXml(path); History.Clear(); }
		public void OpenBinary(string path) { Document = TextFileDocument.LoadBinary(path); History.Clear(); }

		public void Save() { Document.Save(); }
		public void SaveAs(string path) { Document.SaveTo(path); }
		public void SaveXmlCopy(string path) { Document.SaveXml(path); }
		public void SaveBinaryCopy(string path) { Document.SaveBinary(path); }

		// Перед каждым изменением сохраняем снимок текущего состояния
		private void MakeSnapshot()
		{
			History.Push(new EditorMemento(Document.Content));
		}

		// ---------- операции редактирования (все с откатом) ----------
		public void Append(string text)
		{
			MakeSnapshot();
			Document.Content += text;
		}

		public void InsertAt(int position, string text)
		{
			MakeSnapshot();
			if (position < 0) position = 0;
			if (position > Document.Content.Length) position = Document.Content.Length;
			Document.Content = Document.Content.Insert(position, text);
		}

		public void RemoveRange(int start, int count)
		{
			MakeSnapshot();
			if (start < 0) start = 0;
			if (start > Document.Content.Length) start = Document.Content.Length;
			if (start + count > Document.Content.Length) count = Document.Content.Length - start;
			if (count < 0) count = 0;
			Document.Content = Document.Content.Remove(start, count);
		}

		// Возвращает число заменённых вхождений
		public int ReplaceAll(string oldText, string newText)
		{
			if (string.IsNullOrEmpty(oldText)) return 0;
			int occurrences = TextFileSearcher.CountOccurrences(Document.Content, oldText);
			if (occurrences == 0) return 0;
			MakeSnapshot();
			Document.Content = Document.Content.Replace(oldText, newText);
			return occurrences;
		}

		// Откат: берём последний снимок и восстанавливаем из него содержимое
		public bool Undo()
		{
			if (!History.CanUndo) return false;
			Document.Content = History.Pop().Content;
			return true;
		}

		public void PrintContent()
		{
			Console.WriteLine("Путь: " + (Document.FilePath == "" ? "(новый файл)" : Document.FilePath));
			Console.WriteLine("Длина: " + Document.Content.Length + " символов; снимков в истории (для Undo): " + History.Count);
			Console.WriteLine("----- содержимое -----");
			Console.WriteLine(Document.Content);
			Console.WriteLine("----------------------");
		}
	}

	// ============================================================
	// Программа: меню и консольные приложения
	// ============================================================
	internal class Program
	{
		static void Main()
		{
			Console.OutputEncoding = Encoding.UTF8;
			try { Console.InputEncoding = Encoding.UTF8; } catch { /* не критично */ }

			Console.WriteLine("Лабораторная работа №4. Стандартный ввод-вывод (файлы, C#).");

			bool exit = false;
			while (!exit)
			{
				Console.WriteLine();
				Console.WriteLine("----- ГЛАВНОЕ МЕНЮ -----");
				Console.WriteLine("1. Демонстрация: сериализация (XML/бинарная) и Memento");
				Console.WriteLine("2. Редактор текстовых файлов (откат изменений, Memento)");
				Console.WriteLine("3. Поиск текстовых файлов по ключевым словам");
				Console.WriteLine("4. Индексация директории по ключевым словам");
				Console.WriteLine("0. Выход");

				int choice = ReadInt("Выберите пункт: ", 0, 4);
				switch (choice)
				{
					case 1: RunSerializationMementoDemo(); break;
					case 2: RunEditorApp(); break;
					case 3: RunSearchApp(); break;
					case 4: RunIndexApp(); break;
					default: exit = true; break;
				}
			}
		}

		// ---------- Демонстрация: сериализация + Memento ----------
		static void RunSerializationMementoDemo()
		{
			Console.WriteLine();
			Console.WriteLine("===== ДЕМОНСТРАЦИЯ: СЕРИАЛИЗАЦИЯ И MEMENTO =====");

			string dir = Path.Combine(Path.GetTempPath(), "Lab4Demo_" + Guid.NewGuid().ToString("N").Substring(0, 8));
			Directory.CreateDirectory(dir);
			string txtPath = Path.Combine(dir, "demo.txt");
			string xmlPath = Path.Combine(dir, "demo.xml");
			string binPath = Path.Combine(dir, "demo.dat");

			try
			{
				// [1] Обычный документ
				Console.WriteLine("\n[1] Создаём документ и сохраняем как обычный текст...");
				var doc = new TextFileDocument(txtPath,
					"Привет, лабораторная работа!\nСтрока вторая: 12345\nСтрока третья: C# файлы, ввод-вывод.");
				doc.Save();
				Console.WriteLine("OK: " + txtPath);

				// [2] XML round-trip
				Console.WriteLine("\n[2] XML-сериализация -> " + xmlPath);
				doc.SaveXml(xmlPath);
				Console.WriteLine("Содержимое XML-файла:");
				Console.WriteLine(File.ReadAllText(xmlPath));
				var docXml = TextFileDocument.LoadXml(xmlPath);
				Console.WriteLine("Прочитано из XML. Содержимое совпадает: " + (docXml.Content == doc.Content));

				// [3] Бинарный round-trip
				Console.WriteLine("\n[3] Бинарная сериализация -> " + binPath);
				doc.SaveBinary(binPath);
				var docBin = TextFileDocument.LoadBinary(binPath);
				Console.WriteLine("Прочитано из бинарного файла. Содержимое совпадает: " + (docBin.Content == doc.Content));

				// [4] Memento: редактор с откатом
				Console.WriteLine("\n[4] Редактор с откатом (Memento)...");
				var editor = new TextFileEditor();
				editor.Open(txtPath);
				Console.WriteLine("Открыли файл. Исходное состояние:");
				editor.PrintContent();

				editor.Append("\nСтрока, добавленная редактором.");
				editor.ReplaceAll("12345", "54321");
				editor.InsertAt(0, ">>> НАЧАЛО ФАЙЛА <<<\n");
				Console.WriteLine("После трёх изменений (добавить, заменить, вставить):");
				editor.PrintContent();

				Console.WriteLine("Откат №1 (убирает вставку в начало):");
				editor.Undo();
				editor.PrintContent();

				Console.WriteLine("Откат №2 (убирает замену 12345 -> 54321):");
				editor.Undo();
				editor.PrintContent();

				Console.WriteLine("Откат №3 (убирает добавленную строку):");
				editor.Undo();
				editor.PrintContent();

				Console.WriteLine("Попытка отката дальше исходного состояния: " +
					(editor.Undo() ? "выполнен" : "нечего отменять — история пуста"));
			}
			finally
			{
				try { Directory.Delete(dir, true); } catch { }
			}
		}

		// ---------- Редактор текстовых файлов ----------
		static void RunEditorApp()
		{
			Console.WriteLine();
			Console.WriteLine("===== РЕДАКТОР ТЕКСТОВЫХ ФАЙЛОВ (Memento) =====");
			var editor = new TextFileEditor();
			bool exit = false;

			while (!exit)
			{
				Console.WriteLine();
				Console.WriteLine("--- Меню редактора ---");
				Console.WriteLine(" 1. Показать содержимое");
				Console.WriteLine(" 2. Добавить текст в конец");
				Console.WriteLine(" 3. Вставить текст по позиции");
				Console.WriteLine(" 4. Удалить фрагмент");
				Console.WriteLine(" 5. Заменить все вхождения");
				Console.WriteLine(" 6. Отменить последнее изменение (Undo / Memento)");
				Console.WriteLine(" 7. Сохранить (обычный текст)");
				Console.WriteLine(" 8. Сохранить XML-копию");
				Console.WriteLine(" 9. Сохранить бинарную копию");
				Console.WriteLine("10. Открыть текстовый файл");
				Console.WriteLine("11. Открыть XML-файл");
				Console.WriteLine("12. Открыть бинарный файл");
				Console.WriteLine(" 0. Выйти из редактора");

				int choice = ReadInt("Ваш выбор: ", 0, 12);
				try
				{
					switch (choice)
					{
						case 1:
							editor.PrintContent();
							break;
						case 2:
							Console.Write("Текст для добавления: ");
							editor.Append(Console.ReadLine() ?? "");
							Console.WriteLine("Добавлено (доступен откат).");
							break;
						case 3:
							int pos = ReadInt("Позиция (индекс символа): ", 0, int.MaxValue);
							Console.Write("Текст для вставки: ");
							editor.InsertAt(pos, Console.ReadLine() ?? "");
							Console.WriteLine("Вставлено (доступен откат).");
							break;
						case 4:
							int start = ReadInt("Начало удаления (индекс): ", 0, int.MaxValue);
							int length = ReadInt("Сколько символов удалить: ", 0, int.MaxValue);
							editor.RemoveRange(start, length);
							Console.WriteLine("Удалено (доступен откат).");
							break;
						case 5:
							Console.Write("Что заменить: ");
							string oldText = Console.ReadLine() ?? "";
							Console.Write("На что заменить: ");
							string newText = Console.ReadLine() ?? "";
							int replaced = editor.ReplaceAll(oldText, newText);
							Console.WriteLine(replaced > 0
								? "Заменено вхождений: " + replaced + " (доступен откат)."
								: "Вхождений не найдено.");
							break;
						case 6:
							Console.WriteLine(editor.Undo()
								? "Последнее изменение отменено."
								: "Откатывать нечего: история пуста.");
							break;
						case 7:
							if (editor.Document.FilePath == "")
							{
								string p = ReadString("Путь для сохранения: ");
								if (p == "") { Console.WriteLine("Сохранение отменено."); break; }
								editor.SaveAs(p);
							}
							else editor.Save();
							Console.WriteLine("Файл сохранён.");
							break;
						case 8:
							string xmlPath = ReadString("Путь XML-копии (Enter = document.xml): ");
							if (xmlPath == "") xmlPath = "document.xml";
							editor.SaveXmlCopy(xmlPath);
							Console.WriteLine("XML-копия сохранена: " + xmlPath);
							break;
						case 9:
							string binPath = ReadString("Путь бинарной копии (Enter = document.dat): ");
							if (binPath == "") binPath = "document.dat";
							editor.SaveBinaryCopy(binPath);
							Console.WriteLine("Бинарная копия сохранена: " + binPath);
							break;
						case 10:
							string txtOpen = ReadString("Путь текстового файла: ");
							editor.Open(txtOpen);
							Console.WriteLine("Файл открыт: " + txtOpen);
							break;
						case 11:
							string xmlOpen = ReadString("Путь XML-файла: ");
							editor.OpenXml(xmlOpen);
							Console.WriteLine("Файл открыт из XML: " + xmlOpen);
							break;
						case 12:
							string binOpen = ReadString("Путь бинарного файла: ");
							editor.OpenBinary(binOpen);
							Console.WriteLine("Файл открыт из бинарного файла: " + binOpen);
							break;
						default:
							exit = true;
							break;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Ошибка: " + ex.Message);
				}
			}
		}

		// ---------- Поиск файлов по ключевым словам ----------
		static void RunSearchApp()
		{
			Console.WriteLine();
			Console.WriteLine("===== ПОИСК ТЕКСТОВЫХ ФАЙЛОВ ПО КЛЮЧЕВЫМ СЛОВАМ =====");

			string dir = ReadString("Каталог (Enter = текущий): ");
			if (dir == "") dir = Directory.GetCurrentDirectory();
			if (!Directory.Exists(dir)) { Console.WriteLine("Каталог не найден: " + dir); return; }

			string pattern = ReadString("Маска файлов (Enter = *.txt): ");
			if (pattern == "") pattern = "*.txt";

			bool recursive = AskYesNo("Искать в подкаталогах");

			string keywordsLine = ReadString("Ключевые слова через запятую: ");
			List<string> keywords = SplitKeywords(keywordsLine);
			if (keywords.Count == 0) { Console.WriteLine("Не задано ни одного ключевого слова."); return; }

			var searcher = new TextFileSearcher();
			var results = searcher.Search(dir, keywords, recursive, pattern);

			Console.WriteLine();
			Console.WriteLine("Найдено файлов с ключевыми словами: " + results.Count);
			foreach (var r in results)
			{
				Console.WriteLine("Файл: " + r.FilePath);
				var parts = new List<string>();
				foreach (var kv in r.KeywordCounts)
					parts.Add(kv.Key + " — " + kv.Value + " раз");
				Console.WriteLine("   " + string.Join(", ", parts));
			}
		}

		// ---------- Индексация директории ----------
		static void RunIndexApp()
		{
			Console.WriteLine();
			Console.WriteLine("===== ИНДЕКСАЦИЯ ДИРЕКТОРИИ ПО КЛЮЧЕВЫМ СЛОВАМ =====");

			string dir = ReadString("Каталог (Enter = текущий): ");
			if (dir == "") dir = Directory.GetCurrentDirectory();
			if (!Directory.Exists(dir)) { Console.WriteLine("Каталог не найден: " + dir); return; }

			string pattern = ReadString("Маска файлов (Enter = *.txt): ");
			if (pattern == "") pattern = "*.txt";

			bool recursive = AskYesNo("Индексировать подкаталоги");

			string keywordsLine = ReadString("Словарь ключевых слов (через запятую): ");
			List<string> keywords = SplitKeywords(keywordsLine);
			if (keywords.Count == 0) { Console.WriteLine("Словарь ключевых слов пуст."); return; }

			Console.WriteLine("Построение индекса...");
			var index = new FileIndex();
			index.Build(dir, keywords, recursive, pattern);
			Console.WriteLine("Индекс построен: файлов с совпадениями — " + index.IndexedFiles +
							  ", слов в словаре — " + index.UniqueKeywords + ".");

			bool exit = false;
			while (!exit)
			{
				Console.WriteLine();
				Console.WriteLine("--- Запросы к индексу ---");
				Console.WriteLine("1. Показать отчёт по индексу");
				Console.WriteLine("2. Файлы по ОДНОМУ слову");
				Console.WriteLine("3. Файлы, содержащие ВСЕ слова (И)");
				Console.WriteLine("4. Файлы, содержащие ЛЮБОЕ из слов (ИЛИ)");
				Console.WriteLine("0. Выйти из индексатора");

				int choice = ReadInt("Выберите: ", 0, 4);
				switch (choice)
				{
					case 1:
						PrintIndexReport(index);
						break;
					case 2:
						{
							string w = ReadString("Слово: ");
							if (w == "") break;
							PrintFileList("По слову «" + w + "» найдено файлов: ", index.FindByKeyword(w), index);
							break;
						}
					case 3:
						{
							List<string> words = SplitKeywords(ReadString("Слова через запятую: "));
							if (words.Count == 0) break;
							PrintFileList("Файлы, содержащие ВСЕ слова: ", index.FindByAll(words), index);
							break;
						}
					case 4:
						{
							List<string> words = SplitKeywords(ReadString("Слова через запятую: "));
							if (words.Count == 0) break;
							PrintFileList("Файлы, содержащие ЛЮБОЕ из слов: ", index.FindByAny(words), index);
							break;
						}
					default:
						exit = true;
						break;
				}
			}
		}

		static void PrintIndexReport(FileIndex index)
		{
			Console.WriteLine();
			Console.WriteLine("Каталог индекса: " + index.RootDirectory);
			Console.WriteLine("Проиндексировано файлов: " + index.IndexedFiles + ", слов: " + index.UniqueKeywords);
			Console.WriteLine();
			foreach (string kw in index.IndexedKeywords)
			{
				List<string> files = index.FindByKeyword(kw);
				Console.WriteLine("Слово «" + kw + "» — файлов: " + files.Count);
				foreach (string f in files)
				{
					int count = 0;
					var entry = index.GetEntry(f);
					if (entry != null) entry.KeywordCounts.TryGetValue(kw, out count);
					Console.WriteLine("    " + f + " (вхождений: " + count + ")");
				}
			}
		}

		static void PrintFileList(string title, List<string> files, FileIndex index)
		{
			Console.WriteLine();
			Console.WriteLine(title + files.Count);
			foreach (string f in files)
			{
				Console.WriteLine("  " + f);
				var entry = index.GetEntry(f);
				if (entry != null)
				{
					var parts = new List<string>();
					foreach (var kv in entry.KeywordCounts)
						if (kv.Value > 0) parts.Add(kv.Key + " (" + kv.Value + ")");
					if (parts.Count > 0) Console.WriteLine("      слова: " + string.Join(", ", parts));
				}
			}
		}

		// ================= Вспомогательный ввод =================
		static string ReadString(string prompt)
		{
			Console.Write(prompt);
			return (Console.ReadLine() ?? "").Trim();
		}

		static int ReadInt(string prompt, int min, int max)
		{
			while (true)
			{
				Console.Write(prompt);
				if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
					return value;
				Console.WriteLine("Ошибка: введите целое число от " + min + " до " + max + ".");
			}
		}

		static bool AskYesNo(string prompt)
		{
			while (true)
			{
				Console.Write(prompt + " (y/n): ");
				string s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
				if (s == "y" || s == "yes" || s == "д" || s == "да") return true;
				if (s == "n" || s == "no" || s == "н" || s == "нет") return false;
				Console.WriteLine("Пожалуйста, ответьте y или n.");
			}
		}

		static List<string> SplitKeywords(string line)
		{
			var list = new List<string>();
			if (string.IsNullOrWhiteSpace(line)) return list;
			foreach (string part in line.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string word = part.Trim();
				if (word.Length > 0 && !list.Contains(word, StringComparer.OrdinalIgnoreCase))
					list.Add(word);
			}
			return list;
		}
	}
}
