using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using FirebirdSql.Data.FirebirdClient;
using LexDoctor.AlertasApi.Config;
using LexDoctor.AlertasApi.Models;
using Microsoft.Extensions.Options;

namespace LexDoctor.AlertasApi.Repositories
{
    public class ExpedienteRepository : IExpedienteRepository
    {
        private readonly string _connectionString;

        public ExpedienteRepository(IOptions<DatabaseOptions> options)
        {
            _connectionString = options?.Value?.ConnectionString 
                ?? throw new System.ArgumentNullException(nameof(options), "La cadena de conexión no puede ser nula.");
        }

        public async Task<IEnumerable<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsync()
        {
            const string sql = @"
                WITH MovimientosBrutos AS (
                    SELECT 
                        PROC, DSCR, FECH, HORA,
                        CASE 
                            WHEN FECH IS NOT NULL AND CHAR_LENGTH(TRIM(FECH)) = 8 THEN
                                CAST(
                                    SUBSTRING(FECH FROM 1 FOR 4) || '-' || 
                                    SUBSTRING(FECH FROM 5 FOR 2) || '-' || 
                                    SUBSTRING(FECH FROM 7 FOR 2) 
                                AS DATE)
                            ELSE NULL
                        END AS FechaReal
                    FROM MOVI
                    WHERE TIPO = '1'
                ),
                MovimientosHistoricos AS (
                    SELECT * FROM MovimientosBrutos
                    WHERE FechaReal IS NOT NULL AND FechaReal <= CURRENT_DATE
                ),
                UltimasMarcasTiempo AS (
                    SELECT PROC, MAX(FECH || COALESCE(HORA, '0000')) AS MaxMarcaTiempo
                    FROM MovimientosHistoricos
                    GROUP BY PROC
                ),
                DetalleUltimoMovimiento AS (
                    SELECT mh.PROC, mh.DSCR, mh.FechaReal
                    FROM MovimientosHistoricos mh
                    INNER JOIN UltimasMarcasTiempo umt 
                        ON mh.PROC = umt.PROC AND (mh.FECH || COALESCE(mh.HORA, '0000')) = umt.MaxMarcaTiempo
                )
                SELECT FIRST 20
                    p.PROC AS IdExpediente,
                    p.CARP AS Caratula, 
                    dum.DSCR AS DescripcionUltimoEscrito,
                    dum.FechaReal AS FechaUltimoMovimiento,
                    DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
                    DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
                FROM PROC p
                INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
                ORDER BY DiasInactivo DESC;";

            await using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<AlertaCaducidadDto>(sql);
        }
    }
}
