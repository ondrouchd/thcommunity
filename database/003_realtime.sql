-- Enable Supabase Realtime for specific tables
-- Run this after the initial schema

-- Enable realtime for messages (chat)
ALTER PUBLICATION supabase_realtime ADD TABLE messages;

-- Enable realtime for message reactions
ALTER PUBLICATION supabase_realtime ADD TABLE message_reactions;

-- Enable realtime for event responses
ALTER PUBLICATION supabase_realtime ADD TABLE event_responses;

-- Enable realtime for events
ALTER PUBLICATION supabase_realtime ADD TABLE events;

-- Enable realtime for surveys (for live voting)
ALTER PUBLICATION supabase_realtime ADD TABLE survey_votes;
