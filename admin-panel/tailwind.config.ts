import type { Config } from 'tailwindcss';

export default {
  content: ['./src/**/*.{html,ts,scss}'],
  theme: {
    extend: {
      colors: {
        navy:       '#1C2B4A',
        red:        '#E31837',
        blue:       '#0071C2',
        bg:         '#F5F5F5',
        card:       '#FFFFFF',
        dark:       '#1A1A1A',
        muted:      '#757575',
        'mid-gray': '#9E9E9E',
        border:     '#E0E0E0',
        gold:       '#C9A84C',
        success:    '#2E7D32',
        warning:    '#F57C00',
        error:      '#C62828',
      },
      fontFamily: {
        display: ['"Playfair Display"', 'Georgia', 'serif'],
        sans:    ['"DM Sans"', '-apple-system', 'sans-serif'],
      },
      screens: {
        sm:    '480px',
        md:    '768px',
        lg:    '1024px',
        xl:    '1280px',
        '2xl': '1440px',
      },
    },
  },
  plugins: [],
} satisfies Config;
