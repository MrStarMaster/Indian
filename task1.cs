using System;

namespace Lab1
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			// ---------- Задание 1: a^n (только умножением) ----------
			Console.WriteLine("=== Задание 1: a^n ===");
			Console.Write("Введите a (натуральное): ");
			int a = int.Parse(Console.ReadLine()!);
			Console.Write("Введите n (натуральное): ");
			int n = int.Parse(Console.ReadLine()!);

			if (a <= 0 || n <= 0)
			{
				Console.WriteLine("Ошибка: a и n должны быть натуральными (> 0).");
			}
			else
			{
				Console.WriteLine($"a^n = {Power(a, n)}");
			}

			// ---------- Задание 2: зачёркивание второй цифры ----------
			Console.WriteLine();
			Console.WriteLine("=== Задание 2: x -> n ===");
			Console.Write("Введите x (x >= 100): ");
			int x = int.Parse(Console.ReadLine()!);

			if (x < 100)
			{
				Console.WriteLine("Ошибка: x должен быть >= 100.");
			}
			else
			{
				Console.WriteLine($"n = {CrossOutSecondDigit(x)}");
			}
		}

		/// <summary>
		/// Возводит a в степень n, используя ТОЛЬКО операцию умножения.
		/// </summary>
		static long Power(int a, int n)
		{
			long result = 1;
			for (int i = 0; i < n; i++)
				result *= a;          // n-кратное умножение вместо Math.Pow
			return result;
		}

		/// <summary>
		/// Зачёркивает вторую (слева) цифру числа x и приписывает её справа.
		/// Пример: x = 121111 -> n = 111112.
		/// </summary>
		static long CrossOutSecondDigit(int x)
		{
			// 1) Считаем количество цифр в x
			int digits = 0;
			int t = x;
			while (t > 0)
			{
				t /= 10;
				digits++;
			}

			// 2) pow = 10^(digits-2) — вес разрядов после второй цифры
			long pow = 1;
			for (int i = 0; i < digits - 2; i++)
				pow *= 10;

			// 3) Выделяем нужные части числа
			long first = x / (pow * 10);        // первая цифра (самая левая)
			long second = (x / pow) % 10;        // вторая цифра
			long tail = x % pow;               // всё, что идёт после второй цифры

			// 4) Число без второй цифры: first + tail (вторая цифра «выпадает»)
			long withoutSecond = first * pow + tail;

			// 5) Приписываем вторую цифру справа
			return withoutSecond * 10 + second;
		}
	}
}
