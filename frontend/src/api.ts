import type { View } from './types'

export const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

export const viewPaths: Record<View, string> = {
  dashboard: '/dashboard',
  tickets: '/tickets',
  assets: '/assets',
  users: '/users',
  categories: '/settings/categories',
  audit: '/audit',
  notifications: '/notifications',
}

export function viewFromPath(pathname: string): View {
  if (pathname.startsWith('/tickets')) {
    return 'tickets'
  }

  if (pathname.startsWith('/assets')) {
    return 'assets'
  }

  if (pathname === '/users') {
    return 'users'
  }

  if (pathname === '/settings/categories') {
    return 'categories'
  }

  if (pathname === '/audit') {
    return 'audit'
  }

  if (pathname === '/notifications') {
    return 'notifications'
  }

  return 'dashboard'
}
