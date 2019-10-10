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
				 WL(0,"Usage: "+nameof(SalvageFile)+" (action)")
				+WL(2,"\n Common Options:")
				+WL(2,"\n -h / --help       Print this help")
				+"\n"
				+WL(2,"\n (s)alvage (source) (destination)")
				+WL(2,"\n Copies as much of a file as possible.")
				+WL(3,"\n  Options: ")
				+WL(3,"\n  -b (number)       Size of byte buffer in bytes (default: 4096)."
					+" Larger values can be used to speed up the copy process but at reduced accuracy")
				+WL(3,"\n  -v                Print more information including % complete")
				+WL(3,"\n  -q                Print errors only and suppress copy warning messages")
				+"\n"
				+WL(2,"\n (d)iff (file one) (file two)")
				+WL(2,"\n Compares the blocks of each file and shows if they are different")
				+WL(3,"\n  Options: ")
				+WL(3,"\n  -b (number)       Size blocks in bytes (default: 4096)")
				+WL(3,"\n  -v                Show all block hashes")
				+WL(3,"\n  -q                Only show blocks that differ")
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
				else if (a == 0) {
					WhichAction action = WhichAction.None;
					if (curr.StartsWith("s",StringComparison.InvariantCultureIgnoreCase)) {
						action = WhichAction.Salvage;
					}
					else if (curr.StartsWith("d",StringComparison.InvariantCultureIgnoreCase)) {
						action = WhichAction.Diff;
					}
					else if (!Enum.TryParse(curr,true,out action)) {
						Log.Error("unkown action '"+curr+"'");
						return false;
					}
					if (action == WhichAction.None || !Enum.IsDefined(typeof(WhichAction),action)) {
						Log.Error("Invalid action "+curr);
						return false;
					}
					Action = action;
				}
				else if (curr == "-v") {
					Verbose = true;
					Quiet = false;
				}
				else if (curr == "-q") {
					Verbose = false;
					Quiet = true;
				}
				else if (curr == "-b" && ++a < len) {
					if (!int.TryParse(args[a],out int buff) || buff < 1) {
						Log.Error("size must be a positive number");
						return false;
					}
					BufferSize = buff;
				}
				else if (FileOne == null) {
					FileOne = curr;
				}
				else if (FileTwo == null) {
					FileTwo = curr;
				}
			}

			if (String.IsNullOrWhiteSpace(FileOne)) {
				string id = Action == WhichAction.Salvage ? "Source" : "First";
				Log.Error(id + " file must be provided");
				return false;
			}
			if (String.IsNullOrWhiteSpace(FileTwo)) {
				string id = Action == WhichAction.Salvage ? "Destination" : "Second";
				Log.Error(id + " file must be provided");
				return false;
			}
			if (!File.Exists(FileOne)) {
				Log.Error("Cannot find "+FileOne);
				return false;
			}
			if (Action == WhichAction.Diff && !File.Exists(FileTwo)) {
				Log.Error("Cannot find "+FileTwo);
				return false;
			}

			return true;
		}

		public enum WhichAction { None=0, Salvage=1, Diff=2 }
		public static string FileOne { get; private set; }
		public static string FileTwo { get; private set; }
		public static int BufferSize { get; private set; } = 4096; //4KiB
		public static bool Verbose { get; private set; } = false;
		public static bool Quiet { get; private set; } = false;
		public static WhichAction Action { get; private set; } = WhichAction.None;

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