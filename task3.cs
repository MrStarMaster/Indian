using System;
using System.Globalization;
using System.Text;

namespace MatrixLab
{
	// ============================================================
	//  Пользовательские исключения (паттерн «иерархия»)
	// ============================================================
	public abstract class MatrixException : Exception
	{
		protected MatrixException(string message) : base(message) { }
	}

	/// <summary>Несовпадение размеров матриц при операции.</summary>
	public sealed class MatrixDimensionException : MatrixException
	{
		public MatrixDimensionException(string message) : base(message) { }
	}

	/// <summary>Матрица вырождена (det = 0) — обратной матрицы не существует.</summary>
	public sealed class MatrixSingularException : MatrixException
	{
		public MatrixSingularException(string message) : base(message) { }
	}

	/// <summary>Обращение к элементу вне границ матрицы.</summary>
	public sealed class MatrixIndexException : MatrixException
	{
		public MatrixIndexException(string message) : base(message) { }
	}

	// ============================================================
	//  Класс «Квадратная матрица»
	// ============================================================
	public class Matrix : ICloneable, IComparable<Matrix>, IComparable, IEquatable<Matrix>
	{
		private const double Eps = 1e-9;          // точность сравнения double
		private static readonly Random Rnd = new Random();

		private readonly double[,] _data;          // элементы матрицы
		private readonly int _n;                   // размер (n x n)

		public int Size => _n;

		// ---------- Конструкторы ----------

		/// <summary>Нулевая матрица размера n x n.</summary>
		public Matrix(int size)
		{
			ValidateSize(size);
			_n = size;
			_data = new double[size, size];
		}

		/// <summary>Матрица из готового двумерного массива (глубокая копия).</summary>
		public Matrix(double[,] source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			int rows = source.GetLength(0);
			int cols = source.GetLength(1);
			if (rows != cols) throw new MatrixDimensionException(
				"Массив не квадратный: " + rows + "x" + cols + ". Требуется матрица n x n.");
			ValidateSize(rows);

			_n = rows;
			_data = (double[,])source.Clone();
		}

		/// <summary>Конструктор копирования (используется для глубокого копирования).</summary>
		public Matrix(Matrix other)
		{
			if (other == null) throw new ArgumentNullException(nameof(other));
			_n = other._n;
			_data = (double[,])other._data.Clone();
		}

		/// <summary>
		/// Конструктор со случайным заполнением.
		/// integers = true  -> целые числа из [min; max];
		/// integers = false -> вещественные числа из (min; max).
		/// </summary>
		public Matrix(int size, double min, double max, bool integers = true)
		{
			ValidateSize(size);
			_n = size;
			_data = new double[size, size];

			for (int r = 0; r < size; r++)
			{
				for (int c = 0; c < size; c++)
				{
					if (integers)
					{
						int lo = (int)Math.Ceiling(min);
						int hi = (int)Math.Floor(max);
						_data[r, c] = Rnd.Next(lo, hi + 1);
					}
					else
					{
						_data[r, c] = min + Rnd.NextDouble() * (max - min);
					}
				}
			}
		}

		// ---------- Фабричные методы (удобные обёртки) ----------

		public static Matrix Random(int size, double min = -9, double max = 9, bool integers = true)
			=> new Matrix(size, min, max, integers);

		public static Matrix Identity(int size)
		{
			var m = new Matrix(size);
			for (int i = 0; i < size; i++) m[i, i] = 1;
			return m;
		}

		// ---------- Индексатор ----------

		public double this[int row, int col]
		{
			get
			{
				CheckIndex(row, col);
				return _data[row, col];
			}
			set
			{
				CheckIndex(row, col);
				_data[row, col] = value;
			}
		}

		private void CheckIndex(int row, int col)
		{
			if (row < 0 || row >= _n || col < 0 || col >= _n)
				throw new MatrixIndexException(
					$"Индекс ({row}, {col}) вне границ матрицы размером {_n}x{_n}.");
		}

		// ---------- Арифметические операторы ----------

		public static Matrix operator +(Matrix a, Matrix b)
		{
			EnsureSameSize(a, b, "+");
			var res = new Matrix(a._n);
			for (int r = 0; r < a._n; r++)
				for (int c = 0; c < a._n; c++)
					res[r, c] = a[r, c] + b[r, c];
			return res;
		}

		public static Matrix operator -(Matrix a, Matrix b)
		{
			EnsureSameSize(a, b, "-");
			var res = new Matrix(a._n);
			for (int r = 0; r < a._n; r++)
				for (int c = 0; c < a._n; c++)
					res[r, c] = a[r, c] - b[r, c];
			return res;
		}

		/// <summary>Умножение матриц (обе квадратные, одного размера).</summary>
		public static Matrix operator *(Matrix a, Matrix b)
		{
			EnsureSameSize(a, b, "*");
			var res = new Matrix(a._n);
			for (int r = 0; r < a._n; r++)
				for (int c = 0; c < a._n; c++)
				{
					double sum = 0;
					for (int k = 0; k < a._n; k++)
						sum += a[r, k] * b[k, c];
					res[r, c] = sum;
				}
			return res;
		}

		public static Matrix operator *(Matrix m, double k)
		{
			var res = new Matrix(m._n);
			for (int r = 0; r < m._n; r++)
				for (int c = 0; c < m._n; c++)
					res[r, c] = m[r, c] * k;
			return res;
		}

		public static Matrix operator *(double k, Matrix m) => m * k;

		// ---------- Операторы сравнения ----------
		// Сравнение «больше/меньше» — ПО ОПРЕДЕЛИТЕЛЮ (единый порядок для
		// >, <, >=, <= и CompareTo). Равенство == / Equals — поэлементное.

		private static int Compare(Matrix a, Matrix b)
		{
			if (ReferenceEquals(a, b)) return 0;
			if (a is null) return -1;
			if (b is null) return 1;
			return a.CompareTo(b);
		}

		public static bool operator >(Matrix a, Matrix b) => Compare(a, b) > 0;
		public static bool operator <(Matrix a, Matrix b) => Compare(a, b) < 0;
		public static bool operator >=(Matrix a, Matrix b) => Compare(a, b) >= 0;
		public static bool operator <=(Matrix a, Matrix b) => Compare(a, b) <= 0;

		public static bool operator ==(Matrix a, Matrix b)
		{
			if (ReferenceEquals(a, b)) return true;
			if (a is null || b is null) return false;
			return a.Equals(b);
		}

		public static bool operator !=(Matrix a, Matrix b) => !(a == b);

		// ---------- Операторы true / false (невырожденность) ----------

		public static bool operator true(Matrix m)
			=> m != null && Math.Abs(m.Determinant()) >= Eps;

		public static bool operator false(Matrix m)
			=> m == null || Math.Abs(m.Determinant()) < Eps;

		// ---------- Операторы приведения типов ----------

		/// <summary>Неявное: double[,] -> Matrix (копия данных).</summary>
		public static implicit operator Matrix(double[,] values) => new Matrix(values);

		/// <summary>Явное: Matrix -> double[,] (копия данных).</summary>
		public static explicit operator double[,](Matrix m)
			=> (double[,])m._data.Clone();

		/// <summary>Явное: Matrix -> double (определитель матрицы).</summary>
		public static explicit operator double(Matrix m)
		{
			if (m is null) throw new ArgumentNullException(nameof(m));
			return m.Determinant();
		}

		// ---------- Определитель (метод Гаусса с выбором главного элемента) ----------

		public double Determinant()
		{
			double[,] a = (double[,])_data.Clone();
			double det = 1.0;

			for (int col = 0; col < _n; col++)
			{
				// ищем ведущий (максимальный по модулю) элемент в столбце
				int pivot = col;
				for (int r = col + 1; r < _n; r++)
					if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col]))
						pivot = r;

				if (Math.Abs(a[pivot, col]) < Eps) return 0; // вырожденная

				if (pivot != col)           // перестановка строк меняет знак
				{
					SwapRows(a, pivot, col);
					det = -det;
				}

				det *= a[col, col];
				for (int r = col + 1; r < _n; r++)
				{
					double factor = a[r, col] / a[col, col];
					for (int c = col; c < _n; c++)
						a[r, c] -= factor * a[col, c];
				}
			}
			return det;
		}

		// ---------- Обратная матрица (метод Гаусса—Жордана) ----------

		public Matrix Inverse()
		{
			if (Math.Abs(Determinant()) < Eps)
				throw new MatrixSingularException(
					$"Матрица вырождена (det = {Determinant():0.####}), обратной матрицы не существует.");

			double[,] a = (double[,])_data.Clone();
			double[,] inv = new double[_n, _n];
			for (int i = 0; i < _n; i++) inv[i, i] = 1.0;

			for (int col = 0; col < _n; col++)
			{
				int pivot = col;
				for (int r = col + 1; r < _n; r++)
					if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col]))
						pivot = r;

				if (Math.Abs(a[pivot, col]) < Eps)
					throw new MatrixSingularException(
						"Матрица вырождена: при обращении возник нулевой ведущий элемент.");

				if (pivot != col)
				{
					SwapRows(a, pivot, col);
					SwapRows(inv, pivot, col);
				}

				double lead = a[col, col];
				for (int c = 0; c < _n; c++)
				{
					a[col, c] /= lead;
					inv[col, c] /= lead;
				}

				// обнуляем столбец во всех остальных строках
				for (int r = 0; r < _n; r++)
				{
					if (r == col) continue;
					double factor = a[r, col];
					if (Math.Abs(factor) < Eps) continue;
					for (int c = 0; c < _n; c++)
					{
						a[r, c] -= factor * a[col, c];
						inv[r, c] -= factor * inv[col, c];
					}
				}
			}

			return new Matrix(inv);
		}

		// ---------- Интерфейсы: Equals / GetHashCode / CompareTo / Clone ----------

		public bool Equals(Matrix other)
		{
			if (other is null) return false;
			if (_n != other._n) return false;
			for (int r = 0; r < _n; r++)
				for (int c = 0; c < _n; c++)
					if (Math.Abs(_data[r, c] - other._data[r, c]) > Eps)
						return false;
			return true;
		}

		public override bool Equals(object obj) => Equals(obj as Matrix);

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + _n.GetHashCode();
				foreach (double d in _data)
				{
					long bits = BitConverter.DoubleToInt64Bits(Math.Round(d, 6));
					hash = hash * 31 + bits.GetHashCode();
				}
				return hash;
			}
		}

		/// <summary>Сравнение по определителю (задаёт порядок для &gt; &lt; &gt;= &lt;=).</summary>
		public int CompareTo(Matrix other)
		{
			if (other is null) return 1;
			double d1 = Determinant();
			double d2 = other.Determinant();
			if (Math.Abs(d1 - d2) < Eps) return 0;
			return d1 < d2 ? -1 : 1;
		}

		public int CompareTo(object obj)
		{
			if (obj is null) return 1;
			if (obj is Matrix m) return CompareTo(m);
			throw new ArgumentException("Объект не является матрицей.", nameof(obj));
		}

		/// <summary>Паттерн «Прототип»: глубокое копирование (копия массива данных).</summary>
		public Matrix Clone() => new Matrix(_data);

		object ICloneable.Clone() => Clone();

		// ---------- ToString ----------

		public override string ToString()
		{
			var sb = new StringBuilder();
			for (int r = 0; r < _n; r++)
			{
				sb.Append("[ ");
				for (int c = 0; c < _n; c++)
					sb.Append(_data[r, c].ToString("0.##", CultureInfo.InvariantCulture).PadLeft(6)).Append(' ');
				sb.Append(']');
				if (r < _n - 1) sb.AppendLine();
			}
			return sb.ToString();
		}

		// ---------- Вспомогательные ----------

		private static void ValidateSize(int size)
		{
			if (size < 1)
				throw new MatrixDimensionException(
					"Размер матрицы должен быть >= 1, получено: " + size + ".");
		}

		private static void EnsureSameSize(Matrix a, Matrix b, string op)
		{
			if (a == null) throw new ArgumentNullException(nameof(a));
			if (b == null) throw new ArgumentNullException(nameof(b));
			if (a._n != b._n)
				throw new MatrixDimensionException(
					$"Операция {op} невозможна: размеры матриц не совпадают ({a._n}x{a._n} и {b._n}x{b._n}).");
		}

		private static void SwapRows(double[,] m, int r1, int r2)
		{
			int cols = m.GetLength(1);
			for (int c = 0; c < cols; c++)
			{
				double tmp = m[r1, c];
				m[r1, c] = m[r2, c];
				m[r2, c] = tmp;
			}
		}
	}

	// ============================================================
	//  Матричный калькулятор (консольное меню)
	// ============================================================
	public static class MatrixCalculator
	{
		private static Matrix _a;
		private static Matrix _b;

		private const string Sep = "----------------------------------------";

		public static void Run()
		{
			while (true)
			{
				Console.WriteLine();
				Console.WriteLine("=== МАТРИЧНЫЙ КАЛЬКУЛЯТОР ===");
				Console.WriteLine(" 1. Создать матрицу A");
				Console.WriteLine(" 2. Создать матрицу B");
				Console.WriteLine(" 3. Сгенерировать случайные A и B (одинаковый размер)");
				Console.WriteLine(" 4. Показать A и B");
				Console.WriteLine(" 5. A + B");
				Console.WriteLine(" 6. A - B");
				Console.WriteLine(" 7. A * B");
				Console.WriteLine(" 8. A * число");
				Console.WriteLine(" 9. Определитель det(A) и det(B)");
				Console.WriteLine("10. Обратная матрица A^(-1)");
				Console.WriteLine("11. Сравнение A и B (>, <, >=, <=, ==, !=, CompareTo)");
				Console.WriteLine("12. Проверка true/false (невырожденность A)");
				Console.WriteLine("13. Прототип: глубокое копирование A -> C");
				Console.WriteLine("14. Приведение типов (демонстрация)");
				Console.WriteLine("15. Демонстрация пользовательских исключений");
				Console.WriteLine(" 0. Выход");
				Console.Write("Ваш выбор: ");

				string input = Console.ReadLine() ?? "";
				if (!int.TryParse(input, out int choice))
				{
					Console.WriteLine("Ошибка: введите номер пункта меню (целое число).");
					continue;
				}

				try
				{
					switch (choice)
					{
						case 0:
							Console.WriteLine("До свидания!");
							return;
						case 1:
							_a = CreateMatrix("A");
							break;
						case 2:
							_b = CreateMatrix("B");
							break;
						case 3:
							int size = ReadSize();
							_a = Matrix.Random(size, -9, 9);
							_b = Matrix.Random(size, -9, 9);
							Console.WriteLine("A и B сгенерированы случайно (целые из [-9; 9]):");
							Show(_a, _b);
							break;
						case 4:
							Show(_a, _b);
							break;
						case 5:
							RunBinary((x, y) => x + y, "A + B");
							break;
						case 6:
							RunBinary((x, y) => x - y, "A - B");
							break;
						case 7:
							RunBinary((x, y) => x * y, "A * B");
							break;
						case 8:
							RequireA();
							double k = ReadDouble("Множитель k: ");
							Console.WriteLine("A * " + k.ToString("0.##", CultureInfo.InvariantCulture) + " =\n" + _a * k);
							break;
						case 9:
							RequireA();
							Console.WriteLine("det(A) = " + DetText(_a));
							if (_b != null) Console.WriteLine("det(B) = " + DetText(_b));
							break;
						case 10:
							RequireA();
							Console.WriteLine("A^(-1) =\n" + _a.Inverse());
							break;
						case 11:
							CompareDemo();
							break;
						case 12:
							TruthDemo();
							break;
						case 13:
							PrototypeDemo();
							break;
						case 14:
							ConversionsDemo();
							break;
						case 15:
							ExceptionsDemo();
							break;
						default:
							Console.WriteLine("Нет такого пункта меню.");
							break;
					}
				}
				catch (MatrixException ex)
				{
					Console.WriteLine("Ошибка матричной операции: " + ex.Message);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Непредвиденная ошибка: " + ex.Message);
				}
			}
		}

		// ---------- вспомогательные методы меню ----------

		private static Matrix CreateMatrix(string name)
		{
			Console.WriteLine("Создание матрицы " + name + ":");
			int n = ReadSize();
			Console.WriteLine("1 — ввести вручную, 2 — случайная генерация:");
			while (true)
			{
				string line = Console.ReadLine() ?? "";
				if (line.Trim() == "1")
				{
					var m = new Matrix(n);
					Console.WriteLine("Введите элементы построчно через пробел (пример: 1 2 3):");
					for (int r = 0; r < n; r++)
					{
						while (true)
						{
							Console.Write("Строка " + (r + 1) + ": ");
							string[] parts = (Console.ReadLine() ?? "").Split((char[])null,
								StringSplitOptions.RemoveEmptyEntries);
							if (parts.Length != n)
							{
								Console.WriteLine("Ошибка: нужно ровно " + n + " чисел, получено " + parts.Length + ".");
								continue;
							}
							bool ok = true;
							for (int c = 0; c < n; c++)
							{
								if (!TryParseDouble(parts[c], out double val)) { ok = false; break; }
								m[r, c] = val;
							}
							if (ok) break;
							Console.WriteLine("Ошибка ввода числа, повторите строку.");
						}
					}
					return m;
				}
				if (line.Trim() == "2")
					return Matrix.Random(n, -9, 9);
				Console.WriteLine("Введите 1 или 2.");
			}
		}

		private static void RunBinary(Func<Matrix, Matrix, Matrix> op, string label)
		{
			if (_a == null || _b == null)
			{
				Console.WriteLine("Сначала создайте матрицы A и B (пункты 1 и 2 или 3).");
				return;
			}
			Console.WriteLine(label + " =\n" + op(_a, _b));
		}

		private static void RequireA()
		{
			if (_a == null) throw new MatrixDimensionException("Матрица A ещё не создана. Выберите пункт 1 или 3.");
		}

		private static void CompareDemo()
		{
			if (_a == null || _b == null)
			{
				Console.WriteLine("Сначала создайте матрицы A и B.");
				return;
			}
			Console.WriteLine("Сравнение идёт ПО ОПРЕДЕЛИТЕЛЮ; == проверяет поэлементное равенство.");
			Console.WriteLine("det(A) = " + DetText(_a) + ",  det(B) = " + DetText(_b));
			Console.WriteLine("A >  B : " + (_a > _b));
			Console.WriteLine("A <  B : " + (_a < _b));
			Console.WriteLine("A >= B : " + (_a >= _b));
			Console.WriteLine("A <= B : " + (_a <= _b));
			Console.WriteLine("A == B : " + (_a == _b));
			Console.WriteLine("A != B : " + (_a != _b));
			Console.WriteLine("A.CompareTo(B) = " + _a.CompareTo(_b));
		}

		private static void TruthDemo()
		{
			RequireA();
			Console.WriteLine("Операторы true/false: матрица истинна, если невырожденная (det != 0).");
			if (_a)
				Console.WriteLine("A — НЕвырожденная (true), det(A) = " + DetText(_a) + ".");
			else
				Console.WriteLine("A — вырожденная (false), det(A) = 0.");

			// демонстрация работы true/false в логических выражениях
			if (_b != null)
			{
				if (_a)
				{
					if (_b)
						Console.WriteLine("Обе матрицы A и B невырожденные (A && B == true).");
					else
						Console.WriteLine("Хотя бы одна из матриц вырождена (A && B == false).");
				}
				else
					Console.WriteLine("Хотя бы одна из матриц вырождена (A && B == false).");
			}
		}

		private static void PrototypeDemo()
		{
			RequireA();
			Matrix c = _a.Clone();              // паттерн «Прототип»: глубокое копирование
			c[0, 0] = c[0, 0] + 1000;           // портим клон

			Console.WriteLine("Создан клон C = A.Clone(); затем C[0,0] += 1000.");
			Console.WriteLine("A[0,0] = " + _a[0, 0] + "   (оригинал)");
			Console.WriteLine("C[0,0] = " + c[0, 0] + "   (клон)");
			Console.WriteLine("A == C : " + (_a == c) + " — массивы независимы, копирование глубокое.");
		}

		private static void ConversionsDemo()
		{
			RequireA();
			double[,] raw = (double[,])_a;              // явное: Matrix -> double[,]
			Matrix restored = raw;                      // неявное: double[,] -> Matrix
			double determinant = (double)_a;            // явное: Matrix -> double (определитель)

			Console.WriteLine("(double[,])A — копия в массив; восстановленная матрица:\n" + restored);
			Console.WriteLine("(double)A  (определитель) = " + DetText(determinant));
			Console.WriteLine("restored == A : " + (restored == _a));
		}

		private static void ExceptionsDemo()
		{
			Console.WriteLine("1) Сложение матриц разных размеров (MatrixDimensionException):");
			try
			{
				var small = Matrix.Random(2, -3, 3);
				Console.WriteLine("   A + 2x2:\n" + (_a == null ? Matrix.Random(3, -3, 3) + small : _a + small));
			}
			catch (MatrixException ex)
			{
				Console.WriteLine("   Перехвачено: " + ex.GetType().Name + ": " + ex.Message);
			}

			Console.WriteLine("2) Обращение вырожденной матрицы (MatrixSingularException):");
			try
			{
				Console.WriteLine("   " + new Matrix(3).Inverse());   // нулевая матрица 3x3
			}
			catch (MatrixException ex)
			{
				Console.WriteLine("   Перехвачено: " + ex.GetType().Name + ": " + ex.Message);
			}

			Console.WriteLine("3) Индекс вне границ (MatrixIndexException):");
			try
			{
				var probe = new Matrix(2);
				Console.WriteLine("   probe[5, 5] = " + probe[5, 5]);
			}
			catch (MatrixException ex)
			{
				Console.WriteLine("   Перехвачено: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		private static void Show(Matrix a, Matrix b)
		{
			Console.WriteLine("A" + (a == null ? " — не создана" : " (" + a.Size + "x" + a.Size + ")") + ":");
			if (a != null) Console.WriteLine(a);
			Console.WriteLine("B" + (b == null ? " — не создана" : " (" + b.Size + "x" + b.Size + ")") + ":");
			if (b != null) Console.WriteLine(b);
		}

		// ---------- ввод с клавиатуры ----------

		private static int ReadSize()
		{
			while (true)
			{
				Console.Write("Размер матрицы (целое >= 1): ");
				if (int.TryParse(Console.ReadLine() ?? "", out int n) && n >= 1) return n;
				Console.WriteLine("Ошибка: введите целое число >= 1.");
			}
		}

		private static double ReadDouble(string prompt)
		{
			while (true)
			{
				Console.Write(prompt);
				if (TryParseDouble(Console.ReadLine() ?? "", out double v)) return v;
				Console.WriteLine("Ошибка: введите число (например, 3.14 или 3,14).");
			}
		}

		private static bool TryParseDouble(string s, out double value)
		{
			if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
			return double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		private static string DetText(Matrix m) => m.Determinant().ToString("0.###", CultureInfo.InvariantCulture);

		private static string DetText(double d) => d.ToString("0.###", CultureInfo.InvariantCulture);
	}

	// ============================================================
	//  Точка входа
	// ============================================================
	public class Program
	{
		private const string Sep = "----------------------------------------";

		public static void Main()
		{
			Console.OutputEncoding = Encoding.UTF8;   // чтобы кириллица не «поехала» в Windows
			Console.Title = "Матричный калькулятор";

			try
			{
				RunDemo();
				MatrixCalculator.Run();
			}
			catch (MatrixException ex)
			{
				Console.WriteLine("Ошибка матричной операции: " + ex.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Непредвиденная ошибка: " + ex.Message);
			}

			Console.WriteLine("Программа завершена. Нажмите любую клавишу...");
			Console.ReadKey();
		}

		/// <summary>Автоматическая демонстрация всех возможностей класса Matrix.</summary>
		private static void RunDemo()
		{
			Console.WriteLine("=== ДЕМОНСТРАЦИЯ РАБОТЫ КЛАССА MATRIX ===");

			var a = Matrix.Random(3, -3, 3);   // случайная целочисленная 3x3
			var b = Matrix.Random(3, -3, 3);
			Console.WriteLine("A (случайная 3x3):\n" + a);
			Console.WriteLine("B (случайная 3x3):\n" + b);

			Console.WriteLine("A + B:\n" + (a + b));
			Console.WriteLine("A - B:\n" + (a - b));
			Console.WriteLine("A * B:\n" + (a * b));
			Console.WriteLine("A * 2:\n" + (a * 2));

			Console.WriteLine("det(A) = " + a.Determinant().ToString("0.###", CultureInfo.InvariantCulture));

			try
			{
				Console.WriteLine("A^(-1):\n" + a.Inverse());
				Console.WriteLine("Проверка: A * A^(-1):\n" + a * a.Inverse());
			}
			catch (MatrixSingularException ex)
			{
				Console.WriteLine("A^(-1): " + ex.Message);
			}

			// true / false
			Console.WriteLine("A — " + (a ? "невырожденная (true)" : "вырожденная (false)"));

			// сравнение и равенство
			Console.WriteLine("A > B : " + (a > b) + ",  A == B (поэлементно): " + (a == b)
							  + ",  A.CompareTo(B): " + a.CompareTo(b));

			// приведения типов
			double[,] raw = (double[,])a;
			Matrix restored = raw;
			Console.WriteLine("(double[,])A и обратно: restored == A ? " + (restored == a)
							  + ",  (double)A (определитель): "
							  + ((double)a).ToString("0.###", CultureInfo.InvariantCulture));

			// паттерн «Прототип» — глубокое копирование
			Matrix clone = a.Clone();
			clone[0, 0] = 12345;
			Console.WriteLine("Прототип: клон изменён (clone[0,0] = 12345), но A[0,0] = " + a[0, 0]
							  + " — оригинал не затронут (глубокое копирование).");

			// пользовательские исключения
			try
			{
				var bad = a + Matrix.Random(2, -3, 3);   // 3x3 + 2x2
				Console.WriteLine(bad);
			}
			catch (MatrixException ex)
			{
				Console.WriteLine("Поймано исключение: " + ex.GetType().Name + " — " + ex.Message);
			}

			try
			{
				Console.WriteLine(new Matrix(2).Inverse());   // нулевая матрица
			}
			catch (MatrixException ex)
			{
				Console.WriteLine("Поймано исключение: " + ex.GetType().Name + " — " + ex.Message);
			}

			Console.WriteLine(Sep);
			Console.WriteLine("Демонстрация завершена. Дальше — интерактивный калькулятор.");
		}
	}
}