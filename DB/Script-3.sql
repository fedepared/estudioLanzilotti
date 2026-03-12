WITH MovimientosBrutos AS (
    -- uso CASE para proteger el CAST
    SELECT 
        PROC,
        DSCR,
        FECH,
        HORA,
        CASE 
            -- Solo convierto si tiene exactamente 8 caracteres (ej: '20231025')
            WHEN FECH IS NOT NULL AND CHAR_LENGTH(TRIM(FECH)) = 8 THEN
                CAST(
                    SUBSTRING(FECH FROM 1 FOR 4) || '-' || 
                    SUBSTRING(FECH FROM 5 FOR 2) || '-' || 
                    SUBSTRING(FECH FROM 7 FOR 2) 
                AS DATE)
            ELSE NULL
        END AS FechaReal
    FROM MOVI WHERE HECH LIKE 'P'
),
MovimientosHistoricos AS (
    -- Filtro los nulos y los movimientos del futuro (agendas/errores)
    SELECT * FROM MovimientosBrutos
    WHERE FechaReal IS NOT NULL AND FechaReal <= CURRENT_DATE
),
UltimasMarcasTiempo AS (
    -- Busco el último movimiento histórico válido
    SELECT 
        PROC, 
        MAX(FECH || COALESCE(HORA, '0000')) AS MaxMarcaTiempo
    FROM MovimientosHistoricos
    GROUP BY PROC
),
DetalleUltimoMovimiento AS (
    -- agarro la descripción de ese último movimiento
    SELECT 
        mh.PROC,
        mh.DSCR,
        mh.FechaReal
    FROM MovimientosHistoricos mh
    INNER JOIN UltimasMarcasTiempo umt 
        ON mh.PROC = umt.PROC AND (mh.FECH || COALESCE(mh.HORA, '0000')) = umt.MaxMarcaTiempo
)
-- veo con expedientes y calculo
SELECT 
    p.PROC AS IdExpediente,
    p.ACTO AS Acto,
    p.DEMA AS Dema,
    dum.DSCR AS DescripcionUltimoEscrito,
    dum.FechaReal AS FechaUltimoMovimiento,
    DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
    DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
FROM PROC p
INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
ORDER BY DiasInactivo ASC;