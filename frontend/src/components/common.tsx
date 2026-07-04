import type { ReactNode } from 'react'
import type { AssetStatus, TicketPriority, TicketStatus } from '../types'
import type { UiText } from '../i18n'

export function NavButton({
  active,
  disabled,
  icon,
  label,
  testId,
  onClick,
}: {
  active: boolean
  disabled?: boolean
  icon: ReactNode
  label: string
  testId: string
  onClick: () => void
}) {
  return (
    <button className={active ? 'nav-button active' : 'nav-button'} data-testid={testId} disabled={disabled} type="button" onClick={onClick}>
      {icon}
      {label}
    </button>
  )
}

export function LanguageToggle({
  language,
  ui,
  onChange,
}: {
  language: 'en' | 'pl'
  ui: UiText
  onChange: (language: 'en' | 'pl') => void
}) {
  return (
    <div className="language-toggle" aria-label={ui.language.label}>
      <button className={language === 'en' ? 'active' : ''} type="button" onClick={() => onChange('en')}>
        {ui.language.en}
      </button>
      <button className={language === 'pl' ? 'active' : ''} type="button" onClick={() => onChange('pl')}>
        {ui.language.pl}
      </button>
    </div>
  )
}

export function MetricCard({
  icon,
  label,
  testId,
  tone,
  value,
}: {
  icon: ReactNode
  label: string
  testId?: string
  tone: 'violet' | 'red' | 'green' | 'cyan'
  value: number | string
}) {
  return (
    <article className={`metric-card ${tone}`} data-testid={testId}>
      <div>{icon}</div>
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  )
}

export function PanelHeader({ icon, title }: { icon: ReactNode; title: string }) {
  return (
    <header className="panel-header">
      {icon}
      <h2>{title}</h2>
    </header>
  )
}

export function BarRow({ label, max, value }: { label: string; max: number; value: number }) {
  return (
    <div className="bar-row">
      <span>{label}</span>
      <div>
        <i style={{ width: `${Math.max(8, (value / max) * 100)}%` }} />
      </div>
      <strong>{value}</strong>
    </div>
  )
}

export function InfoTile({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="info-tile">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

export function PriorityBadge({ priority, ui }: { priority: TicketPriority; ui: UiText }) {
  return <span className={`priority-badge ${priority.toLowerCase()}`}>{ui.priorities[priority]}</span>
}

export function StatusBadge({ status, ui }: { status: TicketStatus; ui: UiText }) {
  return <span className={`status-badge ${status.toLowerCase()}`}>{ui.statuses[status]}</span>
}

export function AssetStatusBadge({ status, ui }: { status: AssetStatus; ui: UiText }) {
  return <span className={`status-badge ${status.toLowerCase()}`}>{ui.assetStatuses[status]}</span>
}

export function StatusPill({ label }: { label: string }) {
  return <span className="status-pill">{label}</span>
}

export function TicketTagPill({ color, label }: { color: string; label: string }) {
  return (
    <span className="ticket-tag" style={{ borderColor: color, color }}>
      {label}
    </span>
  )
}
