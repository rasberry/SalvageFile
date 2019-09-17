using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SalvageFile
{
	class Program
	{
		static async Task Main(string[] args)
		{
			if (!ParseArgs(args)) {
				Usage();
				return;
			}
			if (Directory.Exists(Src) || Directory.Exists(Dest)) {
				Console.WriteLine("not implemented");
				return;
			}

			//TODO put a try/catch around this
			await CopyFile(Src,Dest);
		}

		static void Usage()
		{
			Console.WriteLine(
				"Usage: "+nameof(SalvageFile)+" [options] (source) (destination)"
				+"\n Copies as much of a file as possible."
				+"\n Options: "
			);
		}

		static bool ParseArgs(string[] args)
		{
			int len = args.Length;
			for (int a=0; a<len; a++) {
				string curr = args[a];
				if (Src == null) {
					Src = curr;
				}
				else if (Dest == null) {
					Dest = curr;
				}
			}

			if (Src == null) {
				Console.Error.WriteLine("Source file must be provided");
				return false;
			}
			if (Dest == null) {
				Console.Error.WriteLine("Destination file must be provided");
				return false;
			}

			return true;
		}

		const int BucketSize = 4*1024;
		const int MaxQueueItems = 100;
		static async Task CopyFile(string src, string dst)
		{
			int bestFinished = 0;
			var fatt = new FileInfo(src);
			long srcLen = fatt.Length;
			long numBuckets = srcLen / BucketSize;

			var list = new List<Task>();
			var ilist = new List<long>();

			int index = 0;
			while(true)
			{
				if (index <= numBuckets && list.Count < MaxQueueItems) {
					long start = index * BucketSize;
					var copyTask = CopyChunk(src,dst,start,BucketSize);

					list.Add(copyTask);
					ilist.Add(index);
					index++;
				}
				else {
					var done = await Task.WhenAny(list);
					int which = list.IndexOf(done);
					int pct = (int)(100 * ilist[which] / (numBuckets+1));
					if (pct > bestFinished) {
						bestFinished = pct;
						Console.WriteLine(pct+"%");
					}
					ilist.RemoveAt(which);
					list.RemoveAt(which);
				}

				if (list.Count < 1) { break; }
			}
		}

		static async Task CopyChunk(string src, string dst,long start, long size)
		{
			var fsrc = File.Open(src,FileMode.Open,FileAccess.Read,FileShare.Read);
			var fdst = File.Open(dst,FileMode.OpenOrCreate,FileAccess.Write,FileShare.ReadWrite);
			using(fdst) using(fsrc) {
				long srcLen = fsrc.Length;
				int count = (int)(start + size < srcLen ? size : srcLen - start);
				// Console.WriteLine("D: count = "+count);
				fsrc.Seek(start,SeekOrigin.Begin);
				fdst.Seek(start,SeekOrigin.Begin);

				byte[] buff = new byte[count];
				try {
					await fsrc.ReadAsync(buff,0,count);
				} catch(IOException) {
					Console.WriteLine("Timeout at offset "+start);
				}

				await fdst.WriteAsync(buff,0,count);
			}
		}

		static string Src = null;
		static string Dest = null;
	}
}
