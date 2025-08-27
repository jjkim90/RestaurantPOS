# Database Migrations

## How to Apply Migrations

Since this project uses WPF and requires Windows for proper EF Core migrations, follow these steps:

### Option 1: Using EF Core CLI (Recommended on Windows)

1. Open a command prompt or PowerShell in the solution directory
2. Ensure you have the EF Core tools installed:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

3. Create the migration:
   ```bash
   dotnet ef migrations add AddNewOrderDetailProperties -p RestaurantPOS.Data -s RestaurantPOS.WPF
   ```

4. Update the database:
   ```bash
   dotnet ef database update -p RestaurantPOS.Data -s RestaurantPOS.WPF
   ```

### Option 2: Using SQL Scripts (Manual)

1. Open SQL Server Management Studio
2. Connect to your database (.\SQLEXPRESS, Database: RestaurantPOS)
3. Execute the migration script: `AddNewOrderDetailProperties.sql`

### Migration History

| Migration | Date | Description |
|-----------|------|-------------|
| AddNewOrderDetailProperties | 2025-07-24 | Add IsNewItem, Status, and ConfirmedAt columns to OrderDetails table |

### Rollback Instructions

If you need to rollback a migration:
- For EF Core: `dotnet ef database update <previous-migration-name> -p RestaurantPOS.Data -s RestaurantPOS.WPF`
- For SQL: Execute the corresponding `_Rollback.sql` file