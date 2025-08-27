-- Migration: AddNewOrderDetailProperties
-- Date: 2025-07-24
-- Description: Add IsNewItem, Status, and ConfirmedAt columns to OrderDetails table

-- Add IsNewItem column with default value false
ALTER TABLE [OrderDetails]
ADD [IsNewItem] bit NOT NULL DEFAULT 0;

-- Add Status column with default value 'Pending'
ALTER TABLE [OrderDetails]
ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Pending';

-- Add ConfirmedAt column (nullable)
ALTER TABLE [OrderDetails]
ADD [ConfirmedAt] datetime2 NULL;

-- Add check constraint for Status values
ALTER TABLE [OrderDetails]
ADD CONSTRAINT [CK_OrderDetails_Status] 
CHECK ([Status] IN (N'Pending', N'Confirmed', N'Cancelled'));

GO