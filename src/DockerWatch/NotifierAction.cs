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

            string permissions = "";

            using (var stream = await _DockerService.Exec(containerId, stat))
            using (StreamReader reader = new StreamReader(stream))
            {
                var text = await reader.ReadToEndAsync();

                permissions = text.Trim();
                Console.WriteLine($"From stat: {permissions}");
            }
            
            var chmod = new string[]
            {
                "chmod", permissions, pathChanged
            };

            using(var stream = await _DockerService.Exec(containerId, chmod))
            using(var reader = new StreamReader(stream))
            {
                var response = await reader.ReadToEndAsync();
                Console.WriteLine($"From chmod: {response}");
            }
        }
    }
}
