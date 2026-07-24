import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

/**
 * FrierenHR brand preset — extends PrimeNG's Aura theme with the
 * app's green palette so PrimeNG components (buttons, inputs, tables,
 * cards, etc.) automatically match the custom dashboard/login design.
 */
export const HrPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#eafaf1',
      100: '#c8f0d8',
      200: '#9de0bd',
      300: '#72cf9f',
      400: '#45b980',
      500: '#1f7a4d',
      600: '#1a6b43',
      700: '#155937',
      800: '#14532d',
      900: '#0d3821',
      950: '#082715',
    },
    colorScheme: {
      light: {
        primary: {
          color: '#1f7a4d',
          contrastColor: '#ffffff',
          hoverColor: '#175e3b',
          activeColor: '#14532d',
        },
        surface: {
          0: '#ffffff',
          50: '#f4f6f5',
          100: '#e9ede9',
          200: '#dde3dd',
        },
        highlight: {
          background: '#eafaf1',
          focusBackground: '#c8f0d8',
          color: '#14532d',
          focusColor: '#14532d',
        },
      },
    },
  },
  components: {
    button: {
      colorScheme: {
        light: {
          root: {
            'border.radius': '10px',
          },
        },
      },
    },
    inputtext: {
      colorScheme: {
        light: {
          root: {
            'border.radius': '10px',
          },
        },
      },
    },
    card: {
      colorScheme: {
        light: {
          root: {
            'border.radius': '16px',
          },
        },
      },
    },
  },
});
