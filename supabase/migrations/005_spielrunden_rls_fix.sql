-- Ensure spielrunden has a user_id column (owner column, like the other tables)
ALTER TABLE spielrunden ADD COLUMN IF NOT EXISTS user_id TEXT;

-- Backfill user_id from the data JSON for rows that were saved before user_id existed
UPDATE spielrunden
SET user_id = (data->>'creatorUserId')::uuid
WHERE user_id IS NULL AND data->>'creatorUserId' IS NOT NULL;

-- Drop whatever policy exists (name may vary depending on how the table was originally set up)
DO $$
DECLARE pol record;
BEGIN
    FOR pol IN SELECT policyname FROM pg_policies WHERE tablename = 'spielrunden' AND schemaname = 'public' LOOP
        EXECUTE format('DROP POLICY IF EXISTS %I ON spielrunden', pol.policyname);
    END LOOP;
END $$;

-- Recreate with explicit WITH CHECK (user_id is uuid, so compare directly with auth.uid())
CREATE POLICY "Eigene Daten" ON spielrunden
    USING  (user_id = auth.uid())
    WITH CHECK (user_id = auth.uid());
