using System;

namespace SalvageFile
{
	public static class Log
	{
		public static void Write(string message)
		{
			Console.WriteLine(message);
		}

		public static void Warning(string message)
		{
			Console.Error.WriteLine("W: "+message);
		}

		public static void Error(string message)
		{
			Console.Error.WriteLine("E: "+message);
		}

		public static void Debug(string message)
		{
			#if DEBUG
			Console.WriteLine("D: "+message);
			#endif
		}

		public static void Percent(long pos, long total, ref int best)
		{
			if (!Options.Verbose) { return; }
			if (total < 1) { return; }

			int pct = (int)(100 * pos / total);
			if (pct > best) {
				best = pct;
				Log.Write(pct+"%");
			}
		}
	}
}