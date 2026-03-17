
using System.Collections.Generic;
using System.Threading.Tasks;
using LexDoctor.AlertasApi.Models;

namespace LexDoctor.AlertasApi.Repositories
{
    public interface IExpedienteRepository
    {
        Task<ResultadoPaginado<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsync(int pageNumber, int pageSize);
    }
}