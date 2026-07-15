import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessagingService } from '../../../core/services/messaging.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConversationDto, MessageDto } from '../../../core/models/messaging.model';

@Component({
  selector: 'app-chat-ui',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule],
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

  // Merge REST-loaded history with anything that's arrived live over the hub since joining.
  readonly allMessages = computed(() => [...this.history(), ...this.messagingService.liveMessages()]);

  constructor(public messagingService: MessagingService, private authService: AuthService) {}

  async ngOnInit(): Promise<void> {
    await this.messagingService.connect();
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loadingConversations.set(false); return; }
    this.messagingService.getConversations(employeeId).subscribe({
      next: (list) => { this.conversations.set(list); this.loadingConversations.set(false); },
      error: () => this.loadingConversations.set(false),
    });
  }

  async ngOnDestroy(): Promise<void> {
    if (this.activeConversationId()) await this.messagingService.leaveConversation(this.activeConversationId()!);
    await this.messagingService.disconnect();
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
    if (!body || !conversationId || !senderEmployeeId) return;
    this.draft.set('');
    await this.messagingService.sendMessage(conversationId, senderEmployeeId, body);
  }

  isMine(message: MessageDto): boolean { return message.senderEmployeeId === this.authService.currentEmployeeId(); }
}