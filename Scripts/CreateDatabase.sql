-- RestaurantPOS 데이터베이스 생성 스크립트
-- SQL Server 2022 Express Edition

-- 데이터베이스 생성
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RestaurantPOS')
BEGIN
    CREATE DATABASE RestaurantPOS
    ON PRIMARY 
    (
        NAME = N'RestaurantPOS_Data',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\RestaurantPOS.mdf',
        SIZE = 100MB,
        MAXSIZE = 10GB,
        FILEGROWTH = 10MB
    )
    LOG ON 
    (
        NAME = N'RestaurantPOS_Log',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\RestaurantPOS_log.ldf',
        SIZE = 25MB,
        MAXSIZE = 2GB,
        FILEGROWTH = 10MB
    );
END
GO

-- 데이터베이스 사용
USE RestaurantPOS;
GO

-- 애플리케이션 로그인 및 사용자 생성
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'RestaurantPOSApp')
BEGIN
    CREATE LOGIN [RestaurantPOSApp] WITH PASSWORD = 'RestPOS@2024!Strong';
END
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'RestaurantPOSApp')
BEGIN
    CREATE USER [RestaurantPOSApp] FOR LOGIN [RestaurantPOSApp];
    
    -- 권한 부여
    ALTER ROLE db_datareader ADD MEMBER [RestaurantPOSApp];
    ALTER ROLE db_datawriter ADD MEMBER [RestaurantPOSApp];
    GRANT EXECUTE ON SCHEMA::dbo TO [RestaurantPOSApp];
END
GO

PRINT 'RestaurantPOS 데이터베이스가 성공적으로 생성되었습니다.';