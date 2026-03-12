using System.Threading.Tasks;

namespace ACS.Communication.Http.uHttpSharp
{
    public static class TaskFactoryExtensions
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);

        public static Task GetCompleted(this TaskFactory factory)
        {
            return CompletedTask;
        }

    }
}
