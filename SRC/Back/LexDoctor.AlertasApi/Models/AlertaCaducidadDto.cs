namespace LexDoctor.AlertasApi.Models
{
    public class AlertaCaducidadDto
    {
        public string IdExpediente { get; set; }
        public string Acto { get; set; }
        public string Dema { get; set; }

        public string DescripcionUltimoEscrito { get; set; }
        public DateTime FechaUltimoMovimiento { get; set; }
        public int DiasInactivo { get; set; }
        public int MesesInactivo { get; set; }

        // semáforo
        public string EstadoSemaforo { get; set; }      
        public string ColorSemaforo { get; set; }       
        public int PrioridadSemaforo { get; set; }
    }
}
