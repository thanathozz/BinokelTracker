-- Add individual columns to games (replacing the JSON blob)
ALTER TABLE games
    ADD COLUMN IF NOT EXISTS game_date     BIGINT   NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS spielrunde_id BIGINT,
    ADD COLUMN IF NOT EXISTS einsatz       TEXT     NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS finished      BOOLEAN  NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS quick_winner  INT,
    ADD COLUMN IF NOT EXISTS rules         JSONB    NOT NULL DEFAULT '{}';

-- Drop the JSON blob (existing data discarded per user request)
ALTER TABLE games DROP COLUMN IF EXISTS data;
