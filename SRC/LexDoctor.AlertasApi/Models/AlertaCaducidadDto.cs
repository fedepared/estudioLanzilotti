namespace LexDoctor.AlertasApi.Models
{
    public class AlertaCaducidadDto
    {
        public string IdExpediente { get; set; }
        public string Acto { get; set; }
        public string Dema { get; set; }
        public string EXP1 { get; set; }
        public string EXP2 { get; set; }
        public string EXP3 { get; set; }
        public string EXP4 { get; set; }
        public string DescripcionUltimoEscrito { get; set; }
        public DateTime FechaUltimoMovimiento { get; set; }
        public int DiasInactivo { get; set; }
        public int MesesInactivo { get; set; }
    }
}
