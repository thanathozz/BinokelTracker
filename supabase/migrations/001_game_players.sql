CREATE TABLE game_players (
    game_id          BIGINT  NOT NULL REFERENCES games(id) ON DELETE CASCADE,
    position         INT     NOT NULL,
    display_name     TEXT    NOT NULL,
    player_user_id   TEXT,                 -- UserId des Spielers, null = Gast
    user_id          TEXT    NOT NULL,     -- Account-Besitzer (für RLS)
    PRIMARY KEY (game_id, position)
);

ALTER TABLE game_players ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Eigene Daten" ON game_players
    USING  (user_id = auth.uid()::text)
    WITH CHECK (user_id = auth.uid()::text);
