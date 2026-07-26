import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { MessagingService } from '../../../core/services/messaging.service';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeService } from '../../../core/services/employee.service';
import { ConversationDto, MessageDto } from '../../../core/models/messaging.model';
import { EmployeeDto } from '../../../core/models/employee.model';

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

  constructor(
    public messagingService: MessagingService,
    private authService: AuthService,
    private employeeService: EmployeeService,
  ) {}

  async ngOnInit(): Promise<void> {
    await this.messagingService.connect();
    this.loadConversations();
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
    if (!body || !conversationId || !senderEmployeeId) return;
    this.draft.set('');
    await this.messagingService.sendMessage(conversationId, senderEmployeeId, body);
  }

  isMine(message: MessageDto): boolean { return message.senderEmployeeId === this.authService.currentEmployeeId(); }
}
