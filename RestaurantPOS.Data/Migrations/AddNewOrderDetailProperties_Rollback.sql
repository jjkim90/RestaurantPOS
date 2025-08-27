-- Rollback Migration: AddNewOrderDetailProperties
-- Date: 2025-07-24
-- Description: Remove IsNewItem, Status, and ConfirmedAt columns from OrderDetails table

-- Drop check constraint
ALTER TABLE [OrderDetails]
DROP CONSTRAINT [CK_OrderDetails_Status];

-- Drop columns
ALTER TABLE [OrderDetails]
DROP COLUMN [IsNewItem];

ALTER TABLE [OrderDetails]
DROP COLUMN [Status];

ALTER TABLE [OrderDetails]
DROP COLUMN [ConfirmedAt];

GO