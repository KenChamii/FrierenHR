import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import {
  ConversationDto, MessageDto, CreateDirectConversationDto, CreateGroupConversationDto, AttachmentUploadResultDto,
} from '../models/messaging.model';

// Kept in sync with Messaging:MaxAttachmentSizeBytes in the backend's appsettings.json —
// this is just for instant client-side feedback; the server enforces the real limit.
export const MAX_ATTACHMENT_SIZE_BYTES = 10 * 1024 * 1024; // 10MB

@Injectable({ providedIn: 'root' })
export class MessagingService {
  private readonly baseUrl = `${environment.apiUrl}/api/messaging`;
  private hubConnection: signalR.HubConnection | null = null;

  readonly liveMessages = signal<MessageDto[]>([]);
  readonly connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);

  constructor(private http: HttpClient, private authService: AuthService) {}

  getConversations(employeeId: string) { return this.http.get<ConversationDto[]>(`${this.baseUrl}/conversations/${employeeId}`); }
  getHistory(conversationId: string, skip = 0, take = 50) {
    return this.http.get<MessageDto[]>(`${this.baseUrl}/conversations/${conversationId}/history`, { params: { skip, take } });
  }
  getOrCreateDirect(dto: CreateDirectConversationDto) { return this.http.post<ConversationDto>(`${this.baseUrl}/conversations/direct`, dto); }
  createGroup(dto: CreateGroupConversationDto) { return this.http.post<ConversationDto>(`${this.baseUrl}/conversations/group`, dto); }

  // Uploads the file over plain HTTP (not the hub — SignalR isn't built for binary payloads
  // like this) and gets back the stored URL/metadata to hand to sendMessage().
  uploadAttachment(file: File) {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<AttachmentUploadResultDto>(`${this.baseUrl}/attachments`, form);
  }

  async connect(): Promise<void> {
    if (this.hubConnection) return;
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.authService.token() ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (msg: MessageDto) => this.liveMessages.update(list => [...list, msg]));
    this.hubConnection.onreconnected(() => this.connectionState.set(signalR.HubConnectionState.Connected));
    this.hubConnection.onreconnecting(() => this.connectionState.set(signalR.HubConnectionState.Reconnecting));
    this.hubConnection.onclose(() => this.connectionState.set(signalR.HubConnectionState.Disconnected));

    await this.hubConnection.start();
    this.connectionState.set(signalR.HubConnectionState.Connected);
  }

  async joinConversation(conversationId: string): Promise<void> {
    this.liveMessages.set([]);
    await this.hubConnection?.invoke('JoinConversation', conversationId);
  }

  async leaveConversation(conversationId: string): Promise<void> {
    await this.hubConnection?.invoke('LeaveConversation', conversationId);
  }

  async sendMessage(
    conversationId: string, senderEmployeeId: string, body: string,
    attachment?: AttachmentUploadResultDto | null,
  ): Promise<void> {
    await this.hubConnection?.invoke(
      'SendMessage', conversationId, senderEmployeeId, body,
      attachment?.url ?? null, attachment?.fileName ?? null, attachment?.contentType ?? null, attachment?.sizeBytes ?? null,
    );
  }

  async disconnect(): Promise<void> {
    await this.hubConnection?.stop();
    this.hubConnection = null;
  }
}
