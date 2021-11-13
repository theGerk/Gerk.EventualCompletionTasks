using Gerk.EventualCompletionTasks;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
	//Tests don't actually exist
	public class UnitTest1
	{
		[Fact]
		public async Task Test1()
		{

			await using var tasks = new EventualCompletionTasks();
			tasks.Add(Test2(tasks));
		}

		public async Task Test2(EventualCompletionTasks? tasks = null)
		{
			await using (tasks = tasks.Initialize())
			{
				tasks.Add(Task.Delay(1000 * 20));
			}
		}
	}
}