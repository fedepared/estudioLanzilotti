WITH UltimasMarcasTiempo AS (
    -- Busco la marca de tiempo alfabética más alta (YYYYMMDD + HHMM) por expediente
    -- COALESCE por si algún movimiento no tiene hora registrada
    SELECT 
        PROC, 
        MAX(FECH || COALESCE(HORA, '0000')) AS MaxMarcaTiempo
    FROM MOVI
    WHERE FECH IS NOT NULL AND TRIM(FECH) <> ''
    GROUP BY PROC
),
DetalleUltimoMovimiento AS (
    -- Recupero el registro exacto (con su descripción) haciendo JOIN con la marca de tiempo
    SELECT 
        m.PROC,
        m.DSCR,
        -- Convierto el CHAR(8) a DATE nativo
        CAST(
            SUBSTRING(m.FECH FROM 1 FOR 4) || '-' || 
            SUBSTRING(m.FECH FROM 5 FOR 2) || '-' || 
            SUBSTRING(m.FECH FROM 7 FOR 2) 
        AS DATE) AS FechaReal
    FROM MOVI m
    INNER JOIN UltimasMarcasTiempo umt 
        ON m.PROC = umt.PROC AND (m.FECH || COALESCE(m.HORA, '0000')) = umt.MaxMarcaTiempo
)
-- Cruzo con los expedientes y calculo el vencimiento
SELECT 
    p.PROC AS IdExpediente,
    p.ACTO AS Caratula, 
    dum.DSCR AS DescripcionUltimoEscrito,
    dum.FechaReal AS FechaUltimoMovimiento,
    DATEDIFF(DAY FROM dum.FechaReal TO CURRENT_DATE) AS DiasInactivo,
    DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) AS MesesInactivo
FROM PROC p
INNER JOIN DetalleUltimoMovimiento dum ON dum.PROC = p.PROC
-- WHERE DATEDIFF(MONTH FROM dum.FechaReal TO CURRENT_DATE) >= 4 
ORDER BY DiasInactivo;