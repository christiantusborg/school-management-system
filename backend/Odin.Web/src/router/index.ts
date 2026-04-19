import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

import AuthLayout from '@/layouts/AuthLayout.vue'
import AppLayout from '@/layouts/AppLayout.vue'

import LoginView from '@/views/auth/LoginView.vue'
import RegisterView from '@/views/auth/RegisterView.vue'
import ProfileView from '@/views/profile/ProfileView.vue'
import AccountSettingsView from '@/views/account/AccountSettingsView.vue'
import RecoveryCodesView from '@/views/account/RecoveryCodesView.vue'
import TeamsView from '@/views/teams/TeamsView.vue'
import TeamDetailView from '@/views/teams/TeamDetailView.vue'
import CasesView from '@/views/cases/CasesView.vue'
import CaseDetailView from '@/views/cases/CaseDetailView.vue'
import AdminDashboardView from '@/views/admin/AdminDashboardView.vue'
import UserListView from '@/views/admin/UserListView.vue'
import UserDetailView from '@/views/admin/UserDetailView.vue'
import InviteCodesView from '@/views/admin/InviteCodesView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/auth',
      component: AuthLayout,
      children: [
        { path: 'login', name: 'login', component: LoginView },
        { path: 'register', name: 'register', component: RegisterView }
      ]
    },
    {
      path: '/',
      component: AppLayout,
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/profile' },
        { path: 'profile', name: 'profile', component: ProfileView },
        { path: 'account', name: 'account', component: AccountSettingsView },
        { path: 'account/recovery-codes', name: 'recovery-codes', component: RecoveryCodesView },
        { path: 'account/two-factor', name: 'two-factor', component: () => import('@/views/account/AccountTwoFactorView.vue') },
        { path: 'teams', name: 'teams', component: TeamsView },
        { path: 'teams/:id', name: 'team-detail', component: TeamDetailView },
        { path: 'cases', name: 'cases', component: CasesView },
        { path: 'cases/:id', name: 'case-detail', component: CaseDetailView },
        {
          path: 'admin',
          meta: { requiresAdmin: true },
          children: [
            { path: '', name: 'admin-dashboard', component: AdminDashboardView },
            { path: 'users', name: 'admin-users', component: UserListView },
            { path: 'users/:id', name: 'admin-user-detail', component: UserDetailView },
            { path: 'invite-codes', name: 'admin-invite-codes', component: InviteCodesView }
          ]
        }
      ]
    }
  ]
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  if (auth.isAuthenticated && !auth.user) {
    await auth.fetchUser()
  }

  if (to.matched.some(r => r.meta.requiresAuth) && !auth.isAuthenticated) {
    return { name: 'login' }
  }

  if (to.matched.some(r => r.meta.requiresAdmin) && !auth.isAdmin) {
    return { name: 'profile' }
  }

  if (to.path.startsWith('/auth') && auth.isAuthenticated) {
    return { name: 'profile' }
  }
})

export default router
