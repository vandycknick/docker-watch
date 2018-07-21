using System;
using System.Threading.Tasks;

namespace DockerWatch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var docker = new DockerService();
            var notify = new NotifierAction(docker);
            var monitor = new ContainerMonitor(docker, notify);

            await monitor.Start();
            await Task.Delay(30000);
            Console.WriteLine("Done!");
        }
    }
}
