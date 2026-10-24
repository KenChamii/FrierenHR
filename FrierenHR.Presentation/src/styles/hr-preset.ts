import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

/** FrierenHR brand preset. */
export const HrPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f4efe4',
      100: '#e7dcc4',
      200: '#d4bb91',
      300: '#c0a273',
      400: '#b4945e',
      500: '#a8854f',
      600: '#8f7043',
      700: '#755b37',
      800: '#4a6b5c',
      900: '#1f3a2e',
      950: '#173025',
    },
    colorScheme: {
      light: {
        primary: {
          color: '#1f3a2e',
          contrastColor: '#f5f0e6',
          hoverColor: '#2f5042',
          activeColor: '#173025',
        },
        surface: {
          0: '#faf7ef',
          50: '#f5f0e6',
          100: '#efe9dc',
          200: '#ddd4c2',
        },
        highlight: {
          background: '#f0e9da',
          focusBackground: '#e7dcc4',
          color: '#1f3a2e',
          focusColor: '#1f3a2e',
        },
        formField: {
          background: '#faf7ef',
          borderColor: '#ddd4c2',
          hoverBorderColor: '#a8854f',
          focusBorderColor: '#1f3a2e',
          color: '#1f3a2e',
          placeholderColor: '#6b7b73',
        },
      },
    },
  },
  components: {
    button: {
      colorScheme: { light: { root: { 'border.radius': '2px' } } },
    },
    inputtext: {
      colorScheme: { light: { root: { 'border.radius': '2px' } } },
    },
    card: {
      colorScheme: { light: { root: { 'border.radius': '2px' } } },
    },
    select: {
      colorScheme: { light: { root: { 'border.radius': '2px' } } },
    },
    datatable: {
      colorScheme: { light: { root: { 'border.color': '#ddd4c2' } } },
    },
  },
});
