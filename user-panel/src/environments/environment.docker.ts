export const environment = {
  production: false,
  authApiUrl:    '/api/v1',
  userApiUrl:    '/api/v1',
  catalogApiUrl: '/api/v1',
  cartApiUrl:    '/api/v1',
  orderApiUrl:   '/api/v1',
  adminApiUrl:   '/api/v1',
  // ENH-AI-005 — Analytics (empty in Docker dev; set via env vars for staging)
  analytics: {
    ga4MeasurementId:         '',
    metaPixelId:              '',
    mixpanelToken:            '',
    appInsightsConnectionStr: '',
  },
};
