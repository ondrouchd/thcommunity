-- Security fixes for Supabase Security Advisor warnings
-- Run this in Supabase SQL Editor

-- Fix: Function Search Path Mutable for update_updated_at_column
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql
SET search_path = public;

-- Fix: Function Search Path Mutable for get_user_team_id
CREATE OR REPLACE FUNCTION get_user_team_id()
RETURNS UUID AS $$
BEGIN
    RETURN (SELECT team_id FROM public.users WHERE auth_id = auth.uid()::text);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public;

-- Fix: Function Search Path Mutable for is_team_admin_or_coach
CREATE OR REPLACE FUNCTION is_team_admin_or_coach(check_team_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM public.users 
        WHERE auth_id = auth.uid()::text 
        AND team_id = check_team_id 
        AND role IN ('Admin', 'Coach')
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public;
