-- THcommunity – bootstrap for a vanilla managed PostgreSQL
-- (Azure Database for PostgreSQL, AWS RDS / Aurora PostgreSQL).
--
-- The supabase/postgres image ships the roles, the `auth` schema, the
-- `auth.uid()` helpers and the `supabase_realtime` publication out of the box.
-- A plain managed PostgreSQL does not, so run THIS file ONCE before
-- 001_initial_schema.sql when you host the database on Azure or AWS instead of
-- Supabase. It is idempotent and safe to re-run.
--
-- Requires a login role allowed to create roles and grant BYPASSRLS
-- (rds_superuser on AWS RDS, azure_pg_admin on Azure Flexible Server).
--
-- Two role passwords can be supplied with psql -v, otherwise they default to
-- the role name (CHANGE THEM for anything but local testing):
--   psql -v authenticator_password=... -v auth_admin_password=... -f 000_cloud_bootstrap.sql
\set authenticator_password :authenticator_password
\set auth_admin_password :auth_admin_password

-- Extensions used by the schema and by auth.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Supabase-compatible roles used by PostgREST and GoTrue.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'anon') THEN
        CREATE ROLE anon NOLOGIN NOINHERIT;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'authenticated') THEN
        CREATE ROLE authenticated NOLOGIN NOINHERIT;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'service_role') THEN
        CREATE ROLE service_role NOLOGIN NOINHERIT BYPASSRLS;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'authenticator') THEN
        CREATE ROLE authenticator NOINHERIT LOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'supabase_auth_admin') THEN
        CREATE ROLE supabase_auth_admin NOINHERIT LOGIN CREATEROLE;
    END IF;
END
$$;

-- service_role must bypass RLS so the backend (which uses the service role key)
-- is not blocked by the row-level security policies in 002_rls_policies.sql.
ALTER ROLE service_role BYPASSRLS;

-- Connection passwords (psql variables, default to the role name).
ALTER ROLE authenticator LOGIN PASSWORD :'authenticator_password';
ALTER ROLE supabase_auth_admin LOGIN PASSWORD :'auth_admin_password';

-- PostgREST switches from authenticator into the request role.
GRANT anon, authenticated, service_role TO authenticator;

-- The auth schema is owned and managed by GoTrue.
CREATE SCHEMA IF NOT EXISTS auth AUTHORIZATION supabase_auth_admin;
GRANT USAGE ON SCHEMA auth TO anon, authenticated, service_role;

-- JWT claim helpers used by the RLS policies. They read PostgREST's GUCs and
-- work whether or not legacy per-claim GUCs are enabled.
CREATE OR REPLACE FUNCTION auth.jwt() RETURNS jsonb
    LANGUAGE sql STABLE AS $$
    SELECT nullif(current_setting('request.jwt.claims', true), '')::jsonb
$$;

CREATE OR REPLACE FUNCTION auth.uid() RETURNS uuid
    LANGUAGE sql STABLE AS $$
    SELECT coalesce(
        nullif(current_setting('request.jwt.claim.sub', true), ''),
        (nullif(current_setting('request.jwt.claims', true), '')::jsonb ->> 'sub')
    )::uuid
$$;

CREATE OR REPLACE FUNCTION auth.role() RETURNS text
    LANGUAGE sql STABLE AS $$
    SELECT coalesce(
        nullif(current_setting('request.jwt.claim.role', true), ''),
        (nullif(current_setting('request.jwt.claims', true), '')::jsonb ->> 'role')
    )::text
$$;

CREATE OR REPLACE FUNCTION auth.email() RETURNS text
    LANGUAGE sql STABLE AS $$
    SELECT coalesce(
        nullif(current_setting('request.jwt.claim.email', true), ''),
        (nullif(current_setting('request.jwt.claims', true), '')::jsonb ->> 'email')
    )::text
$$;

-- Grants so the API roles can use objects created by the later migrations.
GRANT USAGE ON SCHEMA public TO anon, authenticated, service_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO service_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO service_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO service_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO authenticated;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO authenticated;

-- Realtime publication referenced by 003_realtime.sql (created empty here).
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_publication WHERE pubname = 'supabase_realtime') THEN
        CREATE PUBLICATION supabase_realtime;
    END IF;
END
$$;
