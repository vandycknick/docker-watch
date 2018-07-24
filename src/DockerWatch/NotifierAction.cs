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

            string permissions = "";

            var stat = new string[] { "stat", "-c", "%a", pathChanged };
            using(var statResult = await _dockerService.Exec(containerID, stat))
            {
                if (statResult.ExitCode == 1)
                {
                    await LogError(containerID, pathChanged, statResult.Stderr);
                    return;
                }

                permissions = await GetStreamResult(statResult.Stdout);
            }

            var chmod = new string[] { "chmod", permissions, pathChanged };
            using(var chmodResult = await _dockerService.Exec(containerID, chmod))
            {
                if (chmodResult.ExitCode == 1)
                    await LogError(containerID, pathChanged, chmodResult.Stderr);
            }

        }

        private async Task<string> GetStreamResult(MemoryStream stream)
        {
            string result = "";
            using (StreamReader reader = new StreamReader(stream))
            {
                var text = await reader.ReadToEndAsync();
                result = text.Trim();
            }
            return result;
        }

        private async Task LogError(string containerID, string pathChanged, MemoryStream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var error = await reader.ReadToEndAsync();
                _logger.LogError($"Error syncing file in {containerID} for file {pathChanged}: {error}");
            }
        }
    }
}
