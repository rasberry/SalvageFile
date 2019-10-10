using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SalvageFile
{
	class Program
	{
		static async Task Main(string[] args)
		{
			if (!Options.ParseArgs(args)) {
				Options.Usage();
				return;
			}

			if (Options.Verbose) {
				Log.Write("First File: "+Options.FileOne);
				Log.Write("Second File: "+Options.FileTwo);
			}

			try {
				if (Options.Action == Options.WhichAction.Salvage) {
					await CopyFile(Options.FileOne,Options.FileTwo);
				}
				else {
					await DiffFile(Options.FileOne,Options.FileTwo);
				}
			} catch(Exception e) {
				#if DEBUG
				Log.Error(e.ToString());
				#else
				Log.Error(e.Message);
				#endif
			}
		}

		const int MaxQueueItems = 128; //this pretty much equates to open file handles
		static async Task CopyFile(string src, string dst)
		{
			int bucketSize = Options.BufferSize;
			int bestFinished = 0;
			var fatt = new FileInfo(src);
			long srcLen = fatt.Length;
			long numBuckets = srcLen / bucketSize;

			var list = new List<Task>();
			var ilist = new List<long>();

			//setup a rolling queue
			int index = 0;
			while(true)
			{
				//fill up the queue if there are spots open
				if (index <= numBuckets && list.Count < MaxQueueItems) {
					long start = index * bucketSize;
					var copyTask = CopyChunk(src,dst,start,bucketSize);

					list.Add(copyTask);
					ilist.Add(index);
					index++;
				}
				//when full, wait for queue to drain at least one item
				else {
					var done = await Task.WhenAny(list);
					int which = list.IndexOf(done);
					Log.Percent(ilist[which],numBuckets, ref bestFinished);
					ilist.RemoveAt(which);
					list.RemoveAt(which);
				}

				//exit only after queue is drained
				if (list.Count < 1) { break; }
			}
		}

		//the filesystem must support seeking for this to work (most do)
		static async Task CopyChunk(string src, string dst,long start, long size)
		{
			var fsrc = File.Open(src,FileMode.Open,FileAccess.Read,FileShare.Read);
			var fdst = File.Open(dst,FileMode.OpenOrCreate,FileAccess.Write,FileShare.ReadWrite);
			using(fdst) using(fsrc) {
				long srcLen = fsrc.Length;
				int count = (int)(start + size < srcLen ? size : srcLen - start);
				fsrc.Seek(start,SeekOrigin.Begin);
				fdst.Seek(start,SeekOrigin.Begin);

				byte[] buff = new byte[count];
				try {
					await fsrc.ReadAsync(buff,0,count);
				}
				catch(IOException) {
					if (!Options.Quiet) {
						string w = "IO Error at offset "+start;
							// + (Options.Verbose ? " "+e.Message : "")
						Log.Warning(w);
					}
				}

				await fdst.WriteAsync(buff,0,count);
			}
		}

		static async Task DiffFile(string one, string two)
		{
			int bucketSize = Options.BufferSize;
			var fone = File.Open(one,FileMode.Open,FileAccess.Read,FileShare.Read);
			var ftwo = File.Open(two,FileMode.Open,FileAccess.Read,FileShare.Read);

			using(fone) using(ftwo) {
				long oneLen = fone.Length;
				long twoLen = ftwo.Length;
				byte[] oneBuff = new byte[bucketSize];
				byte[] twoBuff = new byte[bucketSize];
				long len = Math.Max(oneLen,twoLen);
				long bucketCount = len / bucketSize;
				int padSize = (int)Math.Ceiling(Math.Log(len,16));
				long pos = 0;
				bool oneDone = false;
				bool twoDone = false;

				while(!oneDone || !twoDone)
				{
					string hone = null, htwo = null;
					int oneRead = 0, twoRead = 0;
					bool isSame = false;
					Array.Clear(oneBuff,0,oneBuff.Length);
					Array.Clear(twoBuff,0,twoBuff.Length);

					if (!oneDone) {
						oneRead = await fone.ReadAsync(oneBuff,0,bucketSize);
						if (oneRead < 1) {
							oneDone = true;
						}
					}

					if (!twoDone) {
						twoRead = await ftwo.ReadAsync(twoBuff,0,bucketSize);
						if (twoRead < 1) {
							twoDone = true;
						}
					}

					pos += Math.Max(oneRead,twoRead);

					if (!oneDone && !twoDone) {
						isSame = DiffBucket(oneBuff,twoBuff);
					} else {
						break;
					}

					bool showHash = Options.Verbose || !isSame;
					if (showHash) {
						hone = BytesToMiniView(oneBuff);
						if (!isSame) {
							htwo = BytesToMiniView(twoBuff);
						}
					}

					bool show = !isSame || !Options.Quiet;
					if (show) {
						Log.Write(
							pos.ToString("X"+padSize)
							+" ["+ (isSame ? "=" :  "!") + "]"
							+" "+((double)pos/len).ToString("00.00%")
							+(hone != null ? " "+hone : "")
							+(htwo != null ? " "+htwo : "")
						);
					}
				}
			}
		}

		static bool DiffBucket(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
		{
			return a1.SequenceEqual(a2);
		}

		//static string HashToString(byte[] hash)
		//{
		//	var sb = new StringBuilder(hash.Length * 2);
		//	foreach(byte b in hash) {
		//		sb.Append(b.ToString("X2"));
		//	}
		//	return sb.ToString();
		//}

		const int MiniViewLen = 64;
		static string BytesToMiniView(byte[] data)
		{
			var sb = new StringBuilder(MiniViewLen);
			int seg = data.Length / MiniViewLen;

			for(int b = 0; b<data.Length; b+=seg) {
				int num = data[b] / 16;
				sb.Append(num.ToString("X1"));
			}
			return sb.ToString();
		}
	}
}
