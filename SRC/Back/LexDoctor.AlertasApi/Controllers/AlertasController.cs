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
        public async Task<IActionResult> ObtenerAlertasCaducidad(
             int pageNumber = 1,
             int pageSize = 20,
             string texto = null,
             string semaforo = null,
             string idExpediente = null,
             string exp1 = null,
             string exp2 = null,
             int? mesUltimoMovimiento = null,
             int? anioUltimoMovimiento = null,
             DateTime? fechaUltimoMovimientoDesde = null,
             DateTime? fechaUltimoMovimientoHasta = null)
                {
                    try
                    {
                        var resultado = await _repository.ObtenerAlertasCaducidadAsyncV2(
                            pageNumber,
                            pageSize,
                            texto,
                            semaforo,
                            idExpediente,
                            exp1,
                            exp2,
                            mesUltimoMovimiento,
                            anioUltimoMovimiento,
                            fechaUltimoMovimientoDesde,
                            fechaUltimoMovimientoHasta);

                        return Ok(resultado);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Error interno al consultar Lex-Doctor: {ex.Message}");
                    }
                }



    }
}