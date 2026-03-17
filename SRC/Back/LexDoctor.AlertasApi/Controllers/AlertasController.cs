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

        [HttpGet("caducidad/{pageNumber}/{pageSize}")]
        public async Task<IActionResult> GetAlertasCaducidad(int pageNumber, int pageSize)
        {
            try
            {
                var alertas = await _repository.ObtenerAlertasCaducidadAsync(pageNumber,pageSize);
                return Ok(alertas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al consultar Lex-Doctor: {ex.Message}");
            }
        }
    }
}