use BlogDb
go
CREATE PROCEDURE GetNewUsersLast7Days
AS
BEGIN
    SELECT 
        CAST(CreatedAt AS DATE) AS UserDate,
        COUNT(*) AS TotalUsers
    FROM AspNetUsers
    WHERE CreatedAt >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY UserDate
END

