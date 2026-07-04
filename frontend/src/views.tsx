import { useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import {
  Activity,
  AlertTriangle,
  ArrowRight,
  Boxes,
  CheckCircle2,
  Clock3,
  Cpu,
  Download,
  FileText,
  Filter,
  HardDrive,
  Lock,
  Mail,
  MessageSquare,
  Plus,
  Search,
  Tags,
  Ticket as TicketIcon,
  Upload,
  UserCog,
  UserPlus,
  Users,
  Wrench,
  Zap,
} from 'lucide-react'
import {
  AssetStatusBadge,
  BarRow,
  InfoTile,
  LanguageToggle,
  MetricCard,
  PanelHeader,
  PriorityBadge,
  StatusBadge,
  StatusPill,
  TicketTagPill,
} from './components/common'
import { assetStatuses, assetTypes, demoUsers, ticketPriorities, ticketStatuses } from './demoData'
import {
  formatAuditAction,
  formatAuditValue,
  formatBytes,
  formatCategoryName,
  formatDate,
  formatDepartmentName,
  formatEntityType,
  formatNotificationBody,
  formatNotificationSubject,
  formatSla,
  formatTagList,
  formatTagName,
} from './formatting'
import type { UiText } from './i18n'
import type {
  Asset,
  AssetDetails,
  AssetStatus,
  AuditLogItem,
  Category,
  CurrentUser,
  DashboardSummary,
  EmailNotificationItem,
  Language,
  TicketAttachment,
  TicketDetails,
  TicketListItem,
  TicketPriority,
  TicketStatus,
  TicketTag,
  UserRole,
} from './types'

export function LoginScreen({
  isLoading,
  language,
  ui,
  onLanguageChange,
  onLogin,
}: {
  isLoading: boolean
  language: Language
  ui: UiText
  onLanguageChange: (language: Language) => void
  onLogin: (email: string, password: string) => Promise<void>
}) {
  const [email, setEmail] = useState('admin@test.com')
  const [password, setPassword] = useState('Password123!')

  return (
    <main className="login-screen">
      <section className="login-panel">
        <form
          className="login-form"
          onSubmit={(event) => {
            event.preventDefault()
            void onLogin(email, password)
          }}
        >
          <div className="login-header">
            <h1>{ui.login.title}</h1>
            <LanguageToggle language={language} ui={ui} onChange={onLanguageChange} />
          </div>

          <label>
            {ui.login.email}
            <input data-testid="login-email" value={email} onChange={(event) => setEmail(event.target.value)} />
          </label>
          <label>
            {ui.login.password}
            <input data-testid="login-password" value={password} type="password" onChange={(event) => setPassword(event.target.value)} />
          </label>

          <div className="account-grid">
            {demoUsers.map((user) => (
              <button
                className={email === user.email ? 'account-button active' : 'account-button'}
                data-testid={`account-${user.role.toLowerCase()}`}
                key={user.email}
                type="button"
                onClick={() => {
                  setEmail(user.email)
                  setPassword('Password123!')
                }}
              >
                <span>{ui.roles[user.role]}</span>
                <strong>{user.email}</strong>
              </button>
            ))}
          </div>

          <button className="primary-button" data-testid="login-submit" type="submit" disabled={isLoading}>
            <Lock size={18} />
            {isLoading ? ui.login.signingIn : ui.login.signIn}
          </button>
        </form>
      </section>
    </main>
  )
}

export function DashboardView({
  canExportReports,
  dashboard,
  onExportReport,
  tickets,
  ui,
}: {
  canExportReports: boolean
  dashboard: DashboardSummary
  onExportReport: (format: 'csv' | 'pdf') => Promise<void>
  tickets: TicketListItem[]
  ui: UiText
}) {
  return (
    <section className="dashboard-grid" data-testid="dashboard-view">
      <MetricCard icon={<Activity />} label={ui.dashboard.openTickets} testId="metric-open-tickets" value={dashboard.openTickets} tone="violet" />
      <MetricCard icon={<AlertTriangle />} label={ui.dashboard.overdue} value={dashboard.overdueTickets} tone="red" />
      <MetricCard icon={<CheckCircle2 />} label={ui.dashboard.resolvedThisWeek} value={dashboard.resolvedThisWeek} tone="green" />
      <MetricCard icon={<Boxes />} label={ui.dashboard.availableAssets} value={dashboard.availableAssets} tone="cyan" />

      <section className="panel wide">
        <PanelHeader icon={<Zap />} title={ui.dashboard.ticketsByPriority} />
        <div className="bar-list">
          {dashboard.ticketsByPriority.map((item) => (
            <BarRow key={item.priority} label={ui.priorities[item.priority]} value={item.count} max={Math.max(1, dashboard.openTickets)} />
          ))}
        </div>
      </section>

      <section className="panel">
        <PanelHeader icon={<Clock3 />} title={ui.dashboard.slaQueue} />
        <div className="sla-stack">
          {tickets.slice(0, 4).map((ticket) => (
            <div className="sla-row" key={ticket.id}>
              <span className={ticket.isOverdue ? 'dot danger' : 'dot ok'} />
              <div>
                <strong>{ticket.reference}</strong>
                <span>{ticket.title}</span>
              </div>
              <StatusBadge status={ticket.status} ui={ui} />
            </div>
          ))}
        </div>
      </section>

      <section className="panel">
        <PanelHeader icon={<Cpu />} title={ui.dashboard.assetsByStatus} />
        <div className="status-grid">
          {dashboard.assetsByStatus.map((item) => (
            <div className="status-box" key={item.status}>
              <strong>{item.count}</strong>
              <span>{ui.assetStatuses[item.status]}</span>
            </div>
          ))}
        </div>
      </section>

      {canExportReports && (
        <section className="panel">
          <PanelHeader icon={<FileText />} title={ui.dashboard.reports} />
          <div className="report-actions">
            <button className="icon-text-button" data-testid="report-export-csv" type="button" onClick={() => void onExportReport('csv')}>
              <Download size={16} />
              CSV
            </button>
            <button className="icon-text-button" data-testid="report-export-pdf" type="button" onClick={() => void onExportReport('pdf')}>
              <Download size={16} />
              PDF
            </button>
          </div>
        </section>
      )}
    </section>
  )
}

export function TicketsView({
  assets,
  canCreate,
  canWork,
  categories,
  categoryFilter,
  commentBody,
  assignedFilter,
  filteredTickets,
  internalComment,
  newTicketOpen,
  onlyOverdue,
  priorityFilter,
  search,
  selectedTicket,
  statusFilter,
  tagFilter,
  ticketTags,
  language,
  ui,
  users,
  onAddComment,
  onAddAttachment,
  onAssignToMe,
  onChangePriority,
  onChangeStatus,
  onCreateTicket,
  onDownloadAttachment,
  onLinkAsset,
  onSelectTicket,
  onSetAssignedFilter,
  onSetCategoryFilter,
  onSetCommentBody,
  onSetInternalComment,
  onSetNewTicketOpen,
  onSetOnlyOverdue,
  onSetPriorityFilter,
  onSetSearch,
  onSetStatusFilter,
  onSetTagFilter,
  onUpdateTicketTag,
}: {
  assets: Asset[]
  canCreate: boolean
  canWork: boolean
  categories: Category[]
  categoryFilter: string
  commentBody: string
  assignedFilter: string
  filteredTickets: TicketListItem[]
  internalComment: boolean
  newTicketOpen: boolean
  onlyOverdue: boolean
  priorityFilter: TicketPriority | 'All'
  search: string
  selectedTicket: TicketDetails | null
  statusFilter: TicketStatus | 'All'
  tagFilter: string
  ticketTags: TicketTag[]
  language: Language
  ui: UiText
  users: CurrentUser[]
  onAddComment: (ticketId: string) => Promise<void>
  onAddAttachment: (ticketId: string, file: File | null) => Promise<void>
  onAssignToMe: (ticketId: string) => Promise<void>
  onChangePriority: (ticketId: string, priority: TicketPriority) => Promise<void>
  onChangeStatus: (ticketId: string, status: TicketStatus) => Promise<void>
  onCreateTicket: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onDownloadAttachment: (ticketId: string, attachment: TicketAttachment) => Promise<void>
  onLinkAsset: (ticketId: string, assetId: string) => Promise<void>
  onSelectTicket: (ticketId: string) => Promise<void>
  onSetAssignedFilter: (value: string) => void
  onSetCategoryFilter: (value: string) => void
  onSetCommentBody: (value: string) => void
  onSetInternalComment: (value: boolean) => void
  onSetNewTicketOpen: (value: boolean) => void
  onSetOnlyOverdue: (value: boolean) => void
  onSetPriorityFilter: (value: TicketPriority | 'All') => void
  onSetSearch: (value: string) => void
  onSetStatusFilter: (value: TicketStatus | 'All') => void
  onSetTagFilter: (value: string) => void
  onUpdateTicketTag: (ticketId: string, tagId: string) => Promise<void>
}) {
  const technicianOptions = users.filter((user) => user.role === 'Technician')
  const activeCategories = categories.filter((category) => category.isActive !== false)

  return (
    <section className="tickets-layout" data-testid="tickets-view">
      <div className="ticket-list-panel">
        <div className="toolbar">
          <div className="search-box">
            <Search size={18} />
            <input data-testid="ticket-search" value={search} placeholder={ui.tickets.search} onChange={(event) => onSetSearch(event.target.value)} />
          </div>
          <select data-testid="filter-status" value={statusFilter} onChange={(event) => onSetStatusFilter(event.target.value as TicketStatus | 'All')}>
            <option value="All">{ui.common.all}</option>
            {ticketStatuses.map((status) => (
              <option key={status} value={status}>
                {ui.statuses[status]}
              </option>
            ))}
          </select>
          <select data-testid="filter-priority" value={priorityFilter} onChange={(event) => onSetPriorityFilter(event.target.value as TicketPriority | 'All')}>
            <option value="All">{ui.common.all}</option>
            {ticketPriorities.map((priority) => (
              <option key={priority} value={priority}>
                {ui.priorities[priority]}
              </option>
            ))}
          </select>
          <select data-testid="filter-category" value={categoryFilter} onChange={(event) => onSetCategoryFilter(event.target.value)}>
            <option value="All">{ui.common.all}</option>
            {categories.map((category) => (
              <option key={category.id} value={category.name}>
                {formatCategoryName(category.name, ui)}
              </option>
            ))}
          </select>
          <select data-testid="filter-assigned" value={assignedFilter} onChange={(event) => onSetAssignedFilter(event.target.value)}>
            <option value="All">{ui.common.all}</option>
            <option value="Unassigned">{ui.common.unassigned}</option>
            {technicianOptions.map((user) => (
              <option key={user.id} value={user.fullName}>
                {user.fullName}
              </option>
            ))}
          </select>
          <select data-testid="filter-tag" value={tagFilter} onChange={(event) => onSetTagFilter(event.target.value)}>
            <option value="All">{ui.tickets.allTags}</option>
            {ticketTags
              .filter((tag) => tag.isActive)
              .map((tag) => (
                <option key={tag.id} value={tag.id}>
                  {formatTagName(tag, ui)}
                </option>
              ))}
          </select>
          <label className="toggle-chip">
            <input checked={onlyOverdue} data-testid="filter-overdue" type="checkbox" onChange={(event) => onSetOnlyOverdue(event.target.checked)} />
            SLA
          </label>
          {canCreate && (
            <button className="icon-button" data-testid="new-ticket-toggle" title={ui.tickets.newTicket} type="button" onClick={() => onSetNewTicketOpen(!newTicketOpen)}>
              <Plus size={18} />
            </button>
          )}
        </div>

        {newTicketOpen && (
          <form className="create-ticket-form" onSubmit={(event) => void onCreateTicket(event)}>
            <input data-testid="new-ticket-title" name="title" placeholder={ui.tickets.title} />
            <textarea data-testid="new-ticket-description" name="description" placeholder={ui.tickets.description} />
            <div className="form-row">
              <select data-testid="new-ticket-category" name="category">
                {activeCategories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {formatCategoryName(category.name, ui)}
                  </option>
                ))}
              </select>
              <select data-testid="new-ticket-priority" name="priority">
                <option value="Medium">{ui.priorities.Medium}</option>
                <option value="Low">{ui.priorities.Low}</option>
                <option value="High">{ui.priorities.High}</option>
                <option value="Critical">{ui.priorities.Critical}</option>
              </select>
              <button className="primary-button compact" data-testid="new-ticket-submit" type="submit">
                <Plus size={16} />
                {ui.tickets.create}
              </button>
            </div>
          </form>
        )}

        <div className="table-header">
          <span>
            <Filter size={15} />
            {ui.tickets.count(filteredTickets.length)}
          </span>
          <span>SLA</span>
        </div>

        <div className="ticket-list" data-testid="ticket-list">
          {filteredTickets.map((ticket) => (
            <button className="ticket-row" data-testid={`ticket-row-${ticket.reference}`} key={ticket.id} type="button" onClick={() => void onSelectTicket(ticket.id)}>
              <div>
                <span className="ticket-ref">{ticket.reference}</span>
                <strong>{ticket.title}</strong>
                <small>
                  {formatCategoryName(ticket.category, ui)} / {ticket.createdBy}
                </small>
                {ticket.tags.length > 0 && (
                  <span className="tag-list">
                    {ticket.tags.map((tag) => (
                      <TicketTagPill key={tag.id} color={tag.color} label={formatTagName(tag, ui)} />
                    ))}
                  </span>
                )}
              </div>
              <div className="ticket-row-meta">
                <PriorityBadge priority={ticket.priority} ui={ui} />
                <StatusBadge status={ticket.status} ui={ui} />
                <span className={ticket.isOverdue ? 'sla overdue' : 'sla'}>{formatSla(ticket.slaDueAt, ticket.isOverdue, ui)}</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      <aside className="ticket-detail-panel">
        {selectedTicket ? (
          <>
            <div className="detail-head">
              <div>
                <span className="ticket-ref">{selectedTicket.reference}</span>
                <h2>{selectedTicket.title}</h2>
              </div>
              <span data-testid="selected-ticket-status">
                <StatusBadge status={selectedTicket.status} ui={ui} />
              </span>
            </div>
            <p className="description">{selectedTicket.description}</p>

            <div className="detail-grid">
              <InfoTile label={ui.tickets.priority} value={<PriorityBadge priority={selectedTicket.priority} ui={ui} />} />
              <InfoTile label={ui.tickets.category} value={formatCategoryName(selectedTicket.category, ui)} />
              <InfoTile label={ui.tickets.assigned} value={selectedTicket.assignedTo ?? ui.common.unassigned} />
              <InfoTile label={ui.tickets.asset} value={<span data-testid="selected-ticket-asset">{selectedTicket.relatedAssetTag ?? ui.common.none}</span>} />
            </div>

            <div className="tag-editor">
              <div>
                <span>{ui.tickets.tags}</span>
                <strong>{formatTagList(selectedTicket.tags, ui)}</strong>
              </div>
              {canWork && (
                <select
                  data-testid="ticket-tag-select"
                  value={selectedTicket.tags[0]?.id ?? ''}
                  onChange={(event) => void onUpdateTicketTag(selectedTicket.id, event.target.value)}
                >
                  <option value="">{ui.common.noTag}</option>
                  {ticketTags
                    .filter((tag) => tag.isActive)
                    .map((tag) => (
                      <option key={tag.id} value={tag.id}>
                        {formatTagName(tag, ui)}
                      </option>
                    ))}
                </select>
              )}
            </div>

            {(canWork || selectedTicket.status === 'Resolved') && (
              <div className="action-strip">
                {canWork && (
                  <>
                    {!selectedTicket.assignedTo && (
                      <button className="icon-text-button" data-testid="assign-to-me" type="button" onClick={() => void onAssignToMe(selectedTicket.id)}>
                        <UserCog size={17} />
                        {ui.tickets.assign}
                      </button>
                    )}
                    <button className="icon-text-button" data-testid="work-ticket" type="button" onClick={() => void onChangeStatus(selectedTicket.id, 'InProgress')}>
                      <Wrench size={17} />
                      {ui.tickets.work}
                    </button>
                    <button className="icon-text-button" data-testid="resolve-ticket" type="button" onClick={() => void onChangeStatus(selectedTicket.id, 'Resolved')}>
                      <CheckCircle2 size={17} />
                      {ui.tickets.resolve}
                    </button>
                    <select
                      className="action-select"
                      data-testid="ticket-priority-select"
                      value={selectedTicket.priority}
                      onChange={(event) => void onChangePriority(selectedTicket.id, event.target.value as TicketPriority)}
                    >
                      {ticketPriorities.map((priority) => (
                        <option key={priority} value={priority}>
                          {ui.priorities[priority]}
                        </option>
                      ))}
                    </select>
                    <select
                      className="action-select"
                      data-testid="ticket-asset-select"
                      value={selectedTicket.relatedAssetId ?? ''}
                      onChange={(event) => void onLinkAsset(selectedTicket.id, event.target.value)}
                    >
                      <option value="">{ui.tickets.linkAsset}</option>
                      {assets.map((asset) => (
                        <option key={asset.id} value={asset.id}>
                          {asset.assetTag}
                        </option>
                      ))}
                    </select>
                  </>
                )}
                {selectedTicket.status === 'Resolved' && (
                  <button className="icon-text-button" data-testid="close-ticket" type="button" onClick={() => void onChangeStatus(selectedTicket.id, 'Closed')}>
                    <CheckCircle2 size={17} />
                    {ui.tickets.close}
                  </button>
                )}
              </div>
            )}

            <section className="thread-section">
              <PanelHeader icon={<MessageSquare />} title={ui.tickets.comments} />
              <div className="comment-list">
                {selectedTicket.comments.map((comment) => (
                  <article className={comment.isInternal ? 'comment internal' : 'comment'} key={comment.id}>
                    <div>
                      <strong>{comment.author}</strong>
                      <span>{formatDate(comment.createdAt, language)}</span>
                    </div>
                    <p>{comment.body}</p>
                  </article>
                ))}
              </div>
              <div className="comment-box">
                <textarea data-testid="comment-body" value={commentBody} placeholder={ui.tickets.addComment} onChange={(event) => onSetCommentBody(event.target.value)} />
                <div>
                  {canWork && (
                    <label className="checkbox-label">
                      <input checked={internalComment} data-testid="comment-internal" type="checkbox" onChange={(event) => onSetInternalComment(event.target.checked)} />
                      {ui.tickets.internal}
                    </label>
                  )}
                  <button className="primary-button compact" data-testid="comment-submit" type="button" onClick={() => void onAddComment(selectedTicket.id)}>
                    <ArrowRight size={16} />
                    {ui.tickets.send}
                  </button>
                </div>
              </div>
            </section>

            <section className="thread-section">
              <PanelHeader icon={<Upload />} title={ui.tickets.attachments} />
              {selectedTicket.attachments.length > 0 ? (
                <div className="attachment-list" data-testid="attachment-list">
                  {selectedTicket.attachments.map((attachment) => (
                    <div className="attachment-row" data-testid={`attachment-row-${attachment.fileName}`} key={attachment.id}>
                      <div>
                        <strong>{attachment.fileName}</strong>
                        <span>
                          {formatBytes(attachment.size)} / {attachment.uploadedBy} / {formatDate(attachment.createdAt, language)}
                        </span>
                      </div>
                      <button className="icon-text-button" data-testid={`attachment-download-${attachment.fileName}`} type="button" onClick={() => void onDownloadAttachment(selectedTicket.id, attachment)}>
                        <Download size={16} />
                        {ui.tickets.download}
                      </button>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="empty-state compact">
                  <Upload size={26} />
                  <strong>{ui.tickets.noAttachments}</strong>
                </div>
              )}
              <label className="file-import-button attachment-upload-button">
                <Upload size={16} />
                {ui.tickets.uploadFile}
                <input
                  data-testid="attachment-upload"
                  type="file"
                  onChange={(event) => {
                    void onAddAttachment(selectedTicket.id, event.target.files?.[0] ?? null)
                    event.target.value = ''
                  }}
                />
              </label>
            </section>

            <section className="thread-section">
              <PanelHeader icon={<Activity />} title={ui.tickets.history} />
              <div className="timeline">
                {selectedTicket.history.map((item) => (
                  <div className="timeline-item" key={item.id}>
                    <span />
                    <div>
                      <strong>{formatAuditAction(item.action, ui)}</strong>
                      <small>
                        {item.actor} / {formatDate(item.createdAt, language)}
                      </small>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          </>
        ) : (
          <div className="empty-state">
            <TicketIcon size={32} />
            <strong>{ui.tickets.noTicketSelected}</strong>
          </div>
        )}
      </aside>
    </section>
  )
}

export function AssetsView({
  assets,
  canChangeStatus,
  canManage,
  language,
  selectedAsset,
  ui,
  users,
  onAssignAsset,
  onChangeAssetStatus,
  onCreateAsset,
  onExportAssetsCsv,
  onImportAssetsCsv,
  onSelectAsset,
}: {
  assets: Asset[]
  canChangeStatus: boolean
  canManage: boolean
  language: Language
  selectedAsset: AssetDetails | null
  ui: UiText
  users: CurrentUser[]
  onAssignAsset: (assetId: string, userId: string) => Promise<void>
  onChangeAssetStatus: (assetId: string, status: AssetStatus) => Promise<void>
  onCreateAsset: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onExportAssetsCsv: () => void
  onImportAssetsCsv: (event: ChangeEvent<HTMLInputElement>) => Promise<void>
  onSelectAsset: (assetId: string) => Promise<void>
}) {
  return (
    <section className="assets-workspace" data-testid="assets-view">
      {canManage && (
        <form className="asset-create-panel" onSubmit={(event) => void onCreateAsset(event)}>
          <input data-testid="asset-tag" name="assetTag" placeholder={ui.assets.assetTag} />
          <select data-testid="asset-type" name="type">
            {assetTypes.map((type) => (
              <option key={type} value={type}>
                {ui.assetTypes[type]}
              </option>
            ))}
          </select>
          <input data-testid="asset-brand" name="brand" placeholder={ui.assets.brand} />
          <input data-testid="asset-model" name="model" placeholder={ui.assets.model} />
          <input data-testid="asset-serial-number" name="serialNumber" placeholder={ui.assets.serialNumber} />
          <input data-testid="asset-purchase-date" name="purchaseDate" type="date" />
          <input data-testid="asset-warranty-until" name="warrantyUntil" type="date" />
          <input data-testid="asset-notes" name="notes" placeholder={ui.assets.notes} />
          <button className="primary-button compact" data-testid="asset-submit" type="submit">
            <Plus size={16} />
            {ui.assets.addAsset}
          </button>
        </form>
      )}

      {canManage && (
        <section className="asset-tools-panel">
          <button className="icon-text-button" data-testid="asset-export-csv" type="button" onClick={onExportAssetsCsv}>
            <Download size={16} />
            {ui.assets.exportCsv}
          </button>
          <label className="file-import-button">
            <Upload size={16} />
            {ui.assets.importCsv}
            <input accept=".csv,text/csv" data-testid="asset-import-csv" type="file" onChange={(event) => void onImportAssetsCsv(event)} />
          </label>
          <span>{ui.assets.columns}</span>
        </section>
      )}

      <div className="assets-layout">
        <div className="asset-grid">
          {assets.map((asset) => (
            <article className={selectedAsset?.id === asset.id ? 'asset-panel active' : 'asset-panel'} data-testid={`asset-card-${asset.assetTag}`} key={asset.id}>
              <button className="asset-summary-button" data-testid={`asset-select-${asset.assetTag}`} type="button" onClick={() => void onSelectAsset(asset.id)}>
                <span className="asset-icon">
                  <HardDrive />
                </span>
                <span>
                  <span className="ticket-ref">{asset.assetTag}</span>
                  <strong>
                    {asset.brand} {asset.model}
                  </strong>
                  <small>{asset.notes ?? asset.serialNumber}</small>
                </span>
              </button>
              <div className="asset-meta">
                <StatusPill label={ui.assetTypes[asset.type]} />
                <AssetStatusBadge status={asset.status} ui={ui} />
                <span>{asset.assignedTo ?? ui.common.unassigned}</span>
              </div>
              {(canChangeStatus || canManage) && (
                <div className="asset-controls">
                  {canChangeStatus && (
                    <select data-testid={`asset-status-${asset.assetTag}`} value={asset.status} onChange={(event) => void onChangeAssetStatus(asset.id, event.target.value as AssetStatus)}>
                      {assetStatuses.map((status) => (
                        <option key={status} value={status}>
                          {ui.assetStatuses[status]}
                        </option>
                      ))}
                    </select>
                  )}
                  {canManage && (
                    <select data-testid={`asset-assign-${asset.assetTag}`} value="" onChange={(event) => void onAssignAsset(asset.id, event.target.value)}>
                      <option value="">{ui.assets.assignUser}</option>
                      {users.map((user) => (
                        <option key={user.id} value={user.id}>
                          {user.fullName}
                        </option>
                      ))}
                    </select>
                  )}
                </div>
              )}
            </article>
          ))}
        </div>

        <aside className="asset-detail-panel" data-testid="asset-details-panel">
          {selectedAsset ? (
            <>
              <div className="detail-head">
                <div>
                  <span className="ticket-ref">{selectedAsset.assetTag}</span>
                  <h2>
                    {selectedAsset.brand} {selectedAsset.model}
                  </h2>
                </div>
                <AssetStatusBadge status={selectedAsset.status} ui={ui} />
              </div>

              <div className="detail-grid asset-detail-grid">
                <InfoTile label={ui.assets.type} value={ui.assetTypes[selectedAsset.type]} />
                <InfoTile label={ui.assets.serial} value={selectedAsset.serialNumber} />
                <InfoTile label={ui.assets.assigned} value={selectedAsset.assignedTo ?? ui.common.unassigned} />
                <InfoTile label={ui.assets.warranty} value={selectedAsset.warrantyUntil} />
              </div>

              <section className="thread-section">
                <PanelHeader icon={<Activity />} title={ui.assets.assignmentHistory} />
                {selectedAsset.assignmentHistory.length > 0 ? (
                  <div className="timeline" data-testid="asset-assignment-history">
                    {selectedAsset.assignmentHistory.map((assignment) => (
                      <div className="timeline-item" key={assignment.id}>
                        <span />
                        <div>
                          <strong>{assignment.user}</strong>
                          <small>
                            {ui.assets.assignedBy} {assignment.assignedBy} / {formatDate(assignment.assignedAt, language)}
                            {assignment.returnedAt ? ` / ${ui.assets.returned} ${formatDate(assignment.returnedAt, language)}` : ` / ${ui.common.active}`}
                          </small>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="empty-state compact">
                    <HardDrive size={26} />
                    <strong>{ui.assets.noAssignmentHistory}</strong>
                  </div>
                )}
              </section>
            </>
          ) : (
            <div className="empty-state">
              <HardDrive size={32} />
              <strong>{ui.assets.noAssetSelected}</strong>
            </div>
          )}
        </aside>
      </div>
    </section>
  )
}

export function UsersView({
  ui,
  users,
  onChangeUserRole,
  onCreateUser,
}: {
  ui: UiText
  users: CurrentUser[]
  onChangeUserRole: (userId: string, role: UserRole) => Promise<void>
  onCreateUser: (event: FormEvent<HTMLFormElement>) => Promise<void>
}) {
  return (
    <section className="admin-workspace" data-testid="users-view">
      <form className="admin-create-panel" onSubmit={(event) => void onCreateUser(event)}>
        <input data-testid="user-full-name" name="fullName" placeholder={ui.users.fullName} />
        <input data-testid="user-email" name="email" placeholder={ui.users.email} />
        <input data-testid="user-password" name="password" placeholder={ui.users.temporaryPassword} type="password" />
        <select data-testid="user-role" name="role">
          <option value="Employee">{ui.roles.Employee}</option>
          <option value="Technician">{ui.roles.Technician}</option>
          <option value="Admin">{ui.roles.Admin}</option>
        </select>
        <input data-testid="user-department" name="department" placeholder={ui.users.department} />
        <button className="primary-button compact" data-testid="user-submit" type="submit">
          <UserPlus size={16} />
          {ui.users.addUser}
        </button>
      </form>

      <section className="panel users-panel">
        <PanelHeader icon={<Users />} title={ui.users.users} />
        <div className="users-list">
          {users.map((user) => (
            <div className="user-row" data-testid={`user-row-${user.email}`} key={user.id}>
              <div>
                <strong>{user.fullName}</strong>
                <span>{user.email}</span>
              </div>
              <div className="user-row-meta">
                <select
                  data-testid={`user-role-${user.email}`}
                  value={user.role}
                  onChange={(event) => void onChangeUserRole(user.id, event.target.value as UserRole)}
                >
                  <option value="Employee">{ui.roles.Employee}</option>
                  <option value="Technician">{ui.roles.Technician}</option>
                  <option value="Admin">{ui.roles.Admin}</option>
                </select>
                <span>{formatDepartmentName(user.department, ui)}</span>
              </div>
            </div>
          ))}
        </div>
      </section>
    </section>
  )
}

export function CategoriesView({
  categories,
  ui,
  onCreateCategory,
  onUpdateCategory,
}: {
  categories: Category[]
  ui: UiText
  onCreateCategory: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onUpdateCategory: (event: FormEvent<HTMLFormElement>, categoryId: string) => Promise<void>
}) {
  return (
    <section className="admin-workspace" data-testid="categories-view">
      <form className="admin-create-panel category-create-panel" onSubmit={(event) => void onCreateCategory(event)}>
        <input data-testid="category-name" name="name" placeholder={ui.categories.categoryName} />
        <input data-testid="category-description" name="description" placeholder={ui.categories.description} />
        <button className="primary-button compact" data-testid="category-submit" type="submit">
          <Plus size={16} />
          {ui.categories.addCategory}
        </button>
      </form>

      <section className="panel categories-panel">
        <PanelHeader icon={<Tags />} title={ui.categories.ticketCategories} />
        <div className="management-list">
          {categories.map((category) => (
            <form
              className="management-row category-row"
              data-testid={`category-row-${category.name}`}
              key={category.id}
              onSubmit={(event) => void onUpdateCategory(event, category.id)}
            >
              <input data-testid={`category-name-${category.name}`} defaultValue={category.name} name="name" />
              <input data-testid={`category-description-${category.name}`} defaultValue={category.description} name="description" />
              <label className="checkbox-label category-active-toggle">
                <input data-testid={`category-active-${category.name}`} defaultChecked={category.isActive !== false} name="isActive" type="checkbox" />
                {ui.common.active}
              </label>
              <StatusPill label={category.isActive === false ? ui.common.inactive : ui.common.active} />
              <button className="icon-text-button" data-testid={`category-save-${category.name}`} type="submit">
                <CheckCircle2 size={16} />
                {ui.categories.save}
              </button>
            </form>
          ))}
        </div>
      </section>
    </section>
  )
}

export function AuditView({
  entityFilter,
  language,
  logs,
  search,
  ui,
  onSetEntityFilter,
  onSetSearch,
}: {
  entityFilter: string
  language: Language
  logs: AuditLogItem[]
  search: string
  ui: UiText
  onSetEntityFilter: (value: string) => void
  onSetSearch: (value: string) => void
}) {
  return (
    <section className="audit-workspace" data-testid="audit-view">
      <section className="panel audit-panel">
        <div className="toolbar">
          <div className="search-box">
            <Search size={18} />
            <input data-testid="audit-search" value={search} placeholder={ui.audit.search} onChange={(event) => onSetSearch(event.target.value)} />
          </div>
          <select data-testid="audit-entity-filter" value={entityFilter} onChange={(event) => onSetEntityFilter(event.target.value)}>
            <option value="All">{ui.common.all}</option>
            <option value="Ticket">{ui.audit.ticket}</option>
            <option value="Asset">{ui.audit.asset}</option>
          </select>
        </div>

        <div className="table-header">
          <span>
            <Activity size={15} />
            {ui.audit.count(logs.length)}
          </span>
          <span>{ui.common.newestFirst}</span>
        </div>

        {logs.length > 0 ? (
          <div className="audit-list">
            {logs.map((log) => (
              <article className="audit-row" data-testid={`audit-row-${log.entityType}-${log.action}`} key={log.id}>
                <div className="audit-row-main">
                  <StatusPill label={formatEntityType(log.entityType, ui)} />
                  <div>
                    <strong>{formatAuditAction(log.action, ui)}</strong>
                    <span>
                      {log.actor} / {formatDate(log.createdAt, language)}
                    </span>
                  </div>
                </div>
                <div className="audit-values">
                  <span>{ui.common.oldValue}: {formatAuditValue(log.oldValue, ui)}</span>
                  <span>{ui.common.newValue}: {formatAuditValue(log.newValue, ui)}</span>
                </div>
              </article>
            ))}
          </div>
        ) : (
          <div className="empty-state compact">
            <Activity size={26} />
            <strong>{ui.audit.noEntries}</strong>
          </div>
        )}
      </section>
    </section>
  )
}

export function NotificationsView({
  language,
  notifications,
  ui,
  onRefresh,
}: {
  language: Language
  notifications: EmailNotificationItem[]
  ui: UiText
  onRefresh: () => Promise<void>
}) {
  return (
    <section className="audit-workspace" data-testid="notifications-view">
      <section className="panel audit-panel">
        <div className="toolbar">
          <PanelHeader icon={<Mail />} title={ui.views.notifications} />
          <button className="icon-text-button" data-testid="notifications-refresh" type="button" onClick={() => void onRefresh()}>
            <Activity size={16} />
            {ui.common.refresh}
          </button>
        </div>

        <div className="table-header">
          <span>
            <Mail size={15} />
            {ui.notifications.messages(notifications.length)}
          </span>
          <span>{ui.common.newestFirst}</span>
        </div>

        {notifications.length > 0 ? (
          <div className="audit-list">
            {notifications.map((notification) => (
              <article className="audit-row notification-row" data-testid={`notification-row-${notification.id}`} key={notification.id}>
                <div className="audit-row-main">
                  <StatusPill label={ui.notificationStatuses[notification.status]} />
                  <div>
                    <strong>{formatNotificationSubject(notification.subject, ui)}</strong>
                    <span>
                      {notification.toEmail} / {formatDate(notification.createdAt, language)}
                    </span>
                  </div>
                </div>
                <div className="audit-values">
                  <span>{formatNotificationBody(notification.body, ui)}</span>
                  {notification.error && <span>{ui.notifications.error}: {notification.error}</span>}
                </div>
              </article>
            ))}
          </div>
        ) : (
          <div className="empty-state compact">
            <Mail size={26} />
            <strong>{ui.notifications.noQueued}</strong>
          </div>
        )}
      </section>
    </section>
  )
}
