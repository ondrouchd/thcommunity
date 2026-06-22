-- Row Level Security (RLS) Policies for Supabase
-- Run this after the initial schema

-- Enable RLS on all tables
ALTER TABLE teams ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE events ENABLE ROW LEVEL SECURITY;
ALTER TABLE event_responses ENABLE ROW LEVEL SECURITY;
ALTER TABLE event_waitlist ENABLE ROW LEVEL SECURITY;
ALTER TABLE messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE message_reactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE surveys ENABLE ROW LEVEL SECURITY;
ALTER TABLE survey_options ENABLE ROW LEVEL SECURITY;
ALTER TABLE survey_votes ENABLE ROW LEVEL SECURITY;
ALTER TABLE push_subscriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE join_requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE invites ENABLE ROW LEVEL SECURITY;

-- Helper function to get current user's team
CREATE OR REPLACE FUNCTION get_user_team_id()
RETURNS UUID AS $$
BEGIN
    RETURN (SELECT team_id FROM users WHERE auth_id = auth.uid()::text);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Helper function to check if user is admin/coach
CREATE OR REPLACE FUNCTION is_team_admin_or_coach(check_team_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM users 
        WHERE auth_id = auth.uid()::text 
        AND team_id = check_team_id 
        AND role IN ('Admin', 'Coach')
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Teams policies
CREATE POLICY "Users can view their team" ON teams
    FOR SELECT USING (id = get_user_team_id());

CREATE POLICY "Anyone can view team by invite code" ON teams
    FOR SELECT USING (true);

CREATE POLICY "Authenticated users can create teams" ON teams
    FOR INSERT WITH CHECK (auth.uid() IS NOT NULL);

CREATE POLICY "Admins can update their team" ON teams
    FOR UPDATE USING (is_team_admin_or_coach(id));

-- Users policies
CREATE POLICY "Users can view team members" ON users
    FOR SELECT USING (team_id = get_user_team_id() OR id = (SELECT id FROM users WHERE auth_id = auth.uid()::text));

CREATE POLICY "Users can insert their own profile" ON users
    FOR INSERT WITH CHECK (auth_id = auth.uid()::text);

CREATE POLICY "Users can update their own profile" ON users
    FOR UPDATE USING (auth_id = auth.uid()::text);

CREATE POLICY "Admins can update team members" ON users
    FOR UPDATE USING (is_team_admin_or_coach(team_id));

-- Team settings policies
CREATE POLICY "Team members can view settings" ON team_settings
    FOR SELECT USING (team_id = get_user_team_id());

CREATE POLICY "Admins can manage settings" ON team_settings
    FOR ALL USING (is_team_admin_or_coach(team_id));

-- Events policies
CREATE POLICY "Team members can view events" ON events
    FOR SELECT USING (team_id = get_user_team_id());

CREATE POLICY "Admins/Coaches can create events" ON events
    FOR INSERT WITH CHECK (is_team_admin_or_coach(team_id));

CREATE POLICY "Admins/Coaches can update events" ON events
    FOR UPDATE USING (is_team_admin_or_coach(team_id));

CREATE POLICY "Admins/Coaches can delete events" ON events
    FOR DELETE USING (is_team_admin_or_coach(team_id));

-- Event responses policies
CREATE POLICY "Team members can view responses" ON event_responses
    FOR SELECT USING (
        EXISTS (SELECT 1 FROM events WHERE events.id = event_responses.event_id AND events.team_id = get_user_team_id())
    );

CREATE POLICY "Users can manage their own responses" ON event_responses
    FOR ALL USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

-- Event waitlist policies
CREATE POLICY "Team members can view waitlist" ON event_waitlist
    FOR SELECT USING (
        EXISTS (SELECT 1 FROM events WHERE events.id = event_waitlist.event_id AND events.team_id = get_user_team_id())
    );

CREATE POLICY "Users can manage their own waitlist entry" ON event_waitlist
    FOR ALL USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

-- Messages policies
CREATE POLICY "Team members can view messages" ON messages
    FOR SELECT USING (team_id = get_user_team_id());

CREATE POLICY "Team members can send messages" ON messages
    FOR INSERT WITH CHECK (
        team_id = get_user_team_id() AND 
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

CREATE POLICY "Users can update their own messages" ON messages
    FOR UPDATE USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

CREATE POLICY "Users can delete their own messages" ON messages
    FOR DELETE USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

-- Message reactions policies
CREATE POLICY "Team members can view reactions" ON message_reactions
    FOR SELECT USING (
        EXISTS (SELECT 1 FROM messages WHERE messages.id = message_reactions.message_id AND messages.team_id = get_user_team_id())
    );

CREATE POLICY "Users can manage their own reactions" ON message_reactions
    FOR ALL USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

-- Surveys policies
CREATE POLICY "Team members can view surveys" ON surveys
    FOR SELECT USING (team_id = get_user_team_id());

CREATE POLICY "Admins/Coaches can manage surveys" ON surveys
    FOR ALL USING (is_team_admin_or_coach(team_id));

-- Survey options policies
CREATE POLICY "Team members can view survey options" ON survey_options
    FOR SELECT USING (
        EXISTS (SELECT 1 FROM surveys WHERE surveys.id = survey_options.survey_id AND surveys.team_id = get_user_team_id())
    );

CREATE POLICY "Admins/Coaches can manage survey options" ON survey_options
    FOR ALL USING (
        EXISTS (SELECT 1 FROM surveys WHERE surveys.id = survey_options.survey_id AND is_team_admin_or_coach(surveys.team_id))
    );

-- Survey votes policies
CREATE POLICY "Team members can view non-anonymous votes" ON survey_votes
    FOR SELECT USING (
        EXISTS (
            SELECT 1 FROM surveys 
            WHERE surveys.id = survey_votes.survey_id 
            AND surveys.team_id = get_user_team_id()
            AND surveys.is_anonymous = FALSE
        )
    );

CREATE POLICY "Users can view their own votes" ON survey_votes
    FOR SELECT USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

CREATE POLICY "Users can manage their own votes" ON survey_votes
    FOR ALL USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

-- Push subscriptions policies
CREATE POLICY "Users can manage their own subscriptions" ON push_subscriptions
    FOR ALL USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

-- Join requests policies
CREATE POLICY "Users can view their own requests" ON join_requests
    FOR SELECT USING (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

CREATE POLICY "Admins/Coaches can view team requests" ON join_requests
    FOR SELECT USING (is_team_admin_or_coach(team_id));

CREATE POLICY "Users can create join requests" ON join_requests
    FOR INSERT WITH CHECK (
        user_id = (SELECT id FROM users WHERE auth_id = auth.uid()::text)
    );

CREATE POLICY "Admins/Coaches can update join requests" ON join_requests
    FOR UPDATE USING (is_team_admin_or_coach(team_id));

-- Invites policies
CREATE POLICY "Anyone can view invites by code" ON invites
    FOR SELECT USING (true);

CREATE POLICY "Admins/Coaches can manage invites" ON invites
    FOR ALL USING (is_team_admin_or_coach(team_id));
