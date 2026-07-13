/**
 * Interface matching the backend conversation header.
 */
export interface ConversationHeader {
  Id: string;
  Title: string;
  CreatedAt: string;
  UpdatedAt: string;
}

export enum ConversationRole {
  User = "User",
  Assistant = "Assistant",
}
export interface Message {
  Role: ConversationRole;
  Content: string;
  Timestamp: string;
}

/**
 * Interface matching the backend conversation detail.
 */
export interface Conversation {
  Id: string;
  UserId: string;
  Title: string;
  Messages: Message[];
  CreatedAt: string;
  UpdatedAt: string;
}
