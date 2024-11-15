using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace GuardianService.Services
{
    public class KeepAliveService : BackgroundService
    {
        private readonly ILogger<KeepAliveService> _logger;

        public KeepAliveService(ILogger<KeepAliveService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KeepAliveService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Lógica para "mantener despierto" tu servicio
                    await KeepServiceAwakeAsync();

                    // Esperar 2 minutos
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Ocurre cuando el servicio está siendo detenido
                    _logger.LogWarning("KeepAliveService cancelado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocurrió un error en KeepAliveService.");
                }
            }

            _logger.LogInformation("KeepAliveService detenido.");
        }

        private async Task KeepServiceAwakeAsync()
        {
            // Aquí puedes realizar una solicitud hacia tu propio servicio o ejecutar alguna lógica interna.
            _logger.LogInformation("Ejecutando tarea para mantener el servicio despierto.");

            // Ejemplo: Hacer una solicitud HTTP hacia tu propio endpoint
            /*try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("http://guardian-service.onrender.com/api/paciente/obtener/"+"-1");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ping exitoso.");
                }
                else
                {
                    _logger.LogWarning($"Ping fallido: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el ping.");
            }*/
        }
    }
}