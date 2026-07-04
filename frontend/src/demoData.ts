import type {
  Asset,
  AssetAssignment,
  AssetStatus,
  AssetType,
  Category,
  CurrentUser,
  TicketDetails,
  TicketPriority,
  TicketStatus,
  TicketTag,
} from './types'

export const demoUsers: CurrentUser[] = [
  {
    id: '22222222-2222-2222-2222-222222222222',
    fullName: 'Ewa Nowak',
    email: 'employee@test.com',
    role: 'Employee',
    department: 'Finance',
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    fullName: 'Tomasz Zielinski',
    email: 'technician@test.com',
    role: 'Technician',
    department: 'IT Support',
  },
  {
    id: '55555555-5555-5555-5555-555555555555',
    fullName: 'Marta Lewandowska',
    email: 'admin@test.com',
    role: 'Admin',
    department: 'IT Management',
  },
]

export const demoCategories: Category[] = [
  { id: 'cat-hardware', name: 'Hardware', description: 'Devices and peripherals', isActive: true },
  { id: 'cat-access', name: 'Account/Access', description: 'Permissions and onboarding', isActive: true },
  { id: 'cat-network', name: 'Network', description: 'VPN, Wi-Fi and internal services', isActive: true },
  { id: 'cat-email', name: 'Email', description: 'Mailboxes and calendars', isActive: true },
]

export const demoTags: TicketTag[] = [
  { id: 'tag-vip', name: 'VIP', color: '#8B5CF6', isActive: true },
  { id: 'tag-hardware', name: 'Hardware', color: '#2563EB', isActive: true },
  { id: 'tag-access', name: 'Access', color: '#059669', isActive: true },
  { id: 'tag-escalation', name: 'Escalation', color: '#DC2626', isActive: true },
  { id: 'tag-vendor', name: 'Waiting vendor', color: '#D97706', isActive: true },
]

export const demoAssets: Asset[] = [
  {
    id: 'asset-1',
    assetTag: 'LTP-2026-001',
    type: 'Laptop',
    brand: 'Lenovo',
    model: 'ThinkPad T14 Gen 6',
    serialNumber: 'SN-T14-2026-001',
    status: 'Assigned',
    assignedToUserId: demoUsers[0].id,
    assignedTo: demoUsers[0].fullName,
    purchaseDate: '2026-02-03',
    warrantyUntil: '2029-02-03',
    notes: 'Primary Finance device',
  },
  {
    id: 'asset-2',
    assetTag: 'MON-2026-014',
    type: 'Monitor',
    brand: 'Dell',
    model: 'UltraSharp U2725',
    serialNumber: 'SN-U2725-014',
    status: 'Available',
    assignedToUserId: null,
    assignedTo: null,
    purchaseDate: '2026-05-03',
    warrantyUntil: '2029-05-03',
    notes: 'Spare monitor',
  },
  {
    id: 'asset-3',
    assetTag: 'NET-2025-002',
    type: 'Router',
    brand: 'Ubiquiti',
    model: 'Dream Router',
    serialNumber: 'SN-UDR-002',
    status: 'InRepair',
    assignedToUserId: null,
    assignedTo: null,
    purchaseDate: '2025-07-12',
    warrantyUntil: '2027-07-12',
    notes: 'Branch office router',
  },
]

export const demoAssetHistories: Record<string, AssetAssignment[]> = {
  'asset-1': [
    {
      id: 'asset-assignment-1',
      userId: demoUsers[0].id,
      user: demoUsers[0].fullName,
      assignedByUserId: demoUsers[2].id,
      assignedBy: demoUsers[2].fullName,
      assignedAt: new Date(Date.now() - 120 * 24 * 60 * 60 * 1000).toISOString(),
      returnedAt: null,
    },
  ],
  'asset-3': [
    {
      id: 'asset-assignment-2',
      userId: demoUsers[1].id,
      user: demoUsers[1].fullName,
      assignedByUserId: demoUsers[2].id,
      assignedBy: demoUsers[2].fullName,
      assignedAt: new Date(Date.now() - 340 * 24 * 60 * 60 * 1000).toISOString(),
      returnedAt: new Date(Date.now() - 12 * 24 * 60 * 60 * 1000).toISOString(),
    },
  ],
}

export const demoTicketDetails: TicketDetails[] = [
  {
    id: 'ticket-1',
    reference: 'HD-00001',
    title: 'Laptop overheats during video calls',
    description:
      'Device becomes very hot after a few minutes in Teams meetings and fans run constantly.',
    category: 'Hardware',
    priority: 'High',
    status: 'InProgress',
    createdBy: 'Ewa Nowak',
    assignedTo: 'Tomasz Zielinski',
    relatedAssetId: 'asset-1',
    relatedAssetTag: 'LTP-2026-001',
    createdAt: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
    slaDueAt: new Date(Date.now() + 16 * 60 * 60 * 1000).toISOString(),
    isOverdue: false,
    tags: [demoTags[1]],
    comments: [
      {
        id: 'comment-1',
        author: 'Ewa Nowak',
        body: 'It happens mostly when I share the screen.',
        isInternal: false,
        createdAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
      },
      {
        id: 'comment-2',
        author: 'Tomasz Zielinski',
        body: 'Thermal diagnostics started. Checking firmware and fan curve.',
        isInternal: true,
        createdAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(),
      },
    ],
    attachments: [],
    history: [
      {
        id: 'history-1',
        action: 'Ticket created',
        oldValue: null,
        newValue: 'High hardware issue',
        actor: 'Ewa Nowak',
        createdAt: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString(),
      },
      {
        id: 'history-2',
        action: 'Status changed',
        oldValue: 'Assigned',
        newValue: 'InProgress',
        actor: 'Tomasz Zielinski',
        createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
      },
      {
        id: 'history-3',
        action: 'Asset linked to ticket',
        oldValue: null,
        newValue: 'LTP-2026-001',
        actor: 'Tomasz Zielinski',
        createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
      },
    ],
  },
  {
    id: 'ticket-2',
    reference: 'HD-00002',
    title: 'VPN access missing after role change',
    description: 'I moved to a new project and cannot connect to the client VPN profile.',
    category: 'Account/Access',
    priority: 'Medium',
    status: 'Resolved',
    createdBy: 'Ewa Nowak',
    assignedTo: 'Tomasz Zielinski',
    relatedAssetId: null,
    relatedAssetTag: null,
    createdAt: new Date(Date.now() - 54 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 21 * 60 * 60 * 1000).toISOString(),
    slaDueAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    isOverdue: false,
    tags: [demoTags[2]],
    comments: [
      {
        id: 'comment-3',
        author: 'Tomasz Zielinski',
        body: 'VPN group has been updated. Please sign out and back in.',
        isInternal: false,
        createdAt: new Date(Date.now() - 22 * 60 * 60 * 1000).toISOString(),
      },
    ],
    attachments: [],
    history: [
      {
        id: 'history-4',
        action: 'Status changed',
        oldValue: 'Assigned',
        newValue: 'Resolved',
        actor: 'Tomasz Zielinski',
        createdAt: new Date(Date.now() - 21 * 60 * 60 * 1000).toISOString(),
      },
    ],
  },
  {
    id: 'ticket-3',
    reference: 'HD-00003',
    title: 'Meeting room Wi-Fi drops calls',
    description: 'Several users reported unstable connection in room B-204.',
    category: 'Network',
    priority: 'Critical',
    status: 'New',
    createdBy: 'Marta Lewandowska',
    assignedTo: null,
    relatedAssetId: 'asset-3',
    relatedAssetTag: 'NET-2025-002',
    createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    updatedAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    slaDueAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    isOverdue: true,
    tags: [demoTags[3], demoTags[0]],
    comments: [],
    attachments: [],
    history: [
      {
        id: 'history-5',
        action: 'Ticket created',
        oldValue: null,
        newValue: 'Critical network issue',
        actor: 'Marta Lewandowska',
        createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
      },
    ],
  },
]

export const priorityOrder: Record<TicketPriority, number> = {
  Critical: 4,
  High: 3,
  Medium: 2,
  Low: 1,
}

export const ticketStatuses: TicketStatus[] = [
  'New',
  'Assigned',
  'InProgress',
  'WaitingForUser',
  'Resolved',
  'Closed',
  'Cancelled',
]

export const ticketPriorities: TicketPriority[] = ['Critical', 'High', 'Medium', 'Low']

export const assetStatuses: AssetStatus[] = ['Available', 'Assigned', 'InRepair', 'Retired', 'Lost']

export const assetTypes: AssetType[] = [
  'Laptop',
  'Desktop',
  'Monitor',
  'Phone',
  'Printer',
  'Router',
  'DockingStation',
  'Other',
]
