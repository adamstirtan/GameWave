import { createApp } from 'vue'
import { registerPlugins } from '@/plugins'

import App from './App'

const app = createApp(App)

registerPlugins(app)

app.mount('#app')
