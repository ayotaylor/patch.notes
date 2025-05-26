import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import { authService } from "@/services/authService";
import { getStoredToken, clearAuthData } from "@/utils/authUtils";

// Import your views/components
// You'll need to create these components or adjust the paths to match your existing ones
const LoginComponent = () => import("@/components/LoginComponent.vue");
const RegisterComponent = () => import("@/components/RegisterComponent.vue");
const DashboardComponent = () => import("@/components/DashboardComponent.vue");
//const ProfileComponent = () => import("@/components/ProfileComponent.vue");
// const ForgotPassword = () => import('@/views/ForgotPassword.vue')
// const ResetPassword = () => import('@/views/ResetPassword.vue')

const routes = [
  {
    path: "/",
    redirect: "/dashboard",
  },
  {
    path: "/login",
    name: "Login",
    component: LoginComponent,
    meta: {
      requiresGuest: true,
      title: "Login - Patch Notes",
    },
  },
  {
    path: "/register",
    name: "Register",
    component: RegisterComponent,
    meta: {
      requiresGuest: true,
      title: "Register - Patch Notes",
    },
  },
  {
    path: "/dashboard",
    name: "Dashboard",
    component: DashboardComponent,
    meta: { requiresAuth: true, title: "Login - Patch Notes" },
  },
//   {
//     path: "/profile",
//     name: "Profile",
//     component: () => ProfileComponent,
//     meta: {
//       requiresAuth: true,
//       title: "Profile - AuthApp",
//     },
//   },
//   {
//     path: "/:pathMatch(.*)*",
//     name: "NotFound",
//     component: () => import("@/components/NotFoundComponent.vue"),
//     meta: {
//       title: "Page Not Found - AuthApp",
//     },
//   },
  //   {
  //     path: '/forgot-password',
  //     name: 'ForgotPassword',
  //     component: ForgotPassword,
  //     meta: { requiresGuest: true }
  //   },
  //   {
  //     path: '/reset-password',
  //     name: 'ResetPassword',
  //     component: ResetPassword,
  //     meta: { requiresGuest: true }
  //   },
];

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition;
    } else {
      return { top: 0 };
    }
  },
});

// Navigation guards for authentication
router.beforeEach(async (to, from, next) => {
  // set page title
  document.title = to.meta.title || "Patch Notes";

  const token = getStoredToken();
  // Clear auth data if token is invalid or not present
  if (!token) {
    clearAuthData();
  }

  // Validate token if it exists
  if (token) {
    try {
      const isValid = await authService.validateToken(token);
      if (!isValid) {
        clearAuthData();
        if (to.meta.requiresAuth) {
          next("/login");
          return;
        }
      }
    } catch (error) {
      console.error("Token validation error:", error);
      clearAuthData();
      if (to.meta.requiresAuth) {
        next("/login");
        return;
      }
    }
  }

  const authStore = useAuthStore();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next("/login");
  } else if (to.meta.requiresGuest && authStore.isAuthenticated) {
    next("/dashboard");
  } else {
    next();
  }

  // Global error handling for navigation
  router.onError((error) => {
    console.error("Router error:", error);
  });
});

export default router;
