using Dapper;
using FirebirdSql.Data.FirebirdClient;
using LexDoctor.AlertasApi.Config;
using LexDoctor.AlertasApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
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

            if (_alertasOptions.DiasInicioNaranja <= _alertasOptions.DiasInicioAmarillo)
                throw new ArgumentException("DiasInicioNaranja debe ser mayor que DiasInicioAmarillo.");

            if (_alertasOptions.DiasInicioRojo <= _alertasOptions.DiasInicioNaranja)
                throw new ArgumentException("DiasInicioRojo debe ser mayor que DiasInicioNaranja.");
        }

        private string BuildCacheKey(
    int pageNumber,
    int pageSize,
    string texto,
    string semaforo,
    string idExpediente,
    string exp1,
    string exp2,
    int? mesUltimoMovimiento,
    int? anioUltimoMovimiento,
    DateTime? fechaUltimoMovimientoDesde,
    DateTime? fechaUltimoMovimientoHasta)
        {
    texto = string.IsNullOrWhiteSpace(texto) ? "" : texto.Trim().ToLowerInvariant();
    semaforo = string.IsNullOrWhiteSpace(semaforo) ? "" : semaforo.Trim().ToLowerInvariant();
    idExpediente = string.IsNullOrWhiteSpace(idExpediente) ? "" : idExpediente.Trim().ToLowerInvariant();
    exp1 = string.IsNullOrWhiteSpace(exp1) ? "" : exp1.Trim().ToLowerInvariant();
    exp2 = string.IsNullOrWhiteSpace(exp2) ? "" : exp2.Trim().ToLowerInvariant();

    return $"alertas-caducidad-v2:" +
           $"page={pageNumber}:" +
           $"size={pageSize}:" +
           $"texto={texto}:" +
           $"semaforo={semaforo}:" +
           $"id={idExpediente}:" +
           $"exp1={exp1}:" +
           $"exp2={exp2}:" +
           $"mesUltMov={(mesUltimoMovimiento?.ToString() ?? "")}:" +
           $"anioUltMov={(anioUltimoMovimiento?.ToString() ?? "")}:" +
           $"diaAm={_alertasOptions.DiasInicioAmarillo}:" +
           $"diaNar={_alertasOptions.DiasInicioNaranja}:" +
           $"diaRo={_alertasOptions.DiasInicioRojo}"+
           $"fechaDesde={(fechaUltimoMovimientoDesde?.ToString("yyyy-MM-dd") ?? "")}:" +
            $"fechaHasta={(fechaUltimoMovimientoHasta?.ToString("yyyy-MM-dd") ?? "")}:";
}

            public async Task<ResultadoPaginado<AlertaCaducidadDto>> ObtenerAlertasCaducidadAsyncV2(
            int pageNumber,
            int pageSize,
            string texto = null,
            string semaforo = null,
            string idExpediente = null,
            string exp1 = null,
            string exp2 = null,
            int? mesUltimoMovimiento = null,
            int? anioUltimoMovimiento = null,
            DateTime? fechaUltimoMovimientoDesde = null,
            DateTime? fechaUltimoMovimientoHasta = null)

        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100;
            if (mesUltimoMovimiento.HasValue && (mesUltimoMovimiento < 1 || mesUltimoMovimiento > 12))
                throw new ArgumentException("mesUltimoMovimiento debe estar entre 1 y 12.");

            if (anioUltimoMovimiento.HasValue && anioUltimoMovimiento < 1900)
                throw new ArgumentException("anioUltimoMovimiento no es válido.");
            if (fechaUltimoMovimientoDesde.HasValue && fechaUltimoMovimientoHasta.HasValue &&
            fechaUltimoMovimientoDesde.Value.Date > fechaUltimoMovimientoHasta.Value.Date)
            {
                throw new ArgumentException("fechaUltimoMovimientoDesde no puede ser mayor que fechaUltimoMovimientoHasta.");
            }

            semaforo = string.IsNullOrWhiteSpace(semaforo)
                ? null
                : semaforo.Trim().ToLowerInvariant();

            string cacheKey = BuildCacheKey(
            pageNumber,
            pageSize,
            texto,
            semaforo,
            idExpediente,
            exp1,
            exp2,
            mesUltimoMovimiento,
            anioUltimoMovimiento,
            fechaUltimoMovimientoDesde,
            fechaUltimoMovimientoHasta);

            if (_cache.TryGetValue(cacheKey, out ResultadoPaginado<AlertaCaducidadDto> resultadoCacheado))
            {
                return resultadoCacheado;
            }

            string cacheKeyResumenGlobal =
                $"alertas-caducidad-v2:resumen-global:" +
                $"diaAm={_alertasOptions.DiasInicioAmarillo}:" +
                $"diaNar={_alertasOptions.DiasInicioNaranja}:" +
                $"diaRo={_alertasOptions.DiasInicioRojo}";

            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            const string cteBase = @"
        WITH UltimasMarcasTiempo AS (
            SELECT
                m.PROC,
                MAX(m.FECH || COALESCE(m.HORA, '0000') || m.MOVI) AS MaxMarcaTiempo
            FROM MOVI m
            WHERE TRIM(COALESCE(m.FECH, '')) <> ''
              AND m.HECH = 'P'
            GROUP BY m.PROC
        ),
        DetalleUltimoMovimiento AS (
            SELECT
                m.PROC,
                CAST(COALESCE(m.DSCR, '') AS VARCHAR(500)) AS DSCR,
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
            WHERE m.HECH = 'P'
              AND TRIM(COALESCE(m.FECH, '')) <> ''
        ),
        DatosCalculados AS (
            SELECT
                p.PROC AS IdExpediente,
                COALESCE(p.ACTO, '') AS Acto,
                COALESCE(p.DEMA, '') AS Dema,
                TRIM(COALESCE(p.EXP1, '')) AS EXP1,
                TRIM(COALESCE(p.EXP2, '')) AS EXP2,
                TRIM(COALESCE(p.EXP3, '')) AS EXP3,
                TRIM(COALESCE(p.EXP4, '')) AS EXP4,
                dum.DSCR AS DescripcionUltimoEscrito,
                dum.FechaReal AS FechaUltimoMovimiento,
                DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
                DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
            FROM PROC p
            INNER JOIN DetalleUltimoMovimiento dum
                ON dum.PROC = p.PROC
            WHERE (p.GRUP IS NULL OR p.GRUP <> 'B')
              AND EXTRACT(YEAR FROM dum.FechaReal) >= 2020
              AND TRIM(COALESCE(p.EXP1, '')) <> ''
        ),
        DatosBase AS (
            SELECT
                dc.IdExpediente,
                dc.Acto,
                dc.Dema,
                dc.EXP1,
                dc.EXP2,
                dc.EXP3,
                dc.EXP4,
                dc.DescripcionUltimoEscrito,
                dc.FechaUltimoMovimiento,
                dc.DiasInactivo,
                dc.MesesInactivo,
                CASE
                    WHEN dc.DiasInactivo >= @DiasInicioRojo THEN 'rojo'
                    WHEN dc.DiasInactivo >= @DiasInicioNaranja THEN 'naranja'
                    WHEN dc.DiasInactivo >= @DiasInicioAmarillo THEN 'amarillo'
                    ELSE 'verde'
                END AS EstadoSemaforo,
                CASE
                    WHEN dc.DiasInactivo >= @DiasInicioRojo THEN CAST(@ColorRojo AS VARCHAR(20))
                    WHEN dc.DiasInactivo >= @DiasInicioNaranja THEN CAST(@ColorNaranja AS VARCHAR(20))
                    WHEN dc.DiasInactivo >= @DiasInicioAmarillo THEN CAST(@ColorAmarillo AS VARCHAR(20))
                    ELSE CAST(@ColorVerde AS VARCHAR(20))
                END AS ColorSemaforo,
                CASE
                    WHEN dc.DiasInactivo >= @DiasInicioRojo THEN 1
                    WHEN dc.DiasInactivo >= @DiasInicioNaranja THEN 2
                    WHEN dc.DiasInactivo >= @DiasInicioAmarillo THEN 3
                    ELSE 4
                END AS PrioridadSemaforo
            FROM DatosCalculados dc
        )";

            var parameters = new DynamicParameters();
            parameters.Add("StartRow", startRow, DbType.Int32);
            parameters.Add("EndRow", endRow, DbType.Int32);

            parameters.Add("DiasInicioAmarillo", _alertasOptions.DiasInicioAmarillo, DbType.Int32);
            parameters.Add("DiasInicioNaranja", _alertasOptions.DiasInicioNaranja, DbType.Int32);
            parameters.Add("DiasInicioRojo", _alertasOptions.DiasInicioRojo, DbType.Int32);

            parameters.Add("ColorVerde", _alertasOptions.ColorVerde, DbType.String);
            parameters.Add("ColorAmarillo", _alertasOptions.ColorAmarillo, DbType.String);
            parameters.Add("ColorNaranja", _alertasOptions.ColorNaranja, DbType.String);
            parameters.Add("ColorRojo", _alertasOptions.ColorRojo, DbType.String);

            var where = new StringBuilder(" WHERE 1 = 1 ");

            if (!string.IsNullOrWhiteSpace(texto))
            {
                where.Append(@"
                AND (
                       POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(CAST(db.IdExpediente AS VARCHAR(50)), '') AS VARCHAR(50)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.Acto, '') AS VARCHAR(500)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.Dema, '') AS VARCHAR(500)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.DescripcionUltimoEscrito, '') AS VARCHAR(500)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.EXP1, '') AS VARCHAR(500)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.EXP2, '') AS VARCHAR(500)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.EXP3, '') AS VARCHAR(500)))) > 0
                    OR POSITION(UPPER(@Texto) IN UPPER(CAST(COALESCE(db.EXP4, '') AS VARCHAR(500)))) > 0
                )");
                            parameters.Add("Texto", texto.Trim(), DbType.String);
            }

            if (!string.IsNullOrWhiteSpace(idExpediente))
            {
                where.Append(" AND CAST(db.IdExpediente AS VARCHAR(50)) = @IdExpediente ");
                parameters.Add("IdExpediente", idExpediente.Trim(), DbType.String);
            }

            if (!string.IsNullOrWhiteSpace(exp1))
            {
                where.Append(" AND TRIM(db.EXP1) CONTAINING @Exp1 ");
                parameters.Add("Exp1", exp1.Trim(), DbType.String);
            }

            if (!string.IsNullOrWhiteSpace(exp2))
            {
                where.Append(" AND TRIM(db.EXP2) CONTAINING @Exp2 ");
                parameters.Add("Exp2", exp2.Trim(), DbType.String);
            }

            if (mesUltimoMovimiento.HasValue)
            {
                where.Append(" AND EXTRACT(MONTH FROM db.FechaUltimoMovimiento) = @MesUltimoMovimiento ");
                parameters.Add("MesUltimoMovimiento", mesUltimoMovimiento.Value, DbType.Int32);
            }

            if (anioUltimoMovimiento.HasValue)
            {
                where.Append(" AND EXTRACT(YEAR FROM db.FechaUltimoMovimiento) = @AnioUltimoMovimiento ");
                parameters.Add("AnioUltimoMovimiento", anioUltimoMovimiento.Value, DbType.Int32);
            }

            if (!string.IsNullOrWhiteSpace(semaforo))
            {
                if (semaforo == "rojo" || semaforo == "naranja" || semaforo == "amarillo" || semaforo == "verde")
                {
                    where.Append(" AND db.EstadoSemaforo = @Semaforo ");
                    parameters.Add("Semaforo", semaforo, DbType.String);
                }
            }
            if (fechaUltimoMovimientoDesde.HasValue)
            {
                where.Append(" AND db.FechaUltimoMovimiento >= @FechaUltimoMovimientoDesde ");
                parameters.Add("FechaUltimoMovimientoDesde", fechaUltimoMovimientoDesde.Value.Date, DbType.Date);
            }

            if (fechaUltimoMovimientoHasta.HasValue)
            {
                where.Append(" AND db.FechaUltimoMovimiento <= @FechaUltimoMovimientoHasta ");
                parameters.Add("FechaUltimoMovimientoHasta", fechaUltimoMovimientoHasta.Value.Date, DbType.Date);
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
            db.EXP1,
            db.EXP2,
            db.EXP3,
            db.EXP4,
            CAST('' AS VARCHAR(500)) AS OJUD,
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
            COALESCE(SUM(CASE WHEN db.EstadoSemaforo = 'naranja' THEN 1 ELSE 0 END), 0) AS Naranjas,
            COALESCE(SUM(CASE WHEN db.EstadoSemaforo = 'amarillo' THEN 1 ELSE 0 END), 0) AS Amarillos,
            COALESCE(SUM(CASE WHEN db.EstadoSemaforo = 'verde' THEN 1 ELSE 0 END), 0) AS Verdes
        FROM DatosBase db;";

            await using var connection = new FbConnection(_connectionString);

            try
            {
                await connection.OpenAsync();

                var total = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);
                var datos = (await connection.QueryAsync<AlertaCaducidadDto>(sqlData, parameters)).ToList();

                if (datos.Count > 0)
                {
                    var ids = datos
                        .Select(x => x.IdExpediente)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();

                    if (ids.Count > 0)
                    {
                        var sqlOjudBuilder = new StringBuilder(@"
                    SELECT
                        p.PROC AS IdExpediente,
                        TRIM(COALESCE(o.NOM1, '')) AS Nom1,
                        TRIM(COALESCE(o.NOM2, '')) AS Nom2,
                        TRIM(COALESCE(o.DIRE, '')) AS Dire
                    FROM PROC p
                    LEFT JOIN OJUD o
                        ON TRIM(o.OJUD) = TRIM(p.OJUD)
                    WHERE ");

                        var ojudParams = new DynamicParameters();

                        for (int i = 0; i < ids.Count; i++)
                        {
                            if (i > 0)
                                sqlOjudBuilder.Append(" OR ");

                            sqlOjudBuilder.Append($"p.PROC = @Id{i}");
                            ojudParams.Add($"Id{i}", ids[i], DbType.String);
                        }

                        var ojudRows = (await connection.QueryAsync<OjudPartesDto>(sqlOjudBuilder.ToString(), ojudParams)).ToList();

                        var ojudMap = ojudRows
                            .GroupBy(x => x.IdExpediente)
                            .ToDictionary(
                                g => g.Key,
                                g =>
                                {
                                    var item = g.First();

                                    var partes = new[]
                                    {
                                item.Nom1?.Trim(),
                                item.Nom2?.Trim()
                                    }
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToList();

                                    var encabezado = string.Join(" ", partes);
                                    var direccion = item.Dire?.Trim();

                                    if (!string.IsNullOrWhiteSpace(encabezado) && !string.IsNullOrWhiteSpace(direccion))
                                        return $"{encabezado} - {direccion}";

                                    if (!string.IsNullOrWhiteSpace(encabezado))
                                        return encabezado;

                                    if (!string.IsNullOrWhiteSpace(direccion))
                                        return direccion;

                                    return string.Empty;
                                });

                        foreach (var item in datos)
                        {
                            if (!string.IsNullOrWhiteSpace(item.IdExpediente) &&
                                ojudMap.TryGetValue(item.IdExpediente, out var ojudTexto))
                            {
                                item.OJUD = ojudTexto;
                            }
                        }
                    }
                }

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


    }
}