export const environment = {
  production: true,
  authApiUrl:    '/api/v1',
  userApiUrl:    '/api/v1',
  catalogApiUrl: '/api/v1',
  cartApiUrl:    '/api/v1',
  orderApiUrl:   '/api/v1',
  adminApiUrl:   '/api/v1',
  // ENH-AI-005 — Analytics (inject real IDs via CI/CD environment substitution)
  analytics: {
    ga4MeasurementId:         'REPLACE_GA4_MEASUREMENT_ID',
    metaPixelId:              'REPLACE_META_PIXEL_ID',
    mixpanelToken:            'REPLACE_MIXPANEL_TOKEN',
    appInsightsConnectionStr: 'REPLACE_APP_INSIGHTS_CONNECTION_STRING',
  },
};
