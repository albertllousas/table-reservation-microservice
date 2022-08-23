create table restaurant_table(
  table_id UUID PRIMARY KEY,
  restaurant_id UUID NOT NULL,
  capacity integer NOT NULL,
  table_date TIMESTAMP WITH TIME ZONE NOT NULL,
  daily_schedule JSONB NOT NULL DEFAULT '{}'::JSONB
);

