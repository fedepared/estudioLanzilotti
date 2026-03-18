namespace LexDoctor.AlertasApi.Models
{    
    public class ResultadoPaginado<T>
    {
        public IEnumerable<T> Datos { get; set; }
        public int TotalRegistros { get; set; }

        public ResumenSemaforosDto ResumenSemaforos { get; set; }
    }
}
