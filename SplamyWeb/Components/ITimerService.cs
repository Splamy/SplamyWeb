using System.Threading.Tasks;

namespace SplamyWeb.Components;

public interface ITimerService
{
	void Register(Func<Task> func);
}
