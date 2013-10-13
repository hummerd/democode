using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace MergeTool
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length < 6)
			{
				PrintHelp();
				return 0;
			}

			try
			{
				var basePath = GetArg("b", args);
				var firstPath = GetArg("f", args);
				var secondPath = GetArg("s", args);

				var baseCP = GetArg("bcp", args);
				var baseEncoding = baseCP == null
					? Encoding.UTF8
					: Encoding.GetEncoding(int.Parse(baseCP));
				var firstCP = GetArg("fcp", args);
				var firstEncoding = firstCP == null
					? Encoding.UTF8
					: Encoding.GetEncoding(int.Parse(firstCP));
				var secondCP = GetArg("scp", args);
				var secondEncoding = secondCP == null
					? Encoding.UTF8
					: Encoding.GetEncoding(int.Parse(secondCP));


				using (var baseFile = new StreamReader(basePath, baseEncoding))
				using (var firstFile = new StreamReader(firstPath, firstEncoding))
				using (var secondFile = new StreamReader(secondPath, secondEncoding))
				{
					var merge = new Merger();

					var resultPath = GetArg("r", args);
					var resultCP = GetArg("rcp", args);
					var resultEncoding = resultCP == null
						? Encoding.UTF8
						: Encoding.GetEncoding(int.Parse(resultCP));

					var result = resultPath != null
						? new StreamWriter(resultPath, false, resultEncoding)
						: Console.Out;

					try
					{
						merge.Merge(
							baseFile, 
							firstFile,
							secondFile, 
							result);
					}
					finally
					{
						if (resultPath != null)
							result.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				//1. Можно добавить логирование ошибки
				//2. Можно сделать более подробный анализ ошибки
				Console.Error.WriteLine(ex.ToString());
				return -1;
			}

			return 0;
		}


		private static void PrintHelp()
		{
			//По хорошему строки надо выносить в ресурсы, но в учебных целях все напишем здесь
			var msg = "Использование MergeTool: " + Environment.NewLine +
				"MergeTool.exe -b [basePath] -bcp <baseCodePage> -f [firstPath] -fcp <firstCodePage> -s [secondPath] -scp <secondCodePage> -r <resultPath> -rcp <resultCodePage>" + Environment.NewLine +
				"basePath - Обязательный параметр. Путь к базовому файлу]" + Environment.NewLine +
				"baseCodePage - Опциональный параметр. Кодировка базового файла. По умолчанию UTF8" + Environment.NewLine +
				"..." + Environment.NewLine +
				"resultPath - Опциональный параметр. Путь к результирующему файлу. По умолчанию результат выводится в консоль." + Environment.NewLine + 
				"...";

			Console.WriteLine(msg);
		}

		private static string GetArg(string argName, string[] args)
		{ 
			var ix = Array.IndexOf(args,  "-" + argName);
			if (ix < 0)
				ix = Array.IndexOf(args,  "/" + argName);

			if (ix < 0)
				return null;

			if (ix + 1 >= args.Length)
				return null;

			return args[ix + 1];
		}
	}


	/// <summary>
	/// Всякие общие хелперы
	/// </summary>
	public static class Util
	{
		public static List<string> ReadToArray(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			var result = new List<string>();
			var line = reader.ReadLine();

			int i = 0;
			while (line != null)
			{
				result.Add(line);
				line = reader.ReadLine();
			}

			return result;
		}

		public static bool IsTrimmedStringsEqual(string str1, string str2)
		{
			Predicate<char> notWhiteSearch = c => !char.IsWhiteSpace(c);

			var symStart1 = FindCharIndex(str1, notWhiteSearch);
			var symStart2 = FindCharIndex(str2, notWhiteSearch);

			if (symStart1 == -1
				&& symStart2 == -1)
				return true;

			if (symStart1 == -1
			   || symStart2 == -1)
				return false;

			var symEnd1 = FindLastCharIndex(str1, notWhiteSearch);
			var symEnd2 = FindLastCharIndex(str2, notWhiteSearch);

			var len1 = symEnd1 - symStart1 + 1;
			int len2 = symEnd2 - symStart2 + 1;

			return string.Compare(str1, symStart1, str2, symStart2, Math.Max(len1, len2)) == 0;
		}

		public static int FindCharIndex(string str, Predicate<char> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (string.IsNullOrEmpty(str))
				return -1;

			for (int i = 0; i < str.Length; i++)
			{
				if (predicate(str[i]))
					return i;
			}

			return -1;
		}

		public static int FindLastCharIndex(string str, Predicate<char> predicate)
		{
			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (string.IsNullOrEmpty(str))
				return -1;

			for (int i = str.Length - 1; i >= 0; i--)
			{
				if (predicate(str[i]))
					return i;
			}

			return -1;
		}

		public static bool IsSubSetOf<T>(
			List<T> contained,
			int containedStart,
			int containedLength,
			List<T> contains,
			int containsStart,
			int containsLength,
			Func<T, T, bool> equal)
		{
			if (contained == null)
				throw new ArgumentNullException("contained");

			if (contains == null)
				throw new ArgumentNullException("contains");

			if (equal == null)
				throw new ArgumentNullException("equal");

			if (containedStart > contained.Count - 1
				|| containedStart + containedLength > contained.Count)
				throw new IndexOutOfRangeException("containedStart or containedLength out of range");

			if (containsStart > contains.Count - 1
				|| containsStart + containsLength > contains.Count)
				throw new IndexOutOfRangeException("containsStart or containsLength out of range");


			if (containedLength > containsLength)
				return false;

			var firstContained = contained[containedStart];

			var ix = contains.FindIndex(
				containsStart, 
				containsLength,
				s => equal(firstContained, s));

			if (ix < 0)
				return false;

			var containsLeft = containsLength - (ix - containsStart);
			if (containsLeft < containedLength)
				return false;

			for (int i = 1; i < containedLength; i++)
			{
				if (!equal(contains[ix + i], contained[containedStart + i]))
					return false;
			}

			return true;
		}
	}


	/// <summary>
	/// Класс для выполнения слияний
	/// </summary>
	public class Merger
	{
		private enum LineChange
		{ 
			NotChanged,
			Added,
			Removed,
			Cahnged
		}

		private enum MergeSource
		{
			Conflict,
			First,
			Second
		}

		private class DiffInfo
		{
			public DiffInfo(int baseLine, int changedLine, LineChange action)
			{
				BaseLine = baseLine;
				ChangedLine = changedLine;
				Action = action;
			}

			public int BaseLine;
			public int ChangedLine;
			public LineChange Action;

			public override string ToString()
			{
				return BaseLine + " " + ChangedLine + " " + Action;
			}
		}

		private class MergeInfo
		{
			public MergeInfo(int lineIndex, MergeSource source)
			{
				LineIndex = lineIndex;
				Source = source;
			}

			public int LineIndex;
			public MergeSource Source;

			public override string ToString()
			{
				return LineIndex + " " + Source;
			}
		}


		private bool m_debugMode;


		public Merger()
			: this(false)
		{
		}

		public Merger(bool debug)
		{
			m_debugMode = debug;
		}


		public void Merge(
			TextReader baseReader, 
			TextReader first,
			TextReader second,
			TextWriter resultWriter)
		{
			var baseFile = Util.ReadToArray(baseReader);
			var firstFile = Util.ReadToArray(first);
			var secondFile = Util.ReadToArray(second);

			List<DiffInfo> baseToFirst;
			List<DiffInfo> baseToSecond;
			using (var task1 = Task.Factory.StartNew(() => FindDiff(baseFile, firstFile)))
			using (var task2 = Task.Factory.StartNew(() => FindDiff(baseFile, secondFile)))
			{
				task1.Wait();
				task2.Wait();
				
				baseToFirst = task1.Result;
				baseToSecond = task2.Result;		
			}

			var resultDiff = MergeDiffs(baseToFirst, firstFile, baseToSecond, secondFile);

			WriteResultTo(resultWriter, resultDiff, firstFile, secondFile);
		}
				

		private List<DiffInfo> FindDiff(
			List<string> baseFile, 
			List<string> changedFile)
		{
			var result = new List<DiffInfo>(baseFile.Count + changedFile.Count);
			int changeMatchIndex = 0;

			for (var baseIndex = 0; baseIndex < baseFile.Count; baseIndex++)
			{
				var baseLine = baseFile[baseIndex];

				var changedIndex = changedFile.FindIndex(changeMatchIndex, ch => Util.IsTrimmedStringsEqual(ch, baseLine));
				var diff = new DiffInfo(baseIndex, changedIndex, LineChange.NotChanged);

				//Строка не найдена, значит она удалена(либо изменена, но это мы выясним позднее)
				if (changedIndex == -1)
				{
					diff.Action = LineChange.Removed;
				}
				else
				{
					//Между строками в измененном файле "дырка", значит строки между baseMatchIndex и changedIndex
					//либо изменились, либо добавились
					if (changedIndex > changeMatchIndex)
					{
						var lastAdded = result.FindLastIndex(di => di.ChangedLine != -1) + 1;

						//"Дырке" в измененном файле соответствуют некоторые строки в базовом,
						//помечаем их как измененные
						for (int i = lastAdded; i < result.Count; i++)
						{
							result[i].ChangedLine = changeMatchIndex++;
							result[i].Action = LineChange.Cahnged;
						}

						//Строки, пропущенные в измененном, помечаем как добавленные
						for (int i = changeMatchIndex; i < changedIndex; i++)
						{
							result.Add(new DiffInfo(-1, i, LineChange.Added));
						}
					}

					changeMatchIndex = changedIndex + 1;
				}

				result.Add(diff);
			}

			//Остатки из измененного файла
			for (int i = changeMatchIndex; i < changedFile.Count; i++)
			{
				result.Add(new DiffInfo(-1, i, LineChange.Added));
			}

			return result;
		}

		private List<MergeInfo> MergeDiffs(
			IList<DiffInfo> baseToFirst,
			List<string> first,
			IList<DiffInfo> baseToSecond,
			List<string> second)
		{
			var result = new List<MergeInfo>((baseToFirst.Count + baseToSecond.Count)/2);
			int firstDiffIndex = 0;
			int secondDiffIndex = 0;

			while(true)
			{
				//Добавляем добавленное из первого и второго
				var firstAfterAdded = AddAddedLines(firstDiffIndex, result, baseToFirst, MergeSource.First);
				var secondAfterAdded = AddAddedLines(secondDiffIndex, result, baseToSecond, MergeSource.Second);

				//Обрабатываем случай когда один добавленный блок полностью содержится в 
				//другом в этом же месте(один добавил метод А, другой метод А и Б)
				var addedLenFirst = firstAfterAdded - firstDiffIndex;
				var addedLenSecond = secondAfterAdded - secondDiffIndex;

				if (addedLenFirst > 0 && addedLenSecond > 0)
				{
					var secondInFirst = Util.IsSubSetOf(
						second, 
						baseToSecond[secondDiffIndex].ChangedLine,
						addedLenSecond,
						first,
						baseToFirst[firstDiffIndex].ChangedLine,
						addedLenFirst,
						(s1, s2) => Util.IsTrimmedStringsEqual(s1, s2));
					if (secondInFirst)
					{
						var addedSecond = result.Count - addedLenSecond;
						result.RemoveRange(addedSecond, addedLenSecond);
					}
					else
					{
						var firstInSecond = Util.IsSubSetOf(
							first,
							baseToFirst[firstDiffIndex].ChangedLine,
							addedLenFirst,
							second,
							baseToSecond[secondDiffIndex].ChangedLine,
							addedLenSecond,
							(s1, s2) => Util.IsTrimmedStringsEqual(s1, s2));
						if (firstInSecond)
						{
							var addedFirst = result.Count - addedLenSecond - addedLenFirst;
							result.RemoveRange(addedFirst, addedLenFirst);
						}
					}
				}

				//Так как мы идем по строчкам из базового файла, то закончатся они одновременно в первом и втором
				if (firstAfterAdded >= baseToFirst.Count)
					break;

				//Теперь обрабатываем изменение строки из базового файла
				var firstDiff = baseToFirst[firstAfterAdded];
				var secondDiff = baseToSecond[secondAfterAdded];

				if (firstDiff.Action == LineChange.NotChanged 
					&& secondDiff.Action == LineChange.NotChanged)
					result.Add(new MergeInfo(firstDiff.ChangedLine, MergeSource.First));
				else if (firstDiff.Action == LineChange.Cahnged 
					&& secondDiff.Action == LineChange.NotChanged)
					result.Add(new MergeInfo(firstDiff.ChangedLine, MergeSource.First));
				else if  (firstDiff.Action == LineChange.NotChanged 
					&& secondDiff.Action == LineChange.Cahnged)
					result.Add(new MergeInfo(secondDiff.ChangedLine, MergeSource.Second));
				else if (firstDiff.Action == LineChange.Cahnged
					|| secondDiff.Action == LineChange.Cahnged)
				{
					var bothChanged = firstDiff.Action == LineChange.Cahnged
						&& secondDiff.Action == LineChange.Cahnged; 

					if (bothChanged && Util.IsTrimmedStringsEqual(
						first[firstDiff.ChangedLine],
						second[secondDiff.ChangedLine]))
					{
						result.Add(new MergeInfo(firstDiff.ChangedLine, MergeSource.First));
					}
					else
					{
						result.Add(new MergeInfo(secondDiff.ChangedLine, MergeSource.Conflict));
					}
				}
				//Остальные случаи это удаление строчки, просто не добавляем ее в результат

				firstDiffIndex = firstAfterAdded + 1;
				secondDiffIndex = secondAfterAdded + 1;
			}

			return result;
		}

		private int AddAddedLines(
			int startIndex, 
			List<MergeInfo> dest, 
			IList<DiffInfo> diff, 
			MergeSource source)
		{
			var i = startIndex;
			for (; i < diff.Count; i++)
			{
				if (diff[i].Action != LineChange.Added)
					return i;

				dest.Add(new MergeInfo(diff[i].ChangedLine, source));
			}

			return i;
		}

		private void WriteResultTo(
			TextWriter writer, 
			IList<MergeInfo> resultDiff, 
			IList<string> firstFile, 
			IList<string> secondFile)
		{
			for (int i = 0; i < resultDiff.Count; i++)
			{
				var diff = resultDiff[i];

				string line;
				if (diff.Source == MergeSource.First)
					line = firstFile[diff.LineIndex];
				else if (diff.Source == MergeSource.Second)
					line = secondFile[diff.LineIndex];
				else
					line = "*Конфликт*";

				if (m_debugMode)
				{
					var prefix = (diff.Source == MergeSource.First ? "f" : "s") + " " + diff.LineIndex;
					prefix = prefix.PadRight(5, ' ');
					line = prefix + line;
				}

				writer.WriteLine(line);
			}
		}
	}
}
