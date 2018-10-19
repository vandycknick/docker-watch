using System.Threading.Tasks;
using static DockerWatch.DockerService;

namespace DockerWatch
{
    public interface IDockerService
    {
        Task<ContainerProcessResult> Exec(string containerID, string[] cmd);
    }
}
