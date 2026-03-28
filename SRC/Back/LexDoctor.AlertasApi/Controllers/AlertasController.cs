using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LexDoctor.AlertasApi.Repositories;

namespace LexDoctor.AlertasApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertasController : ControllerBase
    {
        private readonly IExpedienteRepository _repository;

        public AlertasController(IExpedienteRepository repository)
        {
            _repository = repository;
        }


        [HttpGet("caducidad")]
        public async Task<IActionResult> GetAlertasCaducidad(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string texto = null,
            [FromQuery] string semaforo = null,
            [FromQuery] string idExpediente = null)
        {
            try
            {
                var resultado = await _repository.ObtenerAlertasCaducidadAsyncV2(
                    pageNumber,
                    pageSize,
                    texto,
                    semaforo,
                    idExpediente);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al consultar Lex-Doctor: {ex.Message}");
            }
        }
    
    

    }
}