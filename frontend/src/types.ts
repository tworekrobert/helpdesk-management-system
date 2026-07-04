export type UserRole = 'Employee' | 'Technician' | 'Admin'

export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export type TicketStatus =
  | 'New'
  | 'Assigned'
  | 'InProgress'
  | 'WaitingForUser'
  | 'Resolved'
  | 'Closed'
  | 'Cancelled'

export type AssetStatus = 'Available' | 'Assigned' | 'InRepair' | 'Retired' | 'Lost'

export type AssetType =
  | 'Laptop'
  | 'Desktop'
  | 'Monitor'
  | 'Phone'
  | 'Printer'
  | 'Router'
  | 'DockingStation'
  | 'Other'

export type CurrentUser = {
  id: string
  fullName: string
  email: string
  role: UserRole
  department: string
  isActive?: boolean
}

export type TicketListItem = {
  id: string
  reference: string
  title: string
  category: string
  priority: TicketPriority
  status: TicketStatus
  createdBy: string
  assignedTo: string | null
  relatedAssetId: string | null
  relatedAssetTag: string | null
  createdAt: string
  updatedAt: string
  slaDueAt: string
  isOverdue: boolean
  tags: TicketTag[]
}

export type TicketDetails = TicketListItem & {
  description: string
  comments: TicketComment[]
  attachments: TicketAttachment[]
  history: TicketHistory[]
}

export type TicketTag = {
  id: string
  name: string
  color: string
  isActive: boolean
}

export type UserSummary = {
  id: string
  fullName: string
  email: string
}

export type AssetSummary = {
  id: string
  assetTag: string
  label: string
}

export type ApiTicketDetails = Omit<
  TicketDetails,
  'assignedTo' | 'createdBy' | 'relatedAssetId' | 'relatedAssetTag'
> & {
  createdBy: UserSummary
  assignedTo: UserSummary | null
  relatedAsset: AssetSummary | null
  resolvedAt: string | null
  closedAt: string | null
}

export type TicketComment = {
  id: string
  author: string
  body: string
  isInternal: boolean
  createdAt: string
}

export type TicketAttachment = {
  id: string
  fileName: string
  contentType: string
  size: number
  uploadedBy: string
  createdAt: string
}

export type TicketHistory = {
  id: string
  action: string
  oldValue: string | null
  newValue: string | null
  actor: string
  createdAt: string
}

export type Category = {
  id: string
  name: string
  description: string
  isActive?: boolean
}

export type Asset = {
  id: string
  assetTag: string
  type: AssetType
  brand: string
  model: string
  serialNumber: string
  status: AssetStatus
  assignedToUserId: string | null
  assignedTo: string | null
  purchaseDate: string
  warrantyUntil: string
  notes: string | null
}

export type AssetAssignment = {
  id: string
  userId: string
  user: string
  assignedByUserId: string
  assignedBy: string
  assignedAt: string
  returnedAt: string | null
}

export type AssetDetails = Asset & {
  assignmentHistory: AssetAssignment[]
}

export type AssetCsvRow = {
  assetTag: string
  type: AssetType
  brand: string
  model: string
  serialNumber: string
  purchaseDate: string
  warrantyUntil: string
  notes: string | null
}

export type AuditLogItem = {
  id: string
  entityType: string
  entityId: string
  action: string
  oldValue: string | null
  newValue: string | null
  userId: string
  actor: string
  actorEmail: string
  createdAt: string
}

export type DashboardSummary = {
  openTickets: number
  unassignedTickets: number
  overdueTickets: number
  resolvedThisWeek: number
  averageResolutionTime: string
  availableAssets: number
  assetsInRepair: number
  ticketsByPriority: { priority: TicketPriority; count: number }[]
  ticketsByCategory: { category: string; count: number }[]
  assetsByStatus: { status: AssetStatus; count: number }[]
  topUsers: { user: string; count: number }[]
}

export type EmailNotificationStatus = 'Pending' | 'Sent' | 'Failed'

export type EmailNotificationItem = {
  id: string
  recipientUserId: string
  toEmail: string
  subject: string
  body: string
  status: EmailNotificationStatus
  createdAt: string
  processedAt: string | null
  error: string | null
}

export type View = 'dashboard' | 'tickets' | 'assets' | 'users' | 'categories' | 'audit' | 'notifications'

export type Language = 'en' | 'pl'
