import { createRouter, createWebHashHistory } from 'vue-router'
import LoginView        from '../views/LoginView.vue'
import AdminView        from '../views/AdminView.vue'
import PartnerView      from '../views/PartnerView.vue'
import ProgrammesView   from '../views/ProgrammesView.vue'
import PartnerDetailView from '../views/PartnerDetailView.vue'
import ApplyView         from '../views/ApplyView.vue'
import VerifyEmailView   from '../views/VerifyEmailView.vue'
import ApplicationView   from '../views/ApplicationView.vue'
import AdminStudentView  from '../views/AdminStudentView.vue'
import AdminConfigView   from '../views/AdminConfigView.vue'
import AdminAcademicView from '../views/AdminAcademicView.vue'
import StudentView       from '../views/StudentView.vue'
import { auth } from '../store/auth.js'

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: LoginView },
  { path: '/apply', component: ApplyView },
  { path: '/apply/verify-email', component: VerifyEmailView },
  {
    path: '/student/application',
    component: ApplicationView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isStudent) return auth.isEmployee ? '/admin' : '/partner'
    },
  },
  {
    path: '/admin/academic',
    component: AdminAcademicView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isEmployee) return '/partner'
    },
  },
  {
    path: '/admin/config',
    component: AdminConfigView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isEmployee) return '/partner'
    },
  },
  {
    path: '/admin/students/:id',
    component: AdminStudentView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isEmployee) return '/partner'
    },
  },
  {
    path: '/admin',
    component: AdminView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isEmployee) return '/partner'
    },
  },
  {
    path: '/programmes',
    component: ProgrammesView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isEmployee) return '/partner'
    },
  },
  {
    path: '/partners/:id',
    component: PartnerDetailView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isEmployee) return '/partner'
    },
  },
  {
    path: '/partner',
    component: PartnerView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isPartner) return '/admin'
    },
  },
  {
    path: '/partner/change-password',
    component: () => import('../views/PartnerChangePasswordView.vue'),
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isPartner) return '/admin'
    },
  },
  {
    path: '/student',
    component: StudentView,
    beforeEnter: () => {
      if (!auth.user) return '/login'
      if (!auth.isStudent) return auth.isEmployee ? '/admin' : '/partner'
    },
  },
]

export default createRouter({
  history: createWebHashHistory(),
  routes,
})
