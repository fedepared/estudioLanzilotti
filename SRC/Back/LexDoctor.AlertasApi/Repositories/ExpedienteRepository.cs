using Dapper;
using FirebirdSql.Data.FirebirdClient;
using LexDoctor.AlertasApi.Config;
using LexDoctor.AlertasApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexDoctor.AlertasApi.Repositories
{
    public class ExpedienteRepository : IExpedienteRepository
    {
        private readonly string _connectionString;
        private readonly AlertasCaducidadOptions _alertasOptions;
        private readonly IMemoryCache _cache;

        public ExpedienteRepository(
            IOptions<DatabaseOptions> options,
            IOptions<AlertasCaducidadOptions> alertasOptions,
            IMemoryCache cache)
        {
            _connectionString = options?.Value?.ConnectionString
                ?? throw new ArgumentNullException(nameof(options), "La cadena de conexión no puede ser nula.");

            _alertasOptions = alertasOptions?.Value
                ?? throw new ArgumentNullException(nameof(alertasOptions), "La configuración de alertas no puede ser nula.");

            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            if (_alertasOptions.DiasInicioAmarillo < 0)
                throw new ArgumentException("DiasInicioAmarillo no puede ser negativo.");

            if (_alertasOptions.DiasInicioRojo <= _alertasOptions.DiasInicioAmarillo)
                throw new ArgumentException("DiasInicioRojo debe ser mayor que DiasInicioAmarillo.");
        }

        private string BuildCacheKey(
            int pageNumber,
            int pageSize,
            string texto,
            string semaforo,
            string idExpediente)
        {
            texto = string.IsNullOrWhiteSpace(texto) ? "" : texto.Trim().ToLowerInvariant();
            semaforo = string.IsNullOrWhiteSpace(semaforo) ? "" : semaforo.Trim().ToLowerInvariant();
            idExpediente = string.IsNullOrWhiteSpace(idExpediente) ? "" : idExpediente.Trim().ToLowerInvariant();

            return $"alertas-caducidad-v2:" +
                   $"page={pageNumber}:" +
                   $"size={pageSize}:" +
                   $"texto={texto}:" +
                   $"semaforo={semaforo}:" +
                   $"id={idExpediente}:" +
                   $"diaAm={_alertasOptions.DiasInicioAmarillo}:" +
                   $"diaRo={_alertasOptions.DiasInicioRojo}";
        }

        public async Task<ResultadoPaginado<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsyncV2(
            int pageNumber,
            int pageSize,
            string texto = null,
            string semaforo = null,
            string idExpediente = null)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            semaforo = string.IsNullOrWhiteSpace(semaforo)
                ? null
                : semaforo.Trim().ToLowerInvariant();

            string cacheKey = BuildCacheKey(pageNumber, pageSize, texto, semaforo, idExpediente);

            if (_cache.TryGetValue(cacheKey, out ResultadoPaginado<AlertaCaducidadDto> resultadoCacheado))
            {
                return resultadoCacheado;
            }

            string cacheKeyResumenGlobal =
                $"alertas-caducidad-v2:resumen-global:diaAm={_alertasOptions.DiasInicioAmarillo}:diaRo={_alertasOptions.DiasInicioRojo}";

            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            const string cteBase = @"
                WITH UltimasMarcasTiempo AS (
                    SELECT
                        m.PROC,
                        MAX(m.FECH || COALESCE(m.HORA, '0000') || MOVI) AS MaxMarcaTiempo
                    FROM MOVI m
                    WHERE TRIM(COALESCE(m.FECH, '')) <> ''
                      AND m.HECH = 'P'
                    GROUP BY m.PROC
                ),
                DetalleUltimoMovimiento AS (
                    SELECT
                        m.PROC,
                        COALESCE(m.DSCR, '') AS DSCR,
                        m.HECH,
                        CASE 
                            WHEN TRIM(COALESCE(m.FECH, '')) = '' THEN NULL
                            ELSE CAST(
                                SUBSTRING(m.FECH FROM 1 FOR 4) || '-' || 
                                SUBSTRING(m.FECH FROM 5 FOR 2) || '-' || 
                                SUBSTRING(m.FECH FROM 7 FOR 2) 
                            AS DATE)
                        END AS FechaReal
                    FROM MOVI m
                    INNER JOIN UltimasMarcasTiempo umt
                        ON umt.PROC = m.PROC
                       AND umt.MaxMarcaTiempo = (m.FECH || COALESCE(m.HORA, '0000') || m.MOVI)
                    WHERE m.HECH = 'P' AND TRIM(COALESCE(m.FECH, '')) <> ''
                ),
                DatosCalculados AS (
                    SELECT
                        p.PROC AS IdExpediente,
                        COALESCE(p.ACTO, '') AS Acto,
                        COALESCE(p.DEMA, '') AS Dema,
                        dum.DSCR AS DescripcionUltimoEscrito,
                        dum.FechaReal AS FechaUltimoMovimiento,
                        DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
                        DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
                    FROM PROC p
                    INNER JOIN DetalleUltimoMovimiento dum
                        ON dum.PROC = p.PROC
                    WHERE (p.GRUP IS NULL OR p.GRUP <> 'B') AND EXTRACT(YEAR FROM dum.FechaReal) >= 2020
                ),
                DatosBase AS (
                    SELECT
                        dc.IdExpediente,
                        dc.Acto,
                        dc.Dema,
                        dc.DescripcionUltimoEscrito,
                        dc.FechaUltimoMovimiento,
                        dc.DiasInactivo,
                        dc.MesesInactivo,
                        CASE
                            WHEN dc.DiasInactivo >= @DiasInicioRojo THEN 'rojo'
                            WHEN dc.DiasInactivo >= @DiasInicioAmarillo THEN 'amarillo'
                            ELSE 'verde'
                        END AS EstadoSemaforo,
                        CASE
                            WHEN dc.DiasInactivo >= @DiasInicioRojo THEN CAST(@ColorRojo AS VARCHAR(20))
                            WHEN dc.DiasInactivo >= @DiasInicioAmarillo THEN CAST(@ColorAmarillo AS VARCHAR(20))
                            ELSE CAST(@ColorVerde AS VARCHAR(20))
                        END AS ColorSemaforo,
                        CASE
                            WHEN dc.DiasInactivo >= @DiasInicioRojo THEN 1
                            WHEN dc.DiasInactivo >= @DiasInicioAmarillo THEN 2
                            ELSE 3
                        END AS PrioridadSemaforo
                    FROM DatosCalculados dc
                )";

            var parameters = new DynamicParameters();
            parameters.Add("StartRow", startRow, DbType.Int32);
            parameters.Add("EndRow", endRow, DbType.Int32);
            parameters.Add("DiasInicioAmarillo", _alertasOptions.DiasInicioAmarillo, DbType.Int32);
            parameters.Add("DiasInicioRojo", _alertasOptions.DiasInicioRojo, DbType.Int32);
            parameters.Add("ColorRojo", _alertasOptions.ColorRojo, DbType.String);
            parameters.Add("ColorAmarillo", _alertasOptions.ColorAmarillo, DbType.String);
            parameters.Add("ColorVerde", _alertasOptions.ColorVerde, DbType.String);

            var where = new StringBuilder(" WHERE 1 = 1 ");

            if (!string.IsNullOrWhiteSpace(texto))
            {
                where.Append(@"
                AND (
                       db.IdExpediente CONTAINING @Texto
                    OR db.Acto CONTAINING @Texto
                    OR db.Dema CONTAINING @Texto
                    OR db.DescripcionUltimoEscrito CONTAINING @Texto
                )");
                parameters.Add("Texto", texto.Trim(), DbType.String);
            }

            if (!string.IsNullOrWhiteSpace(idExpediente))
            {
                where.Append(" AND db.IdExpediente = @IdExpediente ");
                parameters.Add("IdExpediente", idExpediente.Trim(), DbType.String);
            }

            if (!string.IsNullOrWhiteSpace(semaforo))
            {
                if (semaforo == "rojo" || semaforo == "amarillo" || semaforo == "verde")
                {
                    where.Append(" AND db.EstadoSemaforo = @Semaforo ");
                    parameters.Add("Semaforo", semaforo, DbType.String);
                }
            }

            string sqlCount = cteBase + @"
                SELECT COUNT(*)
                FROM DatosBase db
                " + where;

                        string sqlData = cteBase + @"
                SELECT
                    db.IdExpediente,
                    db.Acto,
                    db.Dema,
                    db.DescripcionUltimoEscrito,
                    db.FechaUltimoMovimiento,
                    db.DiasInactivo,
                    db.MesesInactivo,
                    db.EstadoSemaforo,
                    db.ColorSemaforo,
                    db.PrioridadSemaforo
                FROM DatosBase db
                " + where + @"
                ORDER BY
                    db.PrioridadSemaforo ASC,
                    db.DiasInactivo DESC,
                    db.FechaUltimoMovimiento ASC
                ROWS @StartRow TO @EndRow;";

                        string sqlResumenGlobal = cteBase + @"
                SELECT
                    COALESCE(SUM(CASE WHEN db.EstadoSemaforo = 'rojo' THEN 1 ELSE 0 END), 0) AS Rojos,
                    COALESCE(SUM(CASE WHEN db.EstadoSemaforo = 'amarillo' THEN 1 ELSE 0 END), 0) AS Amarillos,
                    COALESCE(SUM(CASE WHEN db.EstadoSemaforo = 'verde' THEN 1 ELSE 0 END), 0) AS Verdes
                FROM DatosBase db;";

            await using var connection = new FbConnection(_connectionString);

            try
            {
                await connection.OpenAsync();

                var total = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);
                var datos = (await connection.QueryAsync<AlertaCaducidadDto>(sqlData, parameters)).ToList();

                ResumenSemaforosDto resumenGlobal;

                if (!_cache.TryGetValue(cacheKeyResumenGlobal, out resumenGlobal))
                {
                    resumenGlobal = await connection.QueryFirstOrDefaultAsync<ResumenSemaforosDto>(sqlResumenGlobal, parameters)
                                    ?? new ResumenSemaforosDto();

                    var proximaMedianocheResumen = new DateTimeOffset(DateTime.Today.AddDays(1));

                    _cache.Set(cacheKeyResumenGlobal, resumenGlobal, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = proximaMedianocheResumen
                    });
                }

                var resultado = new ResultadoPaginado<AlertaCaducidadDto>
                {
                    TotalRegistros = total,
                    Datos = datos,
                    ResumenSemaforos = resumenGlobal
                };

                var proximaMedianoche = new DateTimeOffset(DateTime.Today.AddDays(1));

                _cache.Set(cacheKey, resultado, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = proximaMedianoche
                });

                return resultado;
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
        public async Task<ResultadoPaginado<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsync(int pageNumber, int pageSize)
        {
            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            const string cteBase = @"
                WITH UltimasMarcasTiempo AS (
                    SELECT PROC, MAX(FECH || COALESCE(HORA, '0000') || MOVI) AS MaxMarcaTiempo
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
                        ON m.PROC = umt.PROC AND (m.FECH || COALESCE(m.HORA, '0000') || m.MOVI) = umt.MaxMarcaTiempo
                    WHERE m.HECH = 'P'
                ) ";

            string sqlCount = cteBase + @"
                SELECT COUNT(p.PROC)
                FROM PROC p
                INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
                WHERE EXTRACT(YEAR FROM dum.FechaReal) >= 2020 AND (p.GRUP IS NULL OR p.GRUP <> 'B');";

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