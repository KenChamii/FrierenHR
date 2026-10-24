import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import {
  ConversationDto, MessageDto, CreateDirectConversationDto, CreateGroupConversationDto,
  AttachmentUploadResultDto,
} from '../models/messaging.model';

// Kept in sync with Messaging:MaxAttachmentSizeBytes in the backend's appsettings.json.
export const MAX_ATTACHMENT_SIZE_BYTES = 10 * 1024 * 1024; // 10MB

@Injectable({ providedIn: 'root' })
export class MessagingService {
  private readonly baseUrl = `${environment.apiUrl}/api/messaging`;

  private hubConnection: signalR.HubConnection | null = null;

  // The in-flight connection attempt, not the connection. Guarding on the connection object
  // let a second caller through while the first was still starting, so a send could be
  // issued against a socket that was not up yet.
  private connecting: Promise<void> | null = null;

  readonly liveMessages = signal<MessageDto[]>([]);
  readonly connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);
  readonly isConnected = computed(() => this.connectionState() === signalR.HubConnectionState.Connected);

  /** Conversation ids with something unread, driving the badge in the shell. */
  readonly unreadConversationIds = signal<Set<string>>(new Set());
  readonly unreadCount = computed(() => this.unreadConversationIds().size);

  /** The most recent message that arrived for a conversation the person is not reading. */
  readonly lastNotification = signal<MessageDto | null>(null);

  constructor(private http: HttpClient, private authService: AuthService) {}

  getConversations(employeeId: string) {
    return this.http.get<ConversationDto[]>(`${this.baseUrl}/conversations/${employeeId}`);
  }

  getHistory(conversationId: string, skip = 0, take = 50) {
    return this.http.get<MessageDto[]>(
      `${this.baseUrl}/conversations/${conversationId}/history`, { params: { skip, take } });
  }

  getOrCreateDirect(dto: CreateDirectConversationDto) {
    return this.http.post<ConversationDto>(`${this.baseUrl}/conversations/direct`, dto);
  }

  createGroup(dto: CreateGroupConversationDto) {
    return this.http.post<ConversationDto>(`${this.baseUrl}/conversations/group`, dto);
  }

  // Uploaded over plain HTTP rather than the hub, which is not built for binary payloads.
  uploadAttachment(file: File) {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<AttachmentUploadResultDto>(`${this.baseUrl}/attachments`, form);
  }

  /** Connects if not already connected, and is safe to call from anywhere: concurrent callers await the same attempt. */
  async connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;
    if (this.connecting) return this.connecting;

    this.connecting = this.start().finally(() => { this.connecting = null; });
    return this.connecting;
  }

  private async start(): Promise<void> {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.authService.token() ?? '' })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (message: MessageDto) =>
      this.liveMessages.update(list => [...list, message]));

    // Sent only to participants who do not have the conversation open, so anything arriving here is by definition unread.
    connection.on('ConversationUpdated', (message: MessageDto) => {
      this.unreadConversationIds.update(ids => new Set(ids).add(message.conversationId));
      this.lastNotification.set(message);
    });

    connection.onreconnected(() => this.connectionState.set(signalR.HubConnectionState.Connected));
    connection.onreconnecting(() => this.connectionState.set(signalR.HubConnectionState.Reconnecting));
    connection.onclose(() => this.connectionState.set(signalR.HubConnectionState.Disconnected));

    try {
      await connection.start();
    } catch (error) {
      // Left null so a later call builds a fresh connection.
      this.hubConnection = null;
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      throw error;
    }

    this.hubConnection = connection;
    this.connectionState.set(signalR.HubConnectionState.Connected);
  }

  async joinConversation(conversationId: string): Promise<void> {
    this.liveMessages.set([]);
    await this.invoke('JoinConversation', conversationId);
    this.clearUnread(conversationId);
  }

  async leaveConversation(conversationId: string): Promise<void> {
    // Best effort. Leaving a group on a connection that has already gone is not a failure worth showing anyone.
    try {
      await this.invoke('LeaveConversation', conversationId);
    } catch {
      // Nothing to do: the group membership died with the connection.
    }
  }

  async sendMessage(
    conversationId: string, senderEmployeeId: string, body: string,
    attachment?: AttachmentUploadResultDto | null,
  ): Promise<void> {
    await this.invoke(
      'SendMessage', conversationId, senderEmployeeId, body,
      attachment?.url ?? null, attachment?.fileName ?? null,
      attachment?.contentType ?? null, attachment?.sizeBytes ?? null);
  }

  async markRead(conversationId: string): Promise<void> {
    this.clearUnread(conversationId);
    try {
      await this.invoke('MarkRead', conversationId);
    } catch {
      // The badge is already cleared locally; it corrects itself on the next load.
    }
  }

  /** Seeds the badge from the conversation list, so it is right before anything arrives live. */
  seedUnread(conversations: ConversationDto[]): void {
    this.unreadConversationIds.set(
      new Set(conversations.filter(c => (c.unreadCount ?? 0) > 0).map(c => c.id)));
  }

  clearUnread(conversationId: string): void {
    this.unreadConversationIds.update(ids => {
      const next = new Set(ids);
      next.delete(conversationId);
      return next;
    });
  }

  async disconnect(): Promise<void> {
    const connection = this.hubConnection;
    this.hubConnection = null;
    this.connectionState.set(signalR.HubConnectionState.Disconnected);
    await connection?.stop();
  }

  /** Every hub call goes through here, so none of them can be silently dropped. */
  private async invoke(method: string, ...args: unknown[]): Promise<void> {
    await this.connect();

    if (!this.hubConnection) {
      throw new Error('Not connected to the chat service.');
    }

    await this.hubConnection.invoke(method, ...args);
  }
}
