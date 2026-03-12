namespace LexDoctor.AlertasApi.Models
{
    public class AlertaCaducidadDto
    {
        public string IdExpediente { get; set; }
        public string Caratula { get; set; }
        public string DescripcionUltimoEscrito { get; set; }
        public DateTime FechaUltimoMovimiento { get; set; }
        public int DiasInactivo { get; set; }
        public int MesesInactivo { get; set; }
    }
}
