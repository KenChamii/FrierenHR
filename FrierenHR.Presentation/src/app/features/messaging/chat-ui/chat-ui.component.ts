import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { MessagingService, MAX_ATTACHMENT_SIZE_BYTES } from '../../../core/services/messaging.service';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeService } from '../../../core/services/employee.service';
import { ConversationDto, MessageDto, AttachmentUploadResultDto } from '../../../core/models/messaging.model';
import { EmployeeDto } from '../../../core/models/employee.model';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-chat-ui',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, DropdownModule],
  templateUrl: './chat-ui.component.html',
  styleUrl: './chat-ui.component.scss',
})
export class ChatUiComponent implements OnInit, OnDestroy {
  readonly conversations = signal<ConversationDto[]>([]);
  readonly activeConversationId = signal<string | null>(null);
  readonly history = signal<MessageDto[]>([]);
  readonly draft = signal('');
  readonly loadingConversations = signal(true);
  readonly loadingHistory = signal(false);

  // "New Message" picker state — without this there was no way to start a conversation at
  // all; the page could only ever show conversations that already existed via a direct API call.
  readonly showNewMessage = signal(false);
  readonly coworkers = signal<EmployeeDto[]>([]);
  readonly selectedCoworkerId = signal<string | null>(null);
  readonly startingConversation = signal(false);

  // Merge REST-loaded history with anything that's arrived live over the hub since joining.
  readonly allMessages = computed(() => [...this.history(), ...this.messagingService.liveMessages()]);

  // The full conversation object for the active chat, so the header can show its name/avatar.
  readonly activeConversation = computed(() =>
    this.conversations().find((c) => c.id === this.activeConversationId()),
  );

  // Sum of unread counts across every conversation, shown as the badge next to "Chats".
  readonly totalUnread = computed(() =>
    this.conversations().reduce((sum, c) => sum + (c.unreadCount ?? 0), 0),
  );

  // Messages bucketed under "Today" / "Yesterday" / date dividers, like a real chat app.
  readonly groupedMessages = computed(() => {
    const groups: { label: string; messages: MessageDto[] }[] = [];
    for (const message of this.allMessages()) {
      const label = this.dateLabel(message.sentAt);
      const lastGroup = groups[groups.length - 1];
      if (lastGroup && lastGroup.label === label) lastGroup.messages.push(message);
      else groups.push({ label, messages: [message] });
    }
    return groups;
  });

  private readonly avatarPalette = ['#1f7a4d', '#14532d', '#3b7dd8', '#d9a441', '#45b980'];

  // Attachment picker state: a file is uploaded as soon as it's chosen (not on send) so the
  // person sees the size/type error immediately instead of after typing a message and hitting send.
  readonly maxAttachmentSizeBytes = MAX_ATTACHMENT_SIZE_BYTES;
  readonly uploadingAttachment = signal(false);
  readonly pendingAttachment = signal<AttachmentUploadResultDto | null>(null);
  readonly attachmentError = signal<string | null>(null);

  constructor(
    public messagingService: MessagingService,
    private authService: AuthService,
    private employeeService: EmployeeService,
  ) {}

  async ngOnInit(): Promise<void> {
    // Load the list first, independently of the realtime hub — a slow/unreachable hub
    // (backend down, cert issue on localhost, etc.) must not block the conversation list
    // from ever rendering.
    this.loadConversations();
    try {
      await this.messagingService.connect();
    } catch (err) {
      console.error('Chat hub connection failed; messages will still load, but live updates are unavailable.', err);
    }
  }

  loadConversations(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loadingConversations.set(false); return; }
    this.loadingConversations.set(true);
    this.messagingService.getConversations(employeeId).subscribe({
      next: (list) => { this.conversations.set(list); this.loadingConversations.set(false); },
      error: () => this.loadingConversations.set(false),
    });
  }

  async ngOnDestroy(): Promise<void> {
    if (this.activeConversationId()) await this.messagingService.leaveConversation(this.activeConversationId()!);
    await this.messagingService.disconnect();
  }

  openNewMessage(): void {
    this.showNewMessage.set(true);
    this.selectedCoworkerId.set(null);
    const companyId = this.authService.currentCompanyId();
    const myId = this.authService.currentEmployeeId();
    if (!companyId) return;
    this.employeeService.getByCompany(companyId).subscribe({
      next: (list) => this.coworkers.set(list.filter((e) => e.id !== myId)),
    });
  }

  startDirectConversation(): void {
    const myId = this.authService.currentEmployeeId();
    const otherId = this.selectedCoworkerId();
    if (!myId || !otherId) return;

    this.startingConversation.set(true);
    this.messagingService.getOrCreateDirect({ employeeAId: myId, employeeBId: otherId }).subscribe({
      next: (conversation) => {
        this.startingConversation.set(false);
        this.showNewMessage.set(false);
        // Put it at the top of the list right away instead of waiting for a reload —
        // it may already be there (existing chat), so replace rather than duplicate.
        this.conversations.update((list) => [conversation, ...list.filter((c) => c.id !== conversation.id)]);
        this.selectConversation(conversation);
      },
      error: () => this.startingConversation.set(false),
    });
  }

  async selectConversation(conversation: ConversationDto): Promise<void> {
    if (this.activeConversationId()) await this.messagingService.leaveConversation(this.activeConversationId()!);
    this.activeConversationId.set(conversation.id);
    this.loadingHistory.set(true);
    this.messagingService.getHistory(conversation.id).subscribe({
      next: (msgs) => { this.history.set(msgs.slice().reverse()); this.loadingHistory.set(false); },
      error: () => this.loadingHistory.set(false),
    });
    await this.messagingService.joinConversation(conversation.id);
  }

  async send(): Promise<void> {
    const body = this.draft().trim();
    const conversationId = this.activeConversationId();
    const senderEmployeeId = this.authService.currentEmployeeId();
    const attachment = this.pendingAttachment();
    // A message needs text or an attachment (matches the hub's own guard) — not neither.
    if ((!body && !attachment) || !conversationId || !senderEmployeeId || this.uploadingAttachment()) return;
    this.draft.set('');
    this.pendingAttachment.set(null);
    await this.messagingService.sendMessage(conversationId, senderEmployeeId, body, attachment);
  }

  // Uploads immediately on selection so the person finds out right away if it's too big or
  // an unsupported type, rather than after they've written a message and hit send.
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // allow re-selecting the same file later
    if (!file) return;

    this.attachmentError.set(null);
    if (file.size > this.maxAttachmentSizeBytes) {
      this.attachmentError.set(`"${file.name}" is too large — the limit is ${this.formatBytes(this.maxAttachmentSizeBytes)}.`);
      return;
    }

    this.uploadingAttachment.set(true);
    this.messagingService.uploadAttachment(file).subscribe({
      next: (result) => { this.pendingAttachment.set(result); this.uploadingAttachment.set(false); },
      error: (err) => {
        this.uploadingAttachment.set(false);
        this.attachmentError.set(err?.error ?? 'Upload failed — please try again.');
      },
    });
  }

  removeAttachment(): void {
    this.pendingAttachment.set(null);
    this.attachmentError.set(null);
  }

  attachmentAbsoluteUrl(url: string | undefined): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${environment.apiUrl}${url}`;
  }

  isImageAttachment(contentType: string | undefined): boolean {
    return !!contentType?.startsWith('image/');
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  isMine(message: MessageDto): boolean { return message.senderEmployeeId === this.authService.currentEmployeeId(); }

  // Mobile view shows one pane at a time — this returns to the conversation list.
  async goBack(): Promise<void> {
    if (this.activeConversationId()) await this.messagingService.leaveConversation(this.activeConversationId()!);
    this.activeConversationId.set(null);
  }

  initials(name: string | undefined | null): string {
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    const first = parts[0]?.[0] ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
    return (first + last).toUpperCase();
  }

  avatarColor(seed: string | undefined | null): string {
    const key = seed ?? '?';
    let hash = 0;
    for (let i = 0; i < key.length; i++) hash = (hash * 31 + key.charCodeAt(i)) >>> 0;
    return this.avatarPalette[hash % this.avatarPalette.length];
  }

  private dateLabel(iso: string): string {
    const date = new Date(iso);
    const today = new Date();
    const yesterday = new Date();
    yesterday.setDate(today.getDate() - 1);
    if (date.toDateString() === today.toDateString()) return 'Today';
    if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';
    return date.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== today.getFullYear() ? 'numeric' : undefined,
    });
  }
}
