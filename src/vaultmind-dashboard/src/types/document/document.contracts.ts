export interface DocumentRecord {
  id: string;
  conversationId: string;
  userId: string;
  fileName: string;
  storageUrl: string;
  contentType: string;
  size: number;
  uploadedAt: string;
}

// Optimistic UI state for in-progress uploads
export interface PendingDocument {
  tempId: string;
  fileName: string;
  size: number;
  contentType: string;
  status: 'uploading' | 'error';
}
