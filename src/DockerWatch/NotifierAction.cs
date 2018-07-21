using System;
using System.IO;
using System.Threading.Tasks;

namespace DockerWatch
{
    public class NotifierAction : INotifierAction
    {
        private readonly DockerService _DockerService;

        public NotifierAction(DockerService dockerService)
        {
            _DockerService = dockerService;
        }

        public async Task Notify(string containerId, string pathChanged)
        {
            Console.WriteLine(pathChanged);

            var stat = new string[]
            {
                "stat", "-c", "%a", pathChanged
            };

            using (var stream = await _DockerService.Exec(containerId, stat))
            using (StreamReader reader = new StreamReader(stream))
            {
                String text = await reader.ReadToEndAsync();
                Console.WriteLine(text);
                // return Task.CompletedTask;
            }
            Console.WriteLine("notified contianer");
        }
    }
}
