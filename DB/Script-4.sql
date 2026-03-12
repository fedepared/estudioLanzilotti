WITH MovimientosBrutos AS (
    -- 1. Extraemos solo movimientos válidos Y QUE TENGAN UN DOCUMENTO ADJUNTO
    SELECT 
        PROC,
        DSCR,
        FECH,
        HORA,
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
      AND TEXT IS NOT NULL -- EL FILTRO CLAVE: Debe tener un documento (ícono en pantalla)
),
MovimientosHistoricos AS (
    SELECT * FROM MovimientosBrutos
    WHERE FechaReal IS NOT NULL AND FechaReal <= CURRENT_DATE
),
UltimasMarcasTiempo AS (
    SELECT 
        PROC, 
        MAX(FECH || COALESCE(HORA, '0000')) AS MaxMarcaTiempo
    FROM MovimientosHistoricos
    GROUP BY PROC
),
DetalleUltimoMovimiento AS (
    SELECT 
        mh.PROC,
        mh.DSCR,
        mh.FechaReal
    FROM MovimientosHistoricos mh
    INNER JOIN UltimasMarcasTiempo umt 
        ON mh.PROC = umt.PROC AND (mh.FECH || COALESCE(mh.HORA, '0000')) = umt.MaxMarcaTiempo
)
SELECT 
    p.PROC AS IdExpediente,
    p.ACTO AS ACTO, 
    p.DEMA AS DEMA,
    dum.DSCR AS DescripcionUltimoEscrito,
    dum.FechaReal AS FechaUltimoMovimiento,
    DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
    DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
FROM PROC p
INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
ORDER BY DiasInactivo;