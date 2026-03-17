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
            // limites
            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            // CTE Base:  para reutilizarla en ambas consultas y refactorizar
            const string cteBase = @"
                WITH UltimasMarcasTiempo AS (
                    SELECT PROC, MAX(FECH || COALESCE(HORA, '0000')) AS MaxMarcaTiempo
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
                        ON m.PROC = umt.PROC AND (m.FECH || COALESCE(m.HORA, '0000')) = umt.MaxMarcaTiempo
                    WHERE m.HECH = 'P'
                ) ";

            // contador para el paginado
            string sqlCount = cteBase + @"
                SELECT COUNT(p.PROC)
                FROM PROC p
                INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
                WHERE NOT EXISTS (
                    SELECT 1 
                    FROM MOVI ms 
                    WHERE ms.PROC = p.PROC 
                      AND ms.HECH IN ('P', 'N') 
                      AND ms.DSCR CONTAINING 'SENTENCIA'
                );";

            // Cantidad de datos de rows
            string sqlData = cteBase + @"
                SELECT 
                    p.PROC AS IdExpediente,
                    p.ACTO AS ACTO, 
                    p.DEMA AS DEMA,
                    dum.DSCR AS DescripcionUltimoEscrito,
                    dum.FechaReal AS FechaUltimoMovimiento,
                    dum.HECH AS HECHO,
                    DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
                    DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
                FROM PROC p
                INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
                WHERE NOT EXISTS (
                    SELECT 1 
                    FROM MOVI ms 
                    WHERE ms.PROC = p.PROC 
                      AND ms.HECH IN ('P', 'N') 
                      AND ms.DSCR CONTAINING 'SENTENCIA'
                )
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
                // Somos claros con el error del motor
                throw new Exception($"Error nativo de Firebird al consultar alertas: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Atrapamos errores de mapeo de Dapper o de lógica
                throw new Exception($"Error inesperado en el repositorio de expedientes: {ex.Message}", ex);
            }
        }
    }
 }
