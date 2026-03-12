


WITH MovimientosConvertidos AS (
    -- Se convierte el char a yyyy-mm-dd
    SELECT 
        PROC,
        -- los extraigo  Año, Mes y Día y los concateno con guiones: YYYY-MM-DD
        CAST(
            SUBSTRING(FECH FROM 1 FOR 4) || '-' || 
            SUBSTRING(FECH FROM 5 FOR 2) || '-' || 
            SUBSTRING(FECH FROM 7 FOR 2) 
        AS DATE) AS FechaReal
    FROM MOVI
    WHERE FECH IS NOT NULL AND TRIM(FECH) <> '' -- Evitar vacíos
),
UltimosMovimientos AS (
    -- saco la fecha máxima por expediente
    SELECT 
        PROC, 
        MAX(FechaReal) AS MaxFecha
    FROM MovimientosConvertidos
    GROUP BY PROC
)
-- calculo el vencimiento
SELECT 
    p.PROC AS IdExpediente,
    p.ACTO AS Caratula,
    um.MaxFecha AS FechaUltimoMovimiento,
    um.DSCR AS descripcion,
    DATEDIFF(DAY FROM um.MaxFecha TO CURRENT_DATE) AS DiasInactivo,
    DATEDIFF(MONTH FROM um.MaxFecha TO CURRENT_DATE) AS MesesInactivo
FROM PROC p
INNER JOIN UltimosMovimientos um ON um.PROC = p.PROC
-- se muestran por ejemplo los de 4 meses
WHERE DATEDIFF(MONTH FROM um.MaxFecha TO CURRENT_DATE) >= 4 
ORDER BY DiasInactivo DESC;
