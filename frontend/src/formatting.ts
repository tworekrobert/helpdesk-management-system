import type { Language, TicketTag, View } from './types'
import type { UiText } from './i18n'

export function formatSla(dueAt: string, overdue: boolean, ui: UiText) {
  const minutes = Math.round((new Date(dueAt).getTime() - Date.now()) / 60000)
  const absolute = Math.abs(minutes)
  const hours = Math.floor(absolute / 60)
  const rest = absolute % 60
  return `${overdue ? ui.sla.overdue : ui.sla.due} ${hours}h ${rest}m`
}

export function formatDate(value: string, language: Language) {
  return new Intl.DateTimeFormat(language === 'pl' ? 'pl-PL' : 'en', {
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    month: 'short',
  }).format(new Date(value))
}

export function formatBytes(value: number) {
  if (value < 1024) {
    return `${value} B`
  }

  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`
  }

  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

function formatFromDictionary<T extends Readonly<Record<string, string>>>(dictionary: T, value: string) {
  return dictionary[value as keyof T] ?? value
}

export function formatCategoryName(value: string, ui: UiText) {
  return formatFromDictionary(ui.categoryNames, value)
}

export function formatTagName(tag: TicketTag, ui: UiText) {
  return formatFromDictionary(ui.tagNames, tag.name)
}

export function formatTagList(tags: TicketTag[], ui: UiText) {
  return tags.length > 0 ? tags.map((tag) => formatTagName(tag, ui)).join(', ') : ui.common.noTags
}

export function formatRawTagList(tags: TicketTag[]) {
  return tags.length > 0 ? tags.map((tag) => tag.name).join(', ') : 'No tags'
}

export function formatDepartmentName(value: string, ui: UiText) {
  return formatFromDictionary(ui.departments, value)
}

export function formatAuditAction(value: string, ui: UiText) {
  return formatFromDictionary(ui.auditActions, value)
}

export function formatAuditValue(value: string | null, ui: UiText) {
  if (!value) {
    return ui.common.none
  }

  return value
    .split(', ')
    .map((part) => formatSingleAuditValue(part, ui))
    .join(', ')
}

export function formatSingleAuditValue(value: string, ui: UiText) {
  if (value === 'No tags') {
    return ui.common.noTags
  }

  return formatFromDictionary(
    ui.auditValues,
    formatFromDictionary(
      ui.tagNames,
      formatFromDictionary(
        ui.categoryNames,
        formatFromDictionary(
          ui.assetStatuses,
          formatFromDictionary(ui.statuses, formatFromDictionary(ui.priorities, value)),
        ),
      ),
    ),
  )
}

export function formatNotificationSubject(value: string, ui: UiText) {
  return formatFromDictionary(ui.notificationSubjects, value)
}

export function formatNotificationBody(value: string, ui: UiText) {
  return formatFromDictionary(ui.notificationBodies, value)
}

export function formatEntityType(value: string, ui: UiText) {
  if (value === 'Ticket') {
    return ui.audit.ticket
  }

  if (value === 'Asset') {
    return ui.audit.asset
  }

  return value
}

export function viewTitle(view: View, ui: UiText) {
  return ui.views[view]
}
