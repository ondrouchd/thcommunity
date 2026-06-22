export type Json =
  | string
  | number
  | boolean
  | null
  | { [key: string]: Json | undefined }
  | Json[]

export interface Database {
  public: {
    Tables: {
      teams: {
        Row: {
          id: string
          name: string
          description: string | null
          logo_url: string | null
          invite_code: string
          created_at: string
          updated_at: string
        }
        Insert: {
          name: string
          description?: string | null
          logo_url?: string | null
          invite_code?: string
        }
        Update: {
          name?: string
          description?: string | null
          logo_url?: string | null
          invite_code?: string
        }
        Relationships: []
      }
      users: {
        Row: {
          id: string
          auth_id: string
          team_id: string | null
          email: string
          phone: string
          display_name: string
          avatar_url: string | null
          role: 'Admin' | 'Coach' | 'Player'
          position: 'Player' | 'Goalie'
          created_at: string
          updated_at: string
        }
        Insert: {
          auth_id: string
          email: string
          phone: string
          display_name: string
          team_id?: string | null
          avatar_url?: string | null
          role?: 'Admin' | 'Coach' | 'Player'
          position?: 'Player' | 'Goalie'
        }
        Update: {
          auth_id?: string
          email?: string
          phone?: string
          display_name?: string
          team_id?: string | null
          avatar_url?: string | null
          role?: 'Admin' | 'Coach' | 'Player'
          position?: 'Player' | 'Goalie'
        }
        Relationships: [
          {
            foreignKeyName: "users_team_id_fkey"
            columns: ["team_id"]
            referencedRelation: "teams"
            referencedColumns: ["id"]
          }
        ]
      }
      events: {
        Row: {
          id: string
          team_id: string
          created_by_user_id: string
          title: string
          description: string | null
          event_type: 'Training' | 'Match'
          start_time: string
          end_time: string
          location: string | null
          capacity_players: number
          capacity_goalies: number
          response_deadline: string | null
          price_override: number | null
          created_at: string
          updated_at: string
        }
        Insert: {
          team_id: string
          created_by_user_id: string
          title: string
          description?: string | null
          event_type: 'Training' | 'Match'
          start_time: string
          end_time: string
          location?: string | null
          capacity_players?: number
          capacity_goalies?: number
          response_deadline?: string | null
          price_override?: number | null
        }
        Update: {
          team_id?: string
          created_by_user_id?: string
          title?: string
          description?: string | null
          event_type?: 'Training' | 'Match'
          start_time?: string
          end_time?: string
          location?: string | null
          capacity_players?: number
          capacity_goalies?: number
          response_deadline?: string | null
          price_override?: number | null
        }
        Relationships: [
          {
            foreignKeyName: "events_team_id_fkey"
            columns: ["team_id"]
            referencedRelation: "teams"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "events_created_by_user_id_fkey"
            columns: ["created_by_user_id"]
            referencedRelation: "users"
            referencedColumns: ["id"]
          }
        ]
      }
      event_responses: {
        Row: {
          id: string
          event_id: string
          user_id: string
          response: 'Player' | 'Goalie' | 'Cannot' | 'Maybe'
          note: string | null
          responded_at: string
          updated_at: string
        }
        Insert: {
          event_id: string
          user_id: string
          response: 'Player' | 'Goalie' | 'Cannot' | 'Maybe'
          note?: string | null
        }
        Update: {
          event_id?: string
          user_id?: string
          response?: 'Player' | 'Goalie' | 'Cannot' | 'Maybe'
          note?: string | null
        }
        Relationships: [
          {
            foreignKeyName: "event_responses_event_id_fkey"
            columns: ["event_id"]
            referencedRelation: "events"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "event_responses_user_id_fkey"
            columns: ["user_id"]
            referencedRelation: "users"
            referencedColumns: ["id"]
          }
        ]
      }
      messages: {
        Row: {
          id: string
          team_id: string
          user_id: string
          content: string
          type: 'Text' | 'Image' | 'Video'
          media_url: string | null
          media_type: string | null
          reply_to_id: string | null
          created_at: string
          edited_at: string | null
          is_deleted: boolean
        }
        Insert: {
          team_id: string
          user_id: string
          content: string
          type?: 'Text' | 'Image' | 'Video'
          media_url?: string | null
          media_type?: string | null
          reply_to_id?: string | null
          is_deleted?: boolean
        }
        Update: {
          team_id?: string
          user_id?: string
          content?: string
          type?: 'Text' | 'Image' | 'Video'
          media_url?: string | null
          media_type?: string | null
          reply_to_id?: string | null
          is_deleted?: boolean
          edited_at?: string | null
        }
        Relationships: [
          {
            foreignKeyName: "messages_team_id_fkey"
            columns: ["team_id"]
            referencedRelation: "teams"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "messages_user_id_fkey"
            columns: ["user_id"]
            referencedRelation: "users"
            referencedColumns: ["id"]
          }
        ]
      }
      surveys: {
        Row: {
          id: string
          team_id: string
          created_by_user_id: string
          question: string
          allow_multiple_answers: boolean
          is_anonymous: boolean
          expires_at: string | null
          created_at: string
          is_closed: boolean
        }
        Insert: {
          team_id: string
          created_by_user_id: string
          question: string
          allow_multiple_answers?: boolean
          is_anonymous?: boolean
          expires_at?: string | null
          is_closed?: boolean
        }
        Update: {
          team_id?: string
          created_by_user_id?: string
          question?: string
          allow_multiple_answers?: boolean
          is_anonymous?: boolean
          expires_at?: string | null
          is_closed?: boolean
        }
        Relationships: [
          {
            foreignKeyName: "surveys_team_id_fkey"
            columns: ["team_id"]
            referencedRelation: "teams"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "surveys_created_by_user_id_fkey"
            columns: ["created_by_user_id"]
            referencedRelation: "users"
            referencedColumns: ["id"]
          }
        ]
      }
      survey_options: {
        Row: {
          id: string
          survey_id: string
          text: string
          display_order: number
        }
        Insert: {
          survey_id: string
          text: string
          display_order?: number
        }
        Update: {
          survey_id?: string
          text?: string
          display_order?: number
        }
        Relationships: [
          {
            foreignKeyName: "survey_options_survey_id_fkey"
            columns: ["survey_id"]
            referencedRelation: "surveys"
            referencedColumns: ["id"]
          }
        ]
      }
      survey_votes: {
        Row: {
          id: string
          option_id: string
          user_id: string
          voted_at: string
        }
        Insert: {
          option_id: string
          user_id: string
        }
        Update: {
          option_id?: string
          user_id?: string
        }
        Relationships: [
          {
            foreignKeyName: "survey_votes_option_id_fkey"
            columns: ["option_id"]
            referencedRelation: "survey_options"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "survey_votes_user_id_fkey"
            columns: ["user_id"]
            referencedRelation: "users"
            referencedColumns: ["id"]
          }
        ]
      }
      push_subscriptions: {
        Row: {
          id: string
          user_id: string
          endpoint: string
          p256dh: string
          auth: string
          created_at: string
        }
        Insert: {
          user_id: string
          endpoint: string
          p256dh: string
          auth: string
        }
        Update: {
          user_id?: string
          endpoint?: string
          p256dh?: string
          auth?: string
        }
        Relationships: [
          {
            foreignKeyName: "push_subscriptions_user_id_fkey"
            columns: ["user_id"]
            referencedRelation: "users"
            referencedColumns: ["id"]
          }
        ]
      }
    }
    Views: {
      [_ in never]: never
    }
    Functions: {
      [_ in never]: never
    }
    Enums: {
      [_ in never]: never
    }
    CompositeTypes: {
      [_ in never]: never
    }
  }
}

export type User = Database['public']['Tables']['users']['Row']
export type Team = Database['public']['Tables']['teams']['Row']
export type Event = Database['public']['Tables']['events']['Row']
export type EventResponse = Database['public']['Tables']['event_responses']['Row']
export type Message = Database['public']['Tables']['messages']['Row']
export type Survey = Database['public']['Tables']['surveys']['Row']
export type SurveyOption = Database['public']['Tables']['survey_options']['Row']
export type SurveyVote = Database['public']['Tables']['survey_votes']['Row']
export type PushSubscription = Database['public']['Tables']['push_subscriptions']['Row']
