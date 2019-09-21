using System;
using System.IO;
using System.Text;

namespace SalvageFile
{
	public static class Options
	{
		public static void Usage()
		{
			Log.Write(
				 WL(0,"Usage: "+nameof(SalvageFile)+" [options] (source) (destination)")
				+WL(2,"\n Copies as much of a file as possible.")
				+WL(2,"\n Options: ")
				+WL(2,"\n -b (number)       Size of byte buffer in bytes (default: 4096)."
					+" Larger values can be used to speed up the copy process but at reduced accuracy")
				+WL(2,"\n -v                Print more information including % complete")
				+WL(2,"\n -q                Print errors only and suppress copy warning messages")
				+WL(2,"\n -h / --help       Print this help")
			);
		}

		public static bool ParseArgs(string[] args)
		{
			int len = args.Length;
			for (int a=0; a<len; a++) {
				string curr = args[a];
				if (curr == "-h" || curr == "--help") {
					return false;
				}
				else if (curr == "-v") {
					Verbose = true;
					Quiet = false;
				}
				else if (curr == "-q") {
					Verbose = false;
					Quiet = true;
				}
				else if (curr == "-s" && ++a < len) {
					if (!long.TryParse(args[a],out long buff) || buff < 1) {
						Log.Error("buffer size must be a positive number");
						return false;
					}
					BufferSize = buff;
				}
				else if (Source == null) {
					Source = curr;
				}
				else if (Destination == null) {
					Destination = curr;
				}
			}

			if (Source == null) {
				Log.Error("Source file must be provided");
				return false;
			}
			if (Destination == null) {
				Log.Error("Destination file must be provided");
				return false;
			}
			if (!File.Exists(Source)) {
				Log.Error("Cannot find "+Source);
				return false;
			}

			return true;
		}

		public static string Source { get; private set; }
		public static string Destination { get; private set; }
		public static long BufferSize { get; private set; } = 4096; //4KiB
		public static bool Verbose { get; private set; } = false;
		public static bool Quiet { get; private set; } = false;

		static string WL(int indent, string s) {
			int w = Console.WindowWidth;
			int l = 0, c = 0;
			var sb = new  StringBuilder();
			while(l < s.Length) {
				if (c < w) {
					sb.Append(s[l]);
					l++; c++;
				}
				else {
					sb.AppendLine();
					sb.Append(new string(' ',indent));
					c = indent;
				}
			}
			return sb.ToString();
		}
	}
}