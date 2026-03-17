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

        public async Task<ResultadoPaginado<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsync(int pageNumber, int pageSize)
        {
            // límites
            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            // 1. CTE Base
            const string cteBase = @"
                WITH UltimasMarcasTiempo AS (
                    SELECT 
                        PROC, 
                        MAX(FECH || COALESCE(HORA, '0000') || MOVI) AS LlaveDesempate
                    FROM MOVI
                    WHERE TRIM(FECH) <> '' AND HECH = 'P'
                    GROUP BY PROC
                ),
                DetalleUltimoMovimiento AS (
                    SELECT 
                        m.PROC,
                        m.DSCR,
                        m.HECH,
                        CAST(
                            SUBSTRING(m.FECH FROM 1 FOR 4) || '-' || 
                            SUBSTRING(m.FECH FROM 5 FOR 2) || '-' || 
                            SUBSTRING(m.FECH FROM 7 FOR 2) 
                        AS DATE) AS FechaReal
                    FROM MOVI m
                    INNER JOIN UltimasMarcasTiempo umt 
                        ON m.PROC = umt.PROC AND (m.FECH || COALESCE(m.HORA, '0000') || m.MOVI) = umt.LlaveDesempate
                ) ";

            // 2. Conteo: con el filtro de años y excluyendo archivados ('B')
            string sqlCount = cteBase + @"
                SELECT COUNT(p.PROC)
                FROM PROC p
                INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
                WHERE EXTRACT(YEAR FROM dum.FechaReal) >= 2016 
                  AND (p.GRUP IS NULL OR p.GRUP <> 'B');";

            // 3. Datos y paginación
            string sqlData = cteBase + @"
                SELECT 
                    p.PROC AS IdExpediente,
                    p.ACTO AS ACTO, 
                    p.DEMA AS DEMA,
                    p.EXP1 AS EXP1,
                    p.EXP2 AS EXP2,
                    p.EXP3 AS EXP3,
                    p.EXP4 AS EXP4,
                    dum.DSCR AS DescripcionUltimoEscrito,
                    dum.FechaReal AS FechaUltimoMovimiento,
                    dum.HECH AS HECHO,
                    DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
                    DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
                FROM PROC p
                INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
                WHERE EXTRACT(YEAR FROM dum.FechaReal) >= 2020 
                  AND (p.GRUP IS NULL OR p.GRUP <> 'B')
                ORDER BY DiasInactivo DESC
                ROWS @StartRow TO @EndRow;";

            await using var connection = new FbConnection(_connectionString);

            try
            {
                await connection.OpenAsync();

                // Ejecutamos ambas consultas en la misma conexión abierta
                var total = await connection.ExecuteScalarAsync<int>(sqlCount);
                var datos = await connection.QueryAsync<AlertaCaducidadDto>(sqlData, new { StartRow = startRow, EndRow = endRow });

                return new ResultadoPaginado<AlertaCaducidadDto>
                {
                    TotalRegistros = total,
                    Datos = datos
                };
            }
            catch (FbException ex)
            {
                throw new Exception($"Error nativo de Firebird al consultar alertas: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado en el repositorio de expedientes: {ex.Message}", ex);
            }
        }
    }
 }
