import { HardDrive, LayoutDashboard, LogOut, Mail, ShieldCheck, Tags, Ticket as TicketIcon, Users } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import './App.css'
import { API_BASE, viewFromPath, viewPaths } from './api'
import { LanguageToggle, NavButton } from './components/common'
import { assetTypes, demoAssetHistories, demoAssets, demoCategories, demoTags, demoTicketDetails, demoUsers, priorityOrder } from './demoData'
import { formatCategoryName, formatDepartmentName, formatRawTagList, viewTitle } from './formatting'
import { uiText, type UiText } from './i18n'
import {
  AssetsView,
  AuditView,
  CategoriesView,
  DashboardView,
  LoginScreen,
  NotificationsView,
  TicketsView,
  UsersView,
} from './views'
import type {
  ApiTicketDetails,
  Asset,
  AssetAssignment,
  AssetCsvRow,
  AssetDetails,
  AssetStatus,
  AssetType,
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
  View,
} from './types'

function App() {
  const [token, setToken] = useState<string | null>(null)
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null)
  const [language, setLanguage] = useState<Language>('en')
  const [demoMode, setDemoMode] = useState(false)
  const [view, setViewState] = useState<View>(() => viewFromPath(window.location.pathname))
  const [tickets, setTickets] = useState<TicketListItem[]>([])
  const [ticketDetails, setTicketDetails] = useState<Record<string, TicketDetails>>({})
  const [assets, setAssets] = useState<Asset[]>([])
  const [assetDetails, setAssetDetails] = useState<Record<string, AssetDetails>>({})
  const [auditLogs, setAuditLogs] = useState<AuditLogItem[]>([])
  const [emailNotifications, setEmailNotifications] = useState<EmailNotificationItem[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [ticketTags, setTicketTags] = useState<TicketTag[]>(demoTags)
  const [users, setUsers] = useState<CurrentUser[]>(demoUsers)
  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null)
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<TicketStatus | 'All'>('All')
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | 'All'>('All')
  const [categoryFilter, setCategoryFilter] = useState<string>('All')
  const [assignedFilter, setAssignedFilter] = useState<string>('All')
  const [tagFilter, setTagFilter] = useState<string>('All')
  const [auditEntityFilter, setAuditEntityFilter] = useState<string>('All')
  const [auditSearch, setAuditSearch] = useState('')
  const [onlyOverdue, setOnlyOverdue] = useState(false)
  const [commentBody, setCommentBody] = useState('')
  const [internalComment, setInternalComment] = useState(false)
  const [newTicketOpen, setNewTicketOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const ui = uiText[language]

  useEffect(() => {
    const handlePopState = () => {
      setViewState(viewFromPath(window.location.pathname))
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const selectedTicket = selectedTicketId ? ticketDetails[selectedTicketId] : null
  const selectedAssetBase = selectedAssetId ? assets.find((asset) => asset.id === selectedAssetId) : assets[0]
  const selectedAsset = selectedAssetBase
    ? assetDetails[selectedAssetBase.id] ?? toAssetDetails(selectedAssetBase, demoAssetHistories[selectedAssetBase.id] ?? [])
    : null
  const dashboard = useMemo(() => buildDashboard(tickets, assets), [tickets, assets])
  const filteredAuditLogs = useMemo(() => {
    const normalizedSearch = auditSearch.trim().toLowerCase()
    return auditLogs
      .filter((log) => auditEntityFilter === 'All' || log.entityType === auditEntityFilter)
      .filter((log) => {
        return (
          normalizedSearch.length === 0 ||
          log.action.toLowerCase().includes(normalizedSearch) ||
          log.actor.toLowerCase().includes(normalizedSearch) ||
          log.actorEmail.toLowerCase().includes(normalizedSearch) ||
          (log.oldValue ?? '').toLowerCase().includes(normalizedSearch) ||
          (log.newValue ?? '').toLowerCase().includes(normalizedSearch)
        )
      })
      .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
  }, [auditEntityFilter, auditLogs, auditSearch])
  const filteredTickets = useMemo(() => {
    return tickets
      .filter((ticket) => statusFilter === 'All' || ticket.status === statusFilter)
      .filter((ticket) => priorityFilter === 'All' || ticket.priority === priorityFilter)
      .filter((ticket) => categoryFilter === 'All' || ticket.category === categoryFilter)
      .filter((ticket) => tagFilter === 'All' || ticket.tags.some((tag) => tag.id === tagFilter))
      .filter((ticket) => {
        if (assignedFilter === 'All') {
          return true
        }

        if (assignedFilter === 'Unassigned') {
          return ticket.assignedTo === null
        }

        return ticket.assignedTo === assignedFilter
      })
      .filter((ticket) => !onlyOverdue || ticket.isOverdue)
      .filter((ticket) => {
        const normalized = search.trim().toLowerCase()
        return (
          normalized.length === 0 ||
          ticket.title.toLowerCase().includes(normalized) ||
          ticket.reference.toLowerCase().includes(normalized) ||
          ticket.category.toLowerCase().includes(normalized)
        )
      })
      .sort((left, right) => priorityOrder[right.priority] - priorityOrder[left.priority])
  }, [assignedFilter, categoryFilter, onlyOverdue, priorityFilter, search, statusFilter, tagFilter, tickets])

  function navigateToView(nextView: View, options: { replace?: boolean } = {}) {
    setViewState(nextView)
    const nextPath = viewPaths[nextView]
    if (window.location.pathname === nextPath) {
      return
    }

    const method = options.replace ? 'replaceState' : 'pushState'
    window.history[method](null, '', nextPath)
  }

  function viewAfterLogin() {
    const currentPath = window.location.pathname
    return currentPath === '/' || currentPath === '/login' ? 'dashboard' : viewFromPath(currentPath)
  }

  function navigateAfterLogin(user: CurrentUser) {
    const requestedView = viewAfterLogin()
    const adminViews: View[] = ['users', 'categories', 'audit', 'notifications']
    const nextView = adminViews.includes(requestedView) && user.role !== 'Admin' ? 'dashboard' : requestedView
    navigateToView(nextView, { replace: true })
  }

  async function apiRequest<T>(path: string, options: RequestInit = {}) {
    const response = await fetch(`${API_BASE}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...options.headers,
      },
    })

    if (!response.ok) {
      throw new Error(`API ${response.status}`)
    }

    return (await response.json()) as T
  }

  async function login(email: string, password: string) {
    setIsLoading(true)
    setMessage(null)
    try {
      const response = await fetch(`${API_BASE}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })

      if (!response.ok) {
        throw new Error(`Login ${response.status}`)
      }

      const payload = (await response.json()) as { token: string; user: CurrentUser }
      setToken(payload.token)
      setDemoMode(false)
      await loadRemoteData(payload.token, payload.user)
      setCurrentUser(payload.user)
      navigateAfterLogin(payload.user)
    } catch {
      const demoUser = demoUsers.find((user) => user.email === email) ?? demoUsers[2]
      setToken('demo-token')
      setDemoMode(true)
      loadDemoData(demoUser)
      setCurrentUser(demoUser)
      navigateAfterLogin(demoUser)
      setMessage(ui.messages.apiDemo)
    } finally {
      setIsLoading(false)
    }
  }

  async function loadRemoteData(authToken: string, user: CurrentUser) {
    const headers = { Authorization: `Bearer ${authToken}` }
    const [remoteTickets, remoteAssets, remoteCategories, remoteTags, remoteUsers, remoteAuditLogs, remoteEmailNotifications] = await Promise.all([
      fetch(`${API_BASE}/api/tickets`, { headers }).then((response) => response.json()),
      fetch(`${API_BASE}/api/assets`, { headers }).then((response) => response.json()),
      fetch(`${API_BASE}/api/tickets/${user.role === 'Admin' ? 'categories/manage' : 'categories'}`, { headers }).then((response) => response.json()),
      fetch(`${API_BASE}/api/tickets/tags`, { headers }).then((response) => (response.ok ? response.json() : [])),
      user.role === 'Admin'
        ? fetch(`${API_BASE}/api/users`, { headers }).then((response) => (response.ok ? response.json() : demoUsers))
        : Promise.resolve(demoUsers),
      user.role === 'Admin'
        ? fetch(`${API_BASE}/api/audit?take=250`, { headers }).then((response) => (response.ok ? response.json() : []))
        : Promise.resolve([]),
      user.role === 'Admin'
        ? fetch(`${API_BASE}/api/notifications/email`, { headers }).then((response) => (response.ok ? response.json() : []))
        : Promise.resolve([]),
    ])

    const ticketRows = (remoteTickets as TicketListItem[]).map(normalizeTicketListItem)
    const assetRows = remoteAssets as Asset[]
    setTickets(ticketRows)
    setAssets(assetRows)
    setCategories(remoteCategories as Category[])
    setTicketTags((remoteTags as TicketTag[]).length > 0 ? (remoteTags as TicketTag[]) : demoTags)
    setUsers(remoteUsers as CurrentUser[])
    setAuditLogs(remoteAuditLogs as AuditLogItem[])
    setEmailNotifications(remoteEmailNotifications as EmailNotificationItem[])
    if (ticketRows[0]) {
      setSelectedTicketId(ticketRows[0].id)
      await selectTicket(ticketRows[0].id, authToken)
    }
    if (assetRows[0]) {
      await selectAsset(assetRows[0].id, authToken)
    }
  }

  function loadDemoData(user: CurrentUser) {
    const visibleTickets = (
      user.role === 'Employee'
        ? demoTicketDetails.filter((ticket) => ticket.createdBy === user.fullName)
        : demoTicketDetails
    ).map((ticket) =>
      user.role === 'Employee'
        ? {
            ...ticket,
            comments: ticket.comments.filter((comment) => !comment.isInternal),
          }
        : ticket,
    )

    const visibleAssets =
      user.role === 'Employee'
        ? demoAssets.filter((asset) => asset.assignedToUserId === user.id)
        : demoAssets

    setTickets(visibleTickets.map(toListItem))
    setTicketDetails(Object.fromEntries(visibleTickets.map((ticket) => [ticket.id, ticket])))
    setAssets(visibleAssets)
    setAssetDetails(Object.fromEntries(visibleAssets.map((asset) => [asset.id, toAssetDetails(asset, demoAssetHistories[asset.id] ?? [])])))
    setCategories(demoCategories)
    setTicketTags(demoTags)
    setUsers(demoUsers)
    setAuditLogs(user.role === 'Admin' ? buildDemoAuditLogs() : [])
    setEmailNotifications(user.role === 'Admin' ? buildDemoEmailNotifications() : [])
    setSelectedTicketId(visibleTickets[0]?.id ?? null)
    setSelectedAssetId(visibleAssets[0]?.id ?? null)
  }

  async function selectTicket(ticketId: string, authToken = token) {
    setSelectedTicketId(ticketId)
    if (demoMode || !authToken) {
      return
    }

    const response = await fetch(`${API_BASE}/api/tickets/${ticketId}`, {
      headers: { Authorization: `Bearer ${authToken}` },
    })
    if (response.ok) {
      const details = normalizeTicketDetails((await response.json()) as ApiTicketDetails)
      setTicketDetails((current) => ({ ...current, [ticketId]: details }))
    }
  }

  async function selectAsset(assetId: string, authToken = token) {
    setSelectedAssetId(assetId)
    if (demoMode || !authToken) {
      return
    }

    const response = await fetch(`${API_BASE}/api/assets/${assetId}`, {
      headers: { Authorization: `Bearer ${authToken}` },
    })
    if (response.ok) {
      const details = (await response.json()) as AssetDetails
      setAssetDetails((current) => ({ ...current, [assetId]: details }))
    }
  }

  async function refreshAuditLogs(authToken = token) {
    if (demoMode || currentUser?.role !== 'Admin' || !authToken) {
      return
    }

    const response = await fetch(`${API_BASE}/api/audit?take=250`, {
      headers: { Authorization: `Bearer ${authToken}` },
    })
    if (response.ok) {
      setAuditLogs((await response.json()) as AuditLogItem[])
    }
  }

  async function refreshEmailNotifications(authToken = token) {
    if (demoMode || currentUser?.role !== 'Admin' || !authToken) {
      return
    }

    const response = await fetch(`${API_BASE}/api/notifications/email`, {
      headers: { Authorization: `Bearer ${authToken}` },
    })
    if (response.ok) {
      setEmailNotifications((await response.json()) as EmailNotificationItem[])
    }
  }

  async function exportDashboardReport(format: 'csv' | 'pdf') {
    if (demoMode || !token) {
      if (format === 'csv') {
        downloadCsv('helpdesk-dashboard-demo.csv', buildDashboardCsv(dashboard, ui))
        setMessage(ui.messages.demoCsvExported)
        return
      }

      setMessage(ui.messages.pdfNeedsApi)
      return
    }

    const response = await fetch(`${API_BASE}/api/reports/dashboard.${format}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (!response.ok) {
      setMessage(ui.messages.reportFailed(response.status))
      return
    }

    downloadBlob(`helpdesk-dashboard.${format}`, await response.blob())
    setMessage(ui.messages.dashboardExported(format))
  }

  function appendDemoAuditLog(entityType: string, entityId: string, action: string, oldValue: string | null, newValue: string | null) {
    if (!demoMode || !currentUser) {
      return
    }

    setAuditLogs((current) => [
      {
        id: crypto.randomUUID(),
        entityType,
        entityId,
        action,
        oldValue,
        newValue,
        userId: currentUser.id,
        actor: currentUser.fullName,
        actorEmail: currentUser.email,
        createdAt: new Date().toISOString(),
      },
      ...current,
    ])
  }

  async function changeStatus(ticketId: string, status: TicketStatus) {
    const previousStatus = ticketDetails[ticketId]?.status ?? null
    if (!demoMode) {
      const updated = normalizeTicketDetails(await apiRequest<ApiTicketDetails>(`/api/tickets/${ticketId}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status }),
      }))
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      return
    }

    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      status,
      updatedAt: new Date().toISOString(),
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Status changed',
          oldValue: ticket.status,
          newValue: status,
          actor: currentUser?.fullName ?? 'Demo user',
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Status changed', previousStatus, status)
  }

  async function assignToMe(ticketId: string) {
    if (!currentUser) {
      return
    }

    if (!demoMode) {
      const updated = normalizeTicketDetails(await apiRequest<ApiTicketDetails>(`/api/tickets/${ticketId}/assign`, {
        method: 'PATCH',
        body: JSON.stringify({ technicianId: currentUser.id }),
      }))
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      return
    }

    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      status: ticket.status === 'New' ? 'Assigned' : ticket.status,
      assignedTo: currentUser.fullName,
      updatedAt: new Date().toISOString(),
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Assigned technician changed',
          oldValue: ticket.assignedTo,
          newValue: currentUser.fullName,
          actor: currentUser.fullName,
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Assigned technician changed', null, currentUser.fullName)
  }

  async function changePriority(ticketId: string, priority: TicketPriority) {
    const previousPriority = ticketDetails[ticketId]?.priority ?? null
    if (!demoMode) {
      const updated = normalizeTicketDetails(await apiRequest<ApiTicketDetails>(`/api/tickets/${ticketId}/priority`, {
        method: 'PATCH',
        body: JSON.stringify({ priority }),
      }))
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      return
    }

    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      priority,
      updatedAt: new Date().toISOString(),
      slaDueAt: calculateSla(new Date(ticket.createdAt), priority).toISOString(),
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Priority changed',
          oldValue: ticket.priority,
          newValue: priority,
          actor: currentUser?.fullName ?? 'Demo user',
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Priority changed', previousPriority, priority)
  }

  async function linkAsset(ticketId: string, assetId: string) {
    const asset = assets.find((item) => item.id === assetId)
    if (!asset) {
      return
    }

    if (!demoMode) {
      const updated = normalizeTicketDetails(await apiRequest<ApiTicketDetails>(`/api/tickets/${ticketId}/asset`, {
        method: 'PATCH',
        body: JSON.stringify({ assetId }),
      }))
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      return
    }

    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      relatedAssetId: asset.id,
      relatedAssetTag: asset.assetTag,
      updatedAt: new Date().toISOString(),
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Asset linked to ticket',
          oldValue: ticket.relatedAssetTag,
          newValue: asset.assetTag,
          actor: currentUser?.fullName ?? 'Demo user',
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Asset linked to ticket', null, asset.assetTag)
  }

  async function updateTicketTag(ticketId: string, tagId: string) {
    const selectedTags = tagId ? ticketTags.filter((tag) => tag.id === tagId) : []

    if (!demoMode) {
      const updated = normalizeTicketDetails(await apiRequest<ApiTicketDetails>(`/api/tickets/${ticketId}/tags`, {
        method: 'PATCH',
        body: JSON.stringify({ tagIds: selectedTags.map((tag) => tag.id) }),
      }))
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      await refreshAuditLogs()
      return
    }

    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      tags: selectedTags,
      updatedAt: new Date().toISOString(),
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Tags updated',
          oldValue: formatRawTagList(ticket.tags),
          newValue: formatRawTagList(selectedTags),
          actor: currentUser?.fullName ?? 'Demo user',
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Tags updated', null, formatRawTagList(selectedTags))
  }

  async function addComment(ticketId: string) {
    if (!commentBody.trim() || !currentUser) {
      return
    }

    const body = commentBody.trim()
    setCommentBody('')
    if (!demoMode) {
      const updated = normalizeTicketDetails(await apiRequest<ApiTicketDetails>(`/api/tickets/${ticketId}/comments`, {
        method: 'POST',
        body: JSON.stringify({ body, isInternal: internalComment }),
      }))
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      return
    }

    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      updatedAt: new Date().toISOString(),
      comments: [
        ...ticket.comments,
        {
          id: crypto.randomUUID(),
          author: currentUser.fullName,
          body,
          isInternal: internalComment,
          createdAt: new Date().toISOString(),
        },
      ],
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Comment added',
          oldValue: null,
          newValue: internalComment ? 'Internal comment' : 'Public comment',
          actor: currentUser.fullName,
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Comment added', null, internalComment ? 'Internal comment' : 'Public comment')
  }

  async function addAttachment(ticketId: string, file: File | null) {
    if (!file || !currentUser) {
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setMessage(ui.messages.attachmentTooLarge)
      return
    }

    if (!demoMode) {
      const form = new FormData()
      form.append('file', file)
      const response = await fetch(`${API_BASE}/api/tickets/${ticketId}/attachments`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
        body: form,
      })
      if (!response.ok) {
        setMessage(ui.messages.attachmentUploadFailed)
        return
      }

      const updated = normalizeTicketDetails((await response.json()) as ApiTicketDetails)
      setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
      setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
      await refreshAuditLogs()
      return
    }

    const createdAt = new Date().toISOString()
    updateTicket(ticketId, (ticket) => ({
      ...ticket,
      updatedAt: createdAt,
      attachments: [
        ...ticket.attachments,
        {
          id: crypto.randomUUID(),
          fileName: file.name,
          contentType: file.type || 'application/octet-stream',
          size: file.size,
          uploadedBy: currentUser.fullName,
          createdAt,
        },
      ],
      history: [
        ...ticket.history,
        {
          id: crypto.randomUUID(),
          action: 'Attachment added',
          oldValue: null,
          newValue: file.name,
          actor: currentUser.fullName,
          createdAt,
        },
      ],
    }))
    appendDemoAuditLog('Ticket', ticketId, 'Attachment added', null, file.name)
  }

  async function downloadAttachment(ticketId: string, attachment: TicketAttachment) {
    if (!demoMode) {
      const response = await fetch(`${API_BASE}/api/tickets/${ticketId}/attachments/${attachment.id}`, {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      })
      if (!response.ok) {
        setMessage(ui.messages.attachmentDownloadFailed)
        return
      }

      downloadBlob(attachment.fileName, await response.blob())
      return
    }

    downloadBlob(attachment.fileName, new Blob([`Demo attachment: ${attachment.fileName}`], { type: attachment.contentType }))
  }

  async function createAsset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formElement = event.currentTarget
    const form = new FormData(formElement)
    const assetTag = String(form.get('assetTag') ?? '').trim().toUpperCase()
    const type = String(form.get('type') ?? 'Laptop') as AssetType
    const brand = String(form.get('brand') ?? '').trim()
    const model = String(form.get('model') ?? '').trim()
    const serialNumber = String(form.get('serialNumber') ?? '').trim()
    const purchaseDate = String(form.get('purchaseDate') ?? '')
    const warrantyUntil = String(form.get('warrantyUntil') ?? '')
    const notes = String(form.get('notes') ?? '').trim()

    if (!assetTag || !brand || !model || !serialNumber || !purchaseDate || !warrantyUntil) {
      return
    }

    if (!demoMode) {
      const created = await apiRequest<Asset>('/api/assets', {
        method: 'POST',
        body: JSON.stringify({
          assetTag,
          type,
          brand,
          model,
          serialNumber,
          purchaseDate,
          warrantyUntil,
          notes: notes.length > 0 ? notes : null,
        }),
      })
      setAssets((current) => [created, ...current])
      await selectAsset(created.id)
      formElement.reset()
      return
    }

    if (assets.some((asset) => asset.assetTag === assetTag || asset.serialNumber === serialNumber)) {
      setMessage(ui.messages.duplicateAsset)
      return
    }

    const created: Asset = {
      id: crypto.randomUUID(),
      assetTag,
      type,
      brand,
      model,
      serialNumber,
      status: 'Available',
      assignedToUserId: null,
      assignedTo: null,
      purchaseDate,
      warrantyUntil,
      notes: notes.length > 0 ? notes : null,
    }

    setAssets((current) => [created, ...current])
    setAssetDetails((current) => ({ ...current, [created.id]: toAssetDetails(created) }))
    setSelectedAssetId(created.id)
    appendDemoAuditLog('Asset', created.id, 'Asset created', null, created.assetTag)
    formElement.reset()
  }

  async function importAssetsCsv(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) {
      return
    }

    let rows: AssetCsvRow[]
    try {
      rows = parseAssetCsv(await file.text())
    } catch {
      setMessage(ui.messages.csvImportFailed)
      return
    }

    if (rows.length === 0) {
      setMessage(ui.messages.csvEmpty)
      return
    }

    if (!demoMode) {
      try {
        const createdAssets: Asset[] = []
        for (const row of rows) {
          const created = await apiRequest<Asset>('/api/assets', {
            method: 'POST',
            body: JSON.stringify(row),
          })
          createdAssets.push(created)
        }

        setAssets((current) => [...createdAssets, ...current])
        if (createdAssets[0]) {
          await selectAsset(createdAssets[0].id)
          await refreshAuditLogs()
        }
        setMessage(ui.messages.assetsImported(createdAssets.length))
      } catch {
        setMessage(ui.messages.csvImportStopped)
      }
      return
    }

    const existingTags = new Set(assets.map((asset) => asset.assetTag))
    const existingSerials = new Set(assets.map((asset) => asset.serialNumber))
    for (const row of rows) {
      if (existingTags.has(row.assetTag) || existingSerials.has(row.serialNumber)) {
        setMessage(ui.messages.csvDuplicateDemo)
        return
      }

      existingTags.add(row.assetTag)
      existingSerials.add(row.serialNumber)
    }

    const importedAssets: Asset[] = rows.map((row) => ({
      id: crypto.randomUUID(),
      ...row,
      status: 'Available',
      assignedToUserId: null,
      assignedTo: null,
    }))

    setAssets((current) => [...importedAssets, ...current])
    setAssetDetails((current) => ({
      ...current,
      ...Object.fromEntries(importedAssets.map((asset) => [asset.id, toAssetDetails(asset)])),
    }))
    setSelectedAssetId(importedAssets[0]?.id ?? null)
    importedAssets.forEach((asset) => appendDemoAuditLog('Asset', asset.id, 'Asset created', null, asset.assetTag))
    setMessage(ui.messages.assetsImported(importedAssets.length))
  }

  function exportAssetsCsv() {
    downloadCsv('helpdesk-assets.csv', buildAssetsCsv(assets))
    setMessage(ui.messages.assetsExported(assets.length))
  }

  async function changeAssetStatus(assetId: string, status: AssetStatus) {
    const asset = assets.find((candidate) => candidate.id === assetId)
    const previousStatus = asset?.status ?? null
    if (status === 'Assigned' && !asset?.assignedToUserId) {
      setMessage(ui.messages.assignBeforeAssigned)
      return
    }

    if (!demoMode) {
      const updated = await apiRequest<Asset>(`/api/assets/${assetId}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status }),
      })
      setAssets((current) => current.map((asset) => (asset.id === assetId ? updated : asset)))
      await selectAsset(assetId)
      return
    }

    const changedAt = new Date().toISOString()
    setAssets((current) =>
      current.map((asset) =>
        asset.id === assetId
          ? {
              ...asset,
              status,
              assignedToUserId: status === 'Available' ? null : asset.assignedToUserId,
              assignedTo: status === 'Available' ? null : asset.assignedTo,
            }
          : asset,
      ),
    )
    setAssetDetails((current) => {
      const existing = current[assetId]
      if (!existing) {
        return current
      }

      const assignmentHistory =
        status === 'Available'
          ? existing.assignmentHistory.map((assignment) =>
              assignment.returnedAt === null ? { ...assignment, returnedAt: changedAt } : assignment,
            )
          : existing.assignmentHistory

      return {
        ...current,
        [assetId]: {
          ...existing,
          status,
          assignedToUserId: status === 'Available' ? null : existing.assignedToUserId,
          assignedTo: status === 'Available' ? null : existing.assignedTo,
          assignmentHistory,
        },
      }
    })
    appendDemoAuditLog('Asset', assetId, 'Asset status changed', previousStatus, status)
  }

  async function assignAsset(assetId: string, userId: string) {
    const user = users.find((candidate) => candidate.id === userId)
    if (!user) {
      return
    }

    if (!demoMode) {
      const updated = await apiRequest<Asset>(`/api/assets/${assetId}/assign`, {
        method: 'PATCH',
        body: JSON.stringify({ userId }),
      })
      setAssets((current) => current.map((asset) => (asset.id === assetId ? updated : asset)))
      await selectAsset(assetId)
      return
    }

    const existingAsset = assets.find((asset) => asset.id === assetId)
    if (existingAsset?.assignedToUserId && existingAsset.assignedToUserId !== userId) {
      setMessage(ui.messages.assetAlreadyAssigned)
      return
    }

    const assignedAt = new Date().toISOString()
    setAssets((current) =>
      current.map((asset) =>
        asset.id === assetId
          ? {
              ...asset,
              status: 'Assigned',
              assignedToUserId: user.id,
              assignedTo: user.fullName,
            }
          : asset,
      ),
    )
    setAssetDetails((current) => {
      const existing = current[assetId] ?? (existingAsset ? toAssetDetails(existingAsset) : null)
      if (!existing) {
        return current
      }

      return {
        ...current,
        [assetId]: {
          ...existing,
          status: 'Assigned',
          assignedToUserId: user.id,
          assignedTo: user.fullName,
          assignmentHistory: [
            {
              id: crypto.randomUUID(),
              userId: user.id,
              user: user.fullName,
              assignedByUserId: currentUser?.id ?? demoUsers[2].id,
              assignedBy: currentUser?.fullName ?? demoUsers[2].fullName,
              assignedAt,
              returnedAt: null,
            },
            ...existing.assignmentHistory,
          ],
        },
      }
    })
    appendDemoAuditLog('Asset', assetId, 'Asset assigned', existingAsset?.assignedTo ?? null, user.fullName)
  }

  async function createUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formElement = event.currentTarget
    const form = new FormData(formElement)
    const fullName = String(form.get('fullName') ?? '').trim()
    const email = String(form.get('email') ?? '').trim().toLowerCase()
    const password = String(form.get('password') ?? '').trim()
    const role = String(form.get('role') ?? 'Employee') as UserRole
    const department = String(form.get('department') ?? '').trim()

    if (!fullName || !email || !password || !department) {
      return
    }

    if (!demoMode) {
      const created = await apiRequest<CurrentUser>('/api/users', {
        method: 'POST',
        body: JSON.stringify({ fullName, email, password, role, department }),
      })
      setUsers((current) => [created, ...current])
      formElement.reset()
      return
    }

    if (users.some((user) => user.email === email)) {
      setMessage(ui.messages.userExists)
      return
    }

    setUsers((current) => [
      {
        id: crypto.randomUUID(),
        fullName,
        email,
        role,
        department,
        isActive: true,
      },
      ...current,
    ])
    formElement.reset()
  }

  async function changeUserRole(userId: string, role: UserRole) {
    if (!demoMode) {
      const updated = await apiRequest<CurrentUser>(`/api/users/${userId}/role`, {
        method: 'PATCH',
        body: JSON.stringify({ role }),
      })
      setUsers((current) => current.map((user) => (user.id === userId ? updated : user)))
      return
    }

    setUsers((current) => current.map((user) => (user.id === userId ? { ...user, role } : user)))
  }

  async function createCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formElement = event.currentTarget
    const form = new FormData(formElement)
    const name = String(form.get('name') ?? '').trim()
    const description = String(form.get('description') ?? '').trim()

    if (!name || !description) {
      return
    }

    if (!demoMode) {
      const created = await apiRequest<Category>('/api/tickets/categories', {
        method: 'POST',
        body: JSON.stringify({ name, description }),
      })
      setCategories((current) => [...current, created].sort((left, right) => left.name.localeCompare(right.name)))
      formElement.reset()
      return
    }

    if (categories.some((category) => category.name.toLowerCase() === name.toLowerCase())) {
      setMessage(ui.messages.categoryExists)
      return
    }

    setCategories((current) =>
      [
        ...current,
        {
          id: crypto.randomUUID(),
          name,
          description,
          isActive: true,
        },
      ].sort((left, right) => left.name.localeCompare(right.name)),
    )
    formElement.reset()
  }

  async function updateCategory(event: FormEvent<HTMLFormElement>, categoryId: string) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const name = String(form.get('name') ?? '').trim()
    const description = String(form.get('description') ?? '').trim()
    const isActive = form.get('isActive') === 'on'

    if (!name || !description) {
      return
    }

    if (!demoMode) {
      const updated = await apiRequest<Category>(`/api/tickets/categories/${categoryId}`, {
        method: 'PUT',
        body: JSON.stringify({ name, description, isActive }),
      })
      setCategories((current) => current.map((category) => (category.id === categoryId ? updated : category)))
      return
    }

    setCategories((current) =>
      current.map((category) =>
        category.id === categoryId
          ? {
              ...category,
              name,
              description,
              isActive,
            }
          : category,
      ),
    )
  }

  async function createTicket(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!currentUser) {
      return
    }

    const formElement = event.currentTarget
    const form = new FormData(formElement)
    const categoryId = String(form.get('category') ?? categories[0]?.id)
    const category = categories.find((item) => item.id === categoryId)?.name ?? 'Hardware'
    const priority = String(form.get('priority') ?? 'Medium') as TicketPriority
    const title = String(form.get('title') ?? '').trim()
    const description = String(form.get('description') ?? '').trim()
    if (!title || !description) {
      return
    }

    if (!demoMode) {
      const created = normalizeTicketDetails(await apiRequest<ApiTicketDetails>('/api/tickets', {
        method: 'POST',
        body: JSON.stringify({
          title,
          description,
          categoryId,
          priority,
          relatedAssetId: null,
        }),
      }))
      setTicketDetails((current) => ({ ...current, [created.id]: created }))
      setTickets((current) => [toListItem(created), ...current])
      setSelectedTicketId(created.id)
      setNewTicketOpen(false)
      formElement.reset()
      return
    }

    const createdAt = new Date()
    const newTicket: TicketDetails = {
      id: crypto.randomUUID(),
      reference: `HD-${String(tickets.length + 1).padStart(5, '0')}`,
      title,
      description,
      category,
      priority,
      status: 'New',
      createdBy: currentUser.fullName,
      assignedTo: null,
      relatedAssetId: null,
      relatedAssetTag: null,
      createdAt: createdAt.toISOString(),
      updatedAt: createdAt.toISOString(),
      slaDueAt: calculateSla(createdAt, priority).toISOString(),
      isOverdue: false,
      tags: [],
      comments: [],
      attachments: [],
      history: [
        {
          id: crypto.randomUUID(),
          action: 'Ticket created',
          oldValue: null,
          newValue: priority,
          actor: currentUser.fullName,
          createdAt: createdAt.toISOString(),
        },
      ],
    }

    setTicketDetails((current) => ({ ...current, [newTicket.id]: newTicket }))
    setTickets((current) => [toListItem(newTicket), ...current])
    setSelectedTicketId(newTicket.id)
    setNewTicketOpen(false)
    appendDemoAuditLog('Ticket', newTicket.id, 'Ticket created', null, priority)
    formElement.reset()
  }

  function updateTicket(ticketId: string, updater: (ticket: TicketDetails) => TicketDetails) {
    const existing = ticketDetails[ticketId]
    if (!existing) {
      return
    }

    const updated = updater(existing)
    setTicketDetails((current) => ({ ...current, [ticketId]: updated }))
    setTickets((current) => current.map((ticket) => (ticket.id === ticketId ? toListItem(updated) : ticket)))
  }

  function logout() {
    setToken(null)
    setCurrentUser(null)
    setViewState('dashboard')
    window.history.replaceState(null, '', '/login')
    setTickets([])
    setAssets([])
    setAssetDetails({})
    setAuditLogs([])
    setEmailNotifications([])
    setTicketTags(demoTags)
    setUsers(demoUsers)
    setTicketDetails({})
    setSelectedTicketId(null)
    setSelectedAssetId(null)
    setAuditEntityFilter('All')
    setAuditSearch('')
    setTagFilter('All')
    setMessage(null)
  }

  if (!currentUser || !token) {
    return <LoginScreen isLoading={isLoading} language={language} ui={ui} onLanguageChange={setLanguage} onLogin={login} />
  }

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <nav className="nav-list">
          <NavButton icon={<LayoutDashboard />} label={ui.nav.dashboard} testId="nav-dashboard" active={view === 'dashboard'} onClick={() => navigateToView('dashboard')} />
          <NavButton icon={<TicketIcon />} label={ui.nav.tickets} testId="nav-tickets" active={view === 'tickets'} onClick={() => navigateToView('tickets')} />
          <NavButton icon={<HardDrive />} label={ui.nav.assets} testId="nav-assets" active={view === 'assets'} onClick={() => navigateToView('assets')} />
          <NavButton icon={<Users />} label={ui.nav.users} testId="nav-users" active={view === 'users'} onClick={() => navigateToView('users')} disabled={currentUser.role !== 'Admin'} />
          <NavButton icon={<Tags />} label={ui.nav.categories} testId="nav-categories" active={view === 'categories'} onClick={() => navigateToView('categories')} disabled={currentUser.role !== 'Admin'} />
          <NavButton
            icon={<ShieldCheck />}
            label={ui.nav.audit}
            testId="nav-audit"
            active={view === 'audit'}
            onClick={() => {
              navigateToView('audit')
              void refreshAuditLogs()
            }}
            disabled={currentUser.role !== 'Admin'}
          />
          <NavButton
            icon={<Mail />}
            label={ui.nav.notifications}
            testId="nav-outbox"
            active={view === 'notifications'}
            onClick={() => {
              navigateToView('notifications')
              void refreshEmailNotifications()
            }}
            disabled={currentUser.role !== 'Admin'}
          />
        </nav>

        <div className="sidebar-footer">
          <div className="role-card">
            <ShieldCheck size={18} />
            <div>
              <strong>{ui.roles[currentUser.role]}</strong>
              <span>{formatDepartmentName(currentUser.department, ui)}</span>
            </div>
          </div>
          <button className="icon-text-button ghost" type="button" onClick={logout}>
            <LogOut size={17} />
            {ui.common.signOut}
          </button>
        </div>
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div>
            <h1>{viewTitle(view, ui)}</h1>
          </div>
          <div className="topbar-actions">
            {message && <span className="status-pill demo">{message}</span>}
            {demoMode && <span className="status-pill">{ui.common.demoMode}</span>}
            <LanguageToggle language={language} ui={ui} onChange={setLanguage} />
            <div className="user-chip">
              <span>{currentUser.fullName}</span>
              <strong>{currentUser.email}</strong>
            </div>
          </div>
        </header>

        {view === 'dashboard' && (
          <DashboardView
            canExportReports={currentUser.role === 'Admin'}
            dashboard={dashboard}
            tickets={tickets}
            ui={ui}
            onExportReport={exportDashboardReport}
          />
        )}
        {view === 'tickets' && (
          <TicketsView
            canCreate={currentUser.role !== 'Technician'}
            canWork={currentUser.role !== 'Employee'}
            assets={assets}
            categories={categories}
            categoryFilter={categoryFilter}
            commentBody={commentBody}
            assignedFilter={assignedFilter}
            filteredTickets={filteredTickets}
            internalComment={internalComment}
            newTicketOpen={newTicketOpen}
            onlyOverdue={onlyOverdue}
            priorityFilter={priorityFilter}
            search={search}
            selectedTicket={selectedTicket}
            statusFilter={statusFilter}
            tagFilter={tagFilter}
            ticketTags={ticketTags}
            language={language}
            ui={ui}
            users={users}
            onAddComment={addComment}
            onAddAttachment={addAttachment}
            onAssignToMe={assignToMe}
            onChangePriority={changePriority}
            onChangeStatus={changeStatus}
            onCreateTicket={createTicket}
            onDownloadAttachment={downloadAttachment}
            onLinkAsset={linkAsset}
            onSelectTicket={selectTicket}
            onSetAssignedFilter={setAssignedFilter}
            onSetCategoryFilter={setCategoryFilter}
            onSetCommentBody={setCommentBody}
            onSetInternalComment={setInternalComment}
            onSetNewTicketOpen={setNewTicketOpen}
            onSetOnlyOverdue={setOnlyOverdue}
            onSetPriorityFilter={setPriorityFilter}
            onSetSearch={setSearch}
            onSetStatusFilter={setStatusFilter}
            onSetTagFilter={setTagFilter}
            onUpdateTicketTag={updateTicketTag}
          />
        )}
        {view === 'assets' && (
          <AssetsView
            assets={assets}
            canChangeStatus={currentUser.role !== 'Employee'}
            canManage={currentUser.role === 'Admin'}
            language={language}
            selectedAsset={selectedAsset}
            ui={ui}
            users={users}
            onAssignAsset={assignAsset}
            onChangeAssetStatus={changeAssetStatus}
            onCreateAsset={createAsset}
            onExportAssetsCsv={exportAssetsCsv}
            onImportAssetsCsv={importAssetsCsv}
            onSelectAsset={selectAsset}
          />
        )}
        {view === 'users' && <UsersView ui={ui} users={users} onChangeUserRole={changeUserRole} onCreateUser={createUser} />}
        {view === 'categories' && <CategoriesView categories={categories} ui={ui} onCreateCategory={createCategory} onUpdateCategory={updateCategory} />}
        {view === 'audit' && (
          <AuditView
            entityFilter={auditEntityFilter}
            language={language}
            logs={filteredAuditLogs}
            search={auditSearch}
            ui={ui}
            onSetEntityFilter={setAuditEntityFilter}
            onSetSearch={setAuditSearch}
          />
        )}
        {view === 'notifications' && <NotificationsView language={language} notifications={emailNotifications} ui={ui} onRefresh={() => refreshEmailNotifications()} />}
      </main>
    </div>
  )
}

function parseAssetCsv(source: string): AssetCsvRow[] {
  const records = parseCsvRecords(source).filter((row) => row.some((cell) => cell.trim().length > 0))
  if (records.length === 0) {
    return []
  }

  const headers = records[0].map((header) => normalizeCsvHeader(header))
  const indexOf = (name: string) => headers.indexOf(name)
  const requiredHeaders = ['assettag', 'type', 'brand', 'model', 'serialnumber', 'purchasedate', 'warrantyuntil']
  if (requiredHeaders.some((header) => indexOf(header) < 0)) {
    throw new Error('Missing required CSV header.')
  }

  return records.slice(1).map((row) => {
    const value = (name: string) => row[indexOf(name)]?.trim() ?? ''
    const type = parseAssetType(value('type'))
    const assetTag = value('assettag').toUpperCase()
    const brand = value('brand')
    const model = value('model')
    const serialNumber = value('serialnumber')
    const purchaseDate = value('purchasedate')
    const warrantyUntil = value('warrantyuntil')
    const notesIndex = indexOf('notes')
    const notes = notesIndex >= 0 ? row[notesIndex]?.trim() ?? '' : ''

    if (!assetTag || !brand || !model || !serialNumber || !purchaseDate || !warrantyUntil) {
      throw new Error('Missing required CSV value.')
    }

    return {
      assetTag,
      type,
      brand,
      model,
      serialNumber,
      purchaseDate,
      warrantyUntil,
      notes: notes.length > 0 ? notes : null,
    }
  })
}

function parseCsvRecords(source: string): string[][] {
  const records: string[][] = []
  let row: string[] = []
  let field = ''
  let quoted = false

  for (let index = 0; index < source.length; index += 1) {
    const char = source[index]
    const next = source[index + 1]

    if (char === '"' && quoted && next === '"') {
      field += '"'
      index += 1
      continue
    }

    if (char === '"') {
      quoted = !quoted
      continue
    }

    if (char === ',' && !quoted) {
      row.push(field)
      field = ''
      continue
    }

    if ((char === '\n' || char === '\r') && !quoted) {
      if (char === '\r' && next === '\n') {
        index += 1
      }
      row.push(field)
      records.push(row)
      row = []
      field = ''
      continue
    }

    field += char
  }

  row.push(field)
  records.push(row)
  return records
}

function normalizeCsvHeader(header: string) {
  return header.trim().toLowerCase().replaceAll(' ', '').replaceAll('_', '').replaceAll('-', '')
}

function parseAssetType(value: string): AssetType {
  const type = assetTypes.find((item) => item.toLowerCase() === value.trim().toLowerCase())
  if (!type) {
    throw new Error('Unknown asset type.')
  }

  return type
}

function buildAssetsCsv(assets: Asset[]) {
  const headers = ['AssetTag', 'Type', 'Brand', 'Model', 'SerialNumber', 'Status', 'AssignedTo', 'PurchaseDate', 'WarrantyUntil', 'Notes']
  const rows = assets.map((asset) => [
    asset.assetTag,
    asset.type,
    asset.brand,
    asset.model,
    asset.serialNumber,
    asset.status,
    asset.assignedTo ?? '',
    asset.purchaseDate,
    asset.warrantyUntil,
    asset.notes ?? '',
  ])

  return [headers, ...rows].map((row) => row.map(escapeCsvValue).join(',')).join('\n')
}

function escapeCsvValue(value: string) {
  return /[",\n\r]/.test(value) ? `"${value.replaceAll('"', '""')}"` : value
}

function downloadCsv(filename: string, content: string) {
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8' })
  downloadBlob(filename, blob)
}

function downloadBlob(filename: string, blob: Blob) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

function toAssetDetails(asset: Asset, assignmentHistory: AssetAssignment[] = []): AssetDetails {
  return {
    ...asset,
    assignmentHistory,
  }
}

function buildDemoAuditLogs(): AuditLogItem[] {
  const ticketLogs = demoTicketDetails.flatMap((ticket) =>
    ticket.history.map((item) => {
      const actor = demoUsers.find((user) => user.fullName === item.actor) ?? demoUsers[2]
      return {
        id: `audit-${item.id}`,
        entityType: 'Ticket',
        entityId: ticket.id,
        action: item.action,
        oldValue: item.oldValue,
        newValue: item.newValue,
        userId: actor.id,
        actor: actor.fullName,
        actorEmail: actor.email,
        createdAt: item.createdAt,
      }
    }),
  )

  const assetLogs = Object.entries(demoAssetHistories).flatMap(([assetId, assignments]) =>
    assignments.flatMap((assignment) => {
      const logs: AuditLogItem[] = [
        {
          id: `audit-${assignment.id}-assigned`,
          entityType: 'Asset',
          entityId: assetId,
          action: 'Asset assigned',
          oldValue: null,
          newValue: assignment.user,
          userId: assignment.assignedByUserId,
          actor: assignment.assignedBy,
          actorEmail: demoUsers.find((user) => user.id === assignment.assignedByUserId)?.email ?? 'admin@test.com',
          createdAt: assignment.assignedAt,
        },
      ]

      if (assignment.returnedAt) {
        logs.push({
          id: `audit-${assignment.id}-returned`,
          entityType: 'Asset',
          entityId: assetId,
          action: 'Asset status changed',
          oldValue: 'Assigned',
          newValue: 'Available',
          userId: assignment.assignedByUserId,
          actor: assignment.assignedBy,
          actorEmail: demoUsers.find((user) => user.id === assignment.assignedByUserId)?.email ?? 'admin@test.com',
          createdAt: assignment.returnedAt,
        })
      }

      return logs
    }),
  )

  return [...ticketLogs, ...assetLogs].sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
}

function buildDemoEmailNotifications(): EmailNotificationItem[] {
  return [
    {
      id: 'email-demo-1',
      recipientUserId: demoUsers[0].id,
      toEmail: demoUsers[0].email,
      subject: 'Ticket HD-00002 status changed to Resolved',
      body: 'Ticket HD-00002 was resolved after VPN group update.',
      status: 'Pending',
      createdAt: new Date(Date.now() - 21 * 60 * 60 * 1000).toISOString(),
      processedAt: null,
      error: null,
    },
  ]
}

function buildDashboardCsv(dashboard: DashboardSummary, ui: UiText) {
  const rows = [
    ['Section', 'Name', 'Value'],
    ['Summary', ui.dashboard.openTickets, String(dashboard.openTickets)],
    ['Summary', ui.common.unassigned, String(dashboard.unassignedTickets)],
    ['Summary', ui.dashboard.overdue, String(dashboard.overdueTickets)],
    ['Summary', ui.dashboard.resolvedThisWeek, String(dashboard.resolvedThisWeek)],
    ['Summary', 'Average resolution time', dashboard.averageResolutionTime],
    ['Summary', ui.dashboard.availableAssets, String(dashboard.availableAssets)],
    ['Summary', ui.assetStatuses.InRepair, String(dashboard.assetsInRepair)],
    ...dashboard.ticketsByPriority.map((item) => [ui.dashboard.ticketsByPriority, ui.priorities[item.priority], String(item.count)]),
    ...dashboard.ticketsByCategory.map((item) => ['Tickets by category', formatCategoryName(item.category, ui), String(item.count)]),
    ...dashboard.assetsByStatus.map((item) => [ui.dashboard.assetsByStatus, ui.assetStatuses[item.status], String(item.count)]),
    ...dashboard.topUsers.map((item) => ['Top users', item.user, String(item.count)]),
  ]

  return rows.map((row) => row.map(escapeCsvValue).join(',')).join('\n')
}

function toListItem(ticket: TicketDetails): TicketListItem {
  return {
    id: ticket.id,
    reference: ticket.reference,
    title: ticket.title,
    category: ticket.category,
    priority: ticket.priority,
    status: ticket.status,
    createdBy: ticket.createdBy,
    assignedTo: ticket.assignedTo,
    relatedAssetId: ticket.relatedAssetId,
    relatedAssetTag: ticket.relatedAssetTag,
    createdAt: ticket.createdAt,
    updatedAt: ticket.updatedAt,
    slaDueAt: ticket.slaDueAt,
    isOverdue: ticket.isOverdue,
    tags: ticket.tags ?? [],
  }
}

function normalizeTicketListItem(ticket: TicketListItem): TicketListItem {
  return {
    ...ticket,
    relatedAssetId: ticket.relatedAssetId ?? null,
    relatedAssetTag: ticket.relatedAssetTag ?? null,
    tags: ticket.tags ?? [],
  }
}

function normalizeTicketDetails(ticket: ApiTicketDetails | TicketDetails): TicketDetails {
  if (isNormalizedTicketDetails(ticket)) {
    return {
      ...ticket,
      relatedAssetId: ticket.relatedAssetId ?? null,
      relatedAssetTag: ticket.relatedAssetTag ?? null,
      tags: ticket.tags ?? [],
    }
  }

  const { assignedTo, createdBy, relatedAsset, ...rest } = ticket as ApiTicketDetails
  return {
    ...rest,
    createdBy: createdBy.fullName,
    assignedTo: assignedTo?.fullName ?? null,
    relatedAssetId: relatedAsset?.id ?? null,
    relatedAssetTag: relatedAsset?.assetTag ?? null,
    tags: rest.tags ?? [],
  }
}

function isNormalizedTicketDetails(ticket: ApiTicketDetails | TicketDetails): ticket is TicketDetails {
  return typeof ticket.createdBy === 'string'
}

function buildDashboard(tickets: TicketListItem[], assets: Asset[]): DashboardSummary {
  const openStatuses: TicketStatus[] = ['New', 'Assigned', 'InProgress', 'WaitingForUser']
  const resolvedThisWeek = tickets.filter((ticket) => {
    return ticket.status === 'Resolved' && Date.now() - new Date(ticket.updatedAt).getTime() < 7 * 24 * 60 * 60 * 1000
  }).length

  return {
    openTickets: tickets.filter((ticket) => openStatuses.includes(ticket.status)).length,
    unassignedTickets: tickets.filter((ticket) => ticket.status === 'New' && !ticket.assignedTo).length,
    overdueTickets: tickets.filter((ticket) => ticket.isOverdue).length,
    resolvedThisWeek,
    averageResolutionTime: resolvedThisWeek > 0 ? '9h 45m' : 'n/a',
    availableAssets: assets.filter((asset) => asset.status === 'Available').length,
    assetsInRepair: assets.filter((asset) => asset.status === 'InRepair').length,
    ticketsByPriority: (['Critical', 'High', 'Medium', 'Low'] as TicketPriority[]).map((priority) => ({
      priority,
      count: tickets.filter((ticket) => ticket.priority === priority).length,
    })),
    ticketsByCategory: [...new Set(tickets.map((ticket) => ticket.category))].map((category) => ({
      category,
      count: tickets.filter((ticket) => ticket.category === category).length,
    })),
    assetsByStatus: (['Available', 'Assigned', 'InRepair', 'Retired', 'Lost'] as AssetStatus[]).map((status) => ({
      status,
      count: assets.filter((asset) => asset.status === status).length,
    })),
    topUsers: [...new Set(tickets.map((ticket) => ticket.createdBy))].map((user) => ({
      user,
      count: tickets.filter((ticket) => ticket.createdBy === user).length,
    })),
  }
}

function calculateSla(createdAt: Date, priority: TicketPriority) {
  const hours: Record<TicketPriority, number> = {
    Low: 72,
    Medium: 48,
    High: 24,
    Critical: 4,
  }
  return new Date(createdAt.getTime() + hours[priority] * 60 * 60 * 1000)
}

export default App
