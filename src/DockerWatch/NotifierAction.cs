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
        const string SYNC_FILE_CMD = "chmod $(stat -c %a {0}) {0}";

        public NotifierAction(DockerService dockerService, ILogger<NotifierAction> logger)
        {
            _dockerService = dockerService;
            _logger = logger;
        }

        public async Task Notify(string containerID, string pathChanged)
        {
            _logger.LogTrace($"Syncing change for {pathChanged} inside container ({containerID}.");

            var cmd = new string[] { "sh", "-c", String.Format(SYNC_FILE_CMD, pathChanged) };
            using(var result = await _dockerService.Exec(containerID, cmd))
            {
                if (result.ExitCode == 1)
                    await LogError(containerID, pathChanged, result.Stderr);
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
