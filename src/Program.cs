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
			if (!Options.ParseArgs(args)) {
				Options.Usage();
				return;
			}

			if (Options.Verbose) {
				Log.Write("Source: "+Options.Source);
				Log.Write("Destination: "+Options.Destination);
			}

			try {
				await CopyFile(Options.Source,Options.Destination);
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
			long bucketSize = Options.BufferSize;
			int bestFinished = 0;
			var fatt = new FileInfo(src);
			long srcLen = fatt.Length;
			long numBuckets = srcLen / bucketSize;

			var list = new List<Task>();
			var ilist = new List<long>();

			int index = 0;
			while(true)
			{
				if (index <= numBuckets && list.Count < MaxQueueItems) {
					long start = index * bucketSize;
					var copyTask = CopyChunk(src,dst,start,bucketSize);

					list.Add(copyTask);
					ilist.Add(index);
					index++;
				}
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
				} catch(IOException) {
					if (!Options.Quiet) {
						string w = "IO Error at offset "+start;
							// + (Options.Verbose ? " "+e.Message : "")
						Log.Warning(w);
					}
				}

				await fdst.WriteAsync(buff,0,count);
			}
		}
	}
}
