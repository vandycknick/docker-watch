using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DockerWatch
{
    public class NotifierAction : INotifierAction
    {
        private readonly DockerService _dockerService;
        private readonly ILogger _logger;

        public NotifierAction(DockerService dockerService, ILogger<NotifierAction> logger)
        {
            _dockerService = dockerService;
            _logger = logger;
        }

        public async Task Notify(string containerID, string pathChanged)
        {
            _logger.LogTrace($"Syncing change for {pathChanged} inside container ({containerID}.");
           
            var stat = new string[]
            {
                "stat", "-c", "%a", pathChanged
            };

            string permissions = "";

            using (var stream = await _dockerService.Exec(containerID, stat))
            using (StreamReader reader = new StreamReader(stream))
            {
                var text = await reader.ReadToEndAsync();

                permissions = text.Trim();
                _logger.LogTrace($"From stat: {permissions}");
            }
            
            var chmod = new string[]
            {
                "chmod", permissions, pathChanged
            };

            using(var stream = await _dockerService.Exec(containerID, chmod))
            using(var reader = new StreamReader(stream))
            {
                var response = await reader.ReadToEndAsync();
                _logger.LogTrace($"From chmod: {response}");
            }
        }
    }
}
