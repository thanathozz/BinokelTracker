CREATE TABLE rounds (
    game_id           BIGINT   NOT NULL REFERENCES games(id) ON DELETE CASCADE,
    position          INT      NOT NULL,
    user_id           TEXT     NOT NULL,
    type              TEXT     NOT NULL DEFAULT 'Normal',
    bidder_position   INT      NOT NULL DEFAULT 0,
    bid               INT      NOT NULL DEFAULT 0,
    won               BOOLEAN  NOT NULL DEFAULT TRUE,
    trumpf            TEXT,
    last_trick_winner INT      NOT NULL DEFAULT -1,
    custom_values     JSONB    NOT NULL DEFAULT '{}',
    PRIMARY KEY (game_id, position)
);

ALTER TABLE rounds ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Eigene Daten" ON rounds
    USING  (user_id = auth.uid()::text)
    WITH CHECK (user_id = auth.uid()::text);
