using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gerk.EventualCompletionTasks
{
	public static class Extensions
	{
		public static EventualCompletionTasks Initialize(this EventualCompletionTasks? tasks)
		{
			if (tasks == null)
				return new EventualCompletionTasks();

			Interlocked.Increment(ref tasks.refrencesToMe);
			return tasks;
		}
	}


	public class EventualCompletionTasks : IAsyncDisposable
	{
		readonly ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
		internal int refrencesToMe = 1;

		public void Add(Task task) => tasks.Add(task);
		public void Add(ValueTask task) => Add(task.AsTask());

		public EventualCompletionTasks() { }

		async ValueTask WaitAll()
		{
			List<Exception> exceptions = new List<Exception>();
			while (!tasks.IsEmpty)
			{
				tasks.TryTake(out Task task);
				try
				{
					await task;
				}
				catch (Exception e)
				{
					exceptions.Add(e);
				}
			}
			if (exceptions.Count > 0)
				throw new AggregateException(exceptions);
		}

		public ValueTask DisposeAsync()
		{
			if (Interlocked.Decrement(ref refrencesToMe) == 0)
				return WaitAll();
			else
				return default;
		}
	}
}
