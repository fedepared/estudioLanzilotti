
using System.Collections.Generic;
using System.Threading.Tasks;
using LexDoctor.AlertasApi.Models;

namespace LexDoctor.AlertasApi.Repositories
{
    public interface IExpedienteRepository
    {
        Task<IEnumerable<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsync();
    }
}