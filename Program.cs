using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SalvageFile
{
	class Program
	{
		static void Main(string[] args)
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
			CopyFile(Src,Dest);
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

		const int BucketSize = 100;
		static void CopyFile(string src, string dst)
		{
			int bestFinished = 0;
			var fatt = new FileInfo(src);
			long srcLen = fatt.Length;
			long numBuckets = srcLen / BucketSize;
			ParallelOptions po = new ParallelOptions {
				MaxDegreeOfParallelism = 100
			};

			Parallel.For(0,numBuckets,po,(index) => {
				long start = index * BucketSize;
				CopyChunk(src,dst,start,BucketSize);
				int pct = (int)(100 * index / numBuckets);
				if (pct > bestFinished) {
					Interlocked.Exchange(ref bestFinished,pct);
					Console.WriteLine(pct+"%");
				}
			});
		}

		static void CopyChunk(string src, string dst,long start, long size)
		{
			var fsrc = File.Open(src,FileMode.Open,FileAccess.Read,FileShare.Read);
			var fdst = File.Open(dst,FileMode.OpenOrCreate,FileAccess.Write,FileShare.ReadWrite);
			using(fdst) using(fsrc) {
				long srcLen = fsrc.Length;
				long end = (int)Math.Clamp(size,0,srcLen - start + size);
				fsrc.Seek(start,SeekOrigin.Begin);
				fdst.Seek(start,SeekOrigin.Begin);

				for(long i=0; i<end; i++) {
					int b = 0;
					try {
						b = fsrc.ReadByte();
					} catch(IOException) {
						Console.WriteLine("Timeout at offset "+(start+i));
					}
					b = Math.Clamp(b,0,255);
					fdst.WriteByte((byte)b);
				}
			}
		}

		static string Src = null;
		static string Dest = null;
	}
}
