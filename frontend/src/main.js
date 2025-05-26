import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from '@/router'
import Toast from 'vue-toastification'
import 'vue-toastification/dist/index.css'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
//import '@assets/styles/main.css'

// Font Awesome
import '@fortawesome/fontawesome-free/css/all.min.css'

const app = createApp(App)//.mount('#app')
const pinia = createPinia();

// configure toast notifications
const toastOptions = {
    position: 'top-right',
    timeout: 5000,
    closeOnClick: true,
    pauseOnFocusLoss: true,
    pauseOnHover: true,
    draggable: true,
    draggablePercent: 0.6,
    showCloseButtoonOnHover: false,
    hideProgressBar: false,
    closeButton: 'button',
    icon: true,
    rtl: false,
}

app.use(pinia)
app.use(router)
app.use(Toast, toastOptions)

// Global properties (if needed)
app.config.globalProperties.$toast = Toast

// Mount the app
app.mount('#app')
