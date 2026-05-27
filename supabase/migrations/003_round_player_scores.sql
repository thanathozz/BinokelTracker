CREATE TABLE round_player_scores (
    game_id         BIGINT   NOT NULL,
    round_position  INT      NOT NULL,
    player_position INT      NOT NULL,
    user_id         TEXT     NOT NULL,
    meld            INT      NOT NULL DEFAULT 0,
    tricks          INT      NOT NULL DEFAULT 0,
    abgegangen      BOOLEAN  NOT NULL DEFAULT FALSE,
    PRIMARY KEY (game_id, round_position, player_position),
    FOREIGN KEY (game_id, round_position) REFERENCES rounds(game_id, position) ON DELETE CASCADE
);

ALTER TABLE round_player_scores ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Eigene Daten" ON round_player_scores
    USING  (user_id = auth.uid()::text)
    WITH CHECK (user_id = auth.uid()::text);
