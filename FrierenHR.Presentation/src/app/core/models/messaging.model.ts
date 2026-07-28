import { ConversationType } from './enums.model';

export interface ConversationDto {
  id: string; type: ConversationType; name?: string; lastMessagePreview?: string; lastMessageAt?: string; unreadCount: number;
}
export interface MessageDto {
  id: string; conversationId: string; senderEmployeeId: string; senderName: string; body: string; sentAt: string;
  attachmentUrl?: string; attachmentFileName?: string; attachmentContentType?: string; attachmentSizeBytes?: number;
}
export interface CreateDirectConversationDto { employeeAId: string; employeeBId: string; }
export interface CreateGroupConversationDto { companyId: string; name: string; memberEmployeeIds: string[]; }
export interface SendMessageDto { conversationId: string; senderEmployeeId: string; body: string; }
export interface AttachmentUploadResultDto { url: string; fileName: string; contentType: string; sizeBytes: number; }
