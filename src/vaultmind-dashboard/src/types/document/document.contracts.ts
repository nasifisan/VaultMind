export interface DocumentRecord {
  Id: string;
  ConversationId: string;
  UserId: string;
  FileName: string;
  StorageUrl: string;
  ContentType: string;
  Size: number;
  UploadedAt: string;
}

// Optimistic UI state for in-progress uploads
export interface PendingDocument {
  tempId: string;
  fileName: string;
  size: number;
  contentType: string;
  status: 'uploading' | 'error';
}
