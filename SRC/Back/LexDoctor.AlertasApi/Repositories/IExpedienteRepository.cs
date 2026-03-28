using System.Threading.Tasks;
using LexDoctor.AlertasApi.Models;

namespace LexDoctor.AlertasApi.Repositories
{
    public interface IExpedienteRepository
    {
         Task<ResultadoPaginado<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsyncV2(
            int pageNumber,
            int pageSize,
            string texto = null,
            string semaforo = null,
            string idExpediente = null,
            string exp1 = null,
            string exp2 = null);

    }
}