using System.Threading.Tasks;

namespace DockerWatch
{
    public interface INotifierAction
    {
         Task Notify(string containerId, string pathChanged);
    }
}
