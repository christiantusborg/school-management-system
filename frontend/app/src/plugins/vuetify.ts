import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { aliases, mdi } from 'vuetify/iconsets/mdi'

const lightTheme = {
  dark: false,
  colors: {
    background: '#f5f5f5',
    surface: '#ffffff',
    primary: '#1976d2',
    secondary: '#424242',
    accent: '#82b1ff',
    error: '#ff5252',
    info: '#2196f3',
    success: '#4caf50',
    warning: '#fb8c00',
  }
}

const darkTheme = {
  dark: true,
  colors: {
    background: '#121212',
    surface: '#1e1e1e',
    primary: '#2196f3',
    secondary: '#616161',
    accent: '#82b1ff',
    error: '#ff5252',
    info: '#2196f3',
    success: '#4caf50',
    warning: '#fb8c00',
  }
}

export default createVuetify({
  components,
  directives,
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: {
      mdi,
    },
  },
  theme: {
    defaultTheme: 'light',
    themes: {
      light: lightTheme,
      dark: darkTheme,
    }
  },
  defaults: {
    VCard: {
      elevation: 2,
    },
    VBtn: {
      variant: 'flat',
    },
    VTextField: {
      variant: 'outlined',
      density: 'comfortable',
    },
    VSelect: {
      variant: 'outlined',
      density: 'comfortable',
    },
  }
})