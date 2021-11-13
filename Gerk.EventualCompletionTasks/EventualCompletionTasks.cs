using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gerk.EventualCompletionTasks
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class EventualCompletionTasksExtensions
	{
		/// <summary>
		/// Returns the same object if <paramref name="tasks"/> is not <see langword="null"/>. Returns a new object if <paramref name="tasks"/> is null. Needs to be paired with a using of some kind. Is equivalent to <see cref="EventualCompletionTasks.Initialize(ref EventualCompletionTasks?)"/>.
		/// </summary>
		/// <param name="tasks"></param>
		/// <returns></returns>
		public static EventualCompletionTasks Initialize(this EventualCompletionTasks? tasks) => EventualCompletionTasks.Initialize(ref tasks);
	}

	/// <summary>
	/// Holds tasks that get completed eventually.
	/// <example>
	/// <code>
	/// void foo(EventualCompletionTasks? tasks = null) {
	///		using(EventualCompletionTasks.Initialize(tasks))
	///		{
	///			// do stuff here
	///		}
	///	}
	///	</code>
	/// </example>
	/// or
	/// <example>
	/// <code>
	/// void foo(EventualCompletionTasks? tasks = null) {
	///		using(tasks = tasks.Initialize())
	///		{
	///			// do stuff here
	///		}
	///	}
	///	</code>
	/// </example>
	/// </summary>
	public class EventualCompletionTasks : IAsyncDisposable
	{
		/// <summary>		
		/// Returns same object as is passed in, simply for easy of use. Returns a new object if <paramref name="tasks"/> is null. Needs to be paired with a using of some kind. Is equivalent to <see cref="EventualCompletionTasksExtensions.Initialize(EventualCompletionTasks?)"/>.
		/// </summary>
		/// <param name="tasks"></param>
		/// <returns></returns>
		public static EventualCompletionTasks Initialize(ref EventualCompletionTasks? tasks)
		{
			if (tasks == null)
				return tasks = new EventualCompletionTasks();

			Interlocked.Increment(ref tasks.refrencesToMe);
			return tasks;
		}

		readonly ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
		int refrencesToMe = 1;

		/// <summary>
		/// Add a task to eventually be completed
		/// </summary>
		/// <param name="task"></param>
		public void Add(Task task) => tasks.Add(task);
		/// <summary>
		/// Add a <see cref="ValueTask"/> to eventually be completed. Currently just uses <see cref="ValueTask.AsTask"/>.
		/// </summary>
		/// <param name="task"></param>
		public void Add(ValueTask task) => Add(task.AsTask());

		/// <summary>
		/// Constructor. Make sure to put in using.
		/// </summary>
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

		/// <summary>
		/// Don't call this directly, weird things may happen.
		/// </summary>
		/// <returns></returns>
		public ValueTask DisposeAsync()
		{
			if (Interlocked.Decrement(ref refrencesToMe) == 0)
				return WaitAll();
			else
				return default;
		}
	}
}
