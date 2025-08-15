import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import { getStoredToken, clearAuthData } from "@/utils/authUtils";
import CompleteProfile from "@/components/CompleteProfile.vue";

// Import your views/components
// You'll need to create these components or adjust the paths to match your existing ones
const LoginComponent = () => import("@/components/LoginComponent.vue");
const RegisterComponent = () => import("@/components/RegisterComponent.vue");
const DashboardComponent = () => import("@/components/DashboardComponent.vue");
const ProfileComponent = () => import("@/components/ProfileComponent.vue");
const GameDetailsComponent = () => import("@/components/GameDetailsComponent.vue");
const ReviewsListPage = () => import("@/components/ReviewsListPage.vue");
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
      validateToken: "never",
      title: "Login - Patch Notes",
    },
  },
  {
    path: "/register",
    name: "Register",
    component: RegisterComponent,
    meta: {
      requiresGuest: true,
      validateToken: "never",
      title: "Register - Patch Notes",
    },
  },
  {
    path: "/dashboard",
    name: "Dashboard",
    component: DashboardComponent,
    meta: {
      requiresAuth: true,
      validateToken: "cache",
      title: "Dashboard - Patch Notes",
    },
  },
  {
    path: "/complete-profile",
    name: "CompleteProfile",
    component: () => CompleteProfile,
    meta: {
      requiresAuth: true,
      validateToken: "cache",
      title: "Complete Your Profile - Patch Notes",
    },
  },
  {
    path: "/profile",
    name: "MyProfile",
    component: ProfileComponent,
    meta: {
      requiresAuth: true,
      validateToken: "cache",
      title: "Profile - Patch Notes",
    },
  },
  {
    path: "/profile/:userId",
    name: "UserProfile",
    component: ProfileComponent,
    meta: {
      requiresAuth: false,
      validateToken: "never",
      title: "Profile - Patch Notes",
    },
  },
  {
    path: "/games/:identifier",
    name: "GameDetails",
    component: GameDetailsComponent,
    props: (route) => {
      const identifier = route.params.identifier;
      console.log("Router props for identifier:", identifier);

      // Check if identifier is numeric (ID) or string (slug)
      if (!isNaN(identifier) && !isNaN(parseFloat(identifier))) {
        return {
          gameId: identifier,
          slug: null,
        };
      } else {
        return {
          gameId: null,
          slug: identifier,
        };
      }
    },
    meta: {
      requiresAuth: false,
      validateToken: "never",
      title: "Game Details - Patch Notes",
    },
  },
  // Reviews routes
  {
    path: "/reviews",
    name: "AllReviews",
    component: ReviewsListPage,
    meta: {
      requiresAuth: false,
      validateToken: "never",
      title: "All Reviews - Patch Notes",
    },
  },
  {
    path: "/games/:gameId/reviews",
    name: "GameReviews",
    component: ReviewsListPage,
    props: (route) => ({
      gameId: route.params.gameId,
      gameName: route.query.name || ''
    }),
    meta: {
      requiresAuth: false,
      validateToken: "never",
      title: "Game Reviews - Patch Notes",
    },
  },
  {
    path: "/profile/:userId/reviews",
    name: "UserReviews",
    component: ReviewsListPage,
    props: (route) => ({
      userId: route.params.userId,
      userName: route.query.name || ''
    }),
    meta: {
      requiresAuth: false,
      validateToken: "never",
      title: "User Reviews - Patch Notes",
    },
  },

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
  //     meta: {
  //      requiresGuest: true,
  //      validateToken: "never",
  //      title: 'Forgot Password - Patch Notes'
  //     }
  //   },
  //   {
  //     path: '/reset-password',
  //     name: 'ResetPassword',
  //     component: ResetPassword,
  //     meta: {
  //        requiresGuest: true
  //        validateToken: "never",
  //        title: 'Reset Password - Patch Notes'
  //     }
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

  const authStore = useAuthStore();
  const token = getStoredToken();

  if (!to.meta.requiresAuth && !to.meta.requiresGuest) {
    return next();
  }

  // If no token exists, clear any stored auth data
  if (!token) {
    clearAuthData();
    authStore.clearAuthState(); // Clear store state

    if (to.meta.requiresAuth) {
      return next("/login");
    }
    return next();
  }

  // Load user from storage if not already loaded and has a valid token
  if (!authStore.isAuthenticated && token) {
    authStore.loadUserFromStorage();
  }

  // Route-specific logic
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return next("/login");
  }

  if (to.meta.requiresGuest && authStore.isAuthenticated) {
    return next("/dashboard");
  }

  // Only validate token for protected routes and if not recently validated
  if (to.meta.requiresAuth) {
    const validationStrategy = to.meta.validateToken || "cache";
    let isValid = true;
    try {
      if (validationStrategy === "always") {
        // Always validate token
        isValid = await authStore.validateToken();
      } else if (validationStrategy === "cache") {
        const lastValidation = authStore.lastTokenValidation;
        const now = Date.now();
        const validationThreshold = 5 * 60 * 1000; // 5 minutes in milliseconds TODO: maybe get from config

        // Only call backend if token hasn't been validated recently
        if (!lastValidation || now - lastValidation > validationThreshold) {
          isValid = await authStore.validateToken();
        }

        if (!isValid) {
          clearAuthData();
          authStore.clearAuthState();
          return next("/login");
        }
      }
      // Check if token was recently validated (within last 5 minutes)
    } catch (error) {
      console.error("Token validation error:", error);
      clearAuthData();
      authStore.clearAuth();
      return next("/login");
    }
  }

  next();

  // Global error handling for navigation
  router.onError((error) => {
    console.error("Router error:", error);
  });
});

export default router;
