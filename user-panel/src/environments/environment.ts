export const environment = {
  production: false,
  authApiUrl:    '/api/v1',
  userApiUrl:    '/api/v1',
  catalogApiUrl: '/api/v1',
  cartApiUrl:    '/api/v1',
  orderApiUrl:   '/api/v1',
  adminApiUrl:   '/api/v1',
  // ENH-AI-005 — Analytics config (replace placeholders before deploying)
  analytics: {
    ga4MeasurementId:         '',   // Google Analytics 4 — e.g. 'G-XXXXXXXXXX'
    metaPixelId:              '',   // Meta / Facebook Pixel — e.g. '123456789012345'
    mixpanelToken:            '',   // Mixpanel project token — e.g. 'abc123...'
    // ENH-INFRA-012 — App Insights RUM
    appInsightsConnectionStr: '',   // Azure App Insights connection string
  },
};
